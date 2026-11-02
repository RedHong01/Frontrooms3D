# V7a foil look: research and build (游戏视觉, 2026-11-01)

C13 (foiled windows), Relay v2 step V7a. Stage 2 of the workflow `v7-alarm-looks`, session local_5656cffd, account A.

**Asks.** Red, 2026-11-01 18:49, in the 游戏视觉 chat: "我需要你接手之前另外账号暂停的工作然后继续" (resume the visual line). The work item is 关卡设计's relay of 2026-10-31 12:44 (their words, not Red's): "Relay v2 V7a LANDED (flag foil off; one trigger budget shared with Run!). 游戏视觉: foil tape on the pane's perimeter and a magnetic reed contact on the sash, at top spec. The hook is Window.foiled on WindowBuilt; every build fires it, rebuilt and broken windows included. The latch is V6's Window.latch, and its aim kind is AimKind.PryAlarmed. The foil flag stays off until this look lands." Standing rules: top spec ("在做视觉的时候，我希望你构思都以最高规格的去进行构思和落地", VISUAL_STANDARD 1.1) and rule 25 ("做的时候先不用测，直接做，然后等都落地了一起测即可").

**Done / not done.** Main read only. No download: sources were read through the web reader (two NIJ/NBS scans were read from their own text layer); nothing third-party is kept in the repo. One Unity batch in the clone `W/proj_bstations`, only to compile the shader offline (66 variants, OK); no Play run, no render, no frame (rule 25: the frames are specified in sec 10 for the combined test). Roslyn macOS and iOS: 0 errors.

**Reused, not redone.** V6's window (pocket double-hung, cam lock on the meeting rails: `research/glass/v6_sash/10_research.md`), GD3's glass stages and debris world (`research/glass/destruction/`), the kit's window section (`FrontRoomsInteractableKit.WindowSection`).

## 1. The tape

| Fact | Source (id in SOURCES.md) |
|---|---|
| Alarm foil "usually about 3/8" wide", 0.002 in thick, a "flexible lead-tin or lead-zinc compound tape" with a self-adhesive back, sold in rolls of about 300 ft | F1 |
| Laid on a narrow coat of varnish the width of the tape, then a **wider** varnish coat over it to seal out moisture | F1 |
| The margin from the frame is set with a guide (its recesses 1 in apart); corners: the tape forms "a sharp 45° bend" and the installer presses "a sharp crease at the overlapped point" | F1 |
| One continuous strip round the perimeter, both ends to a terminal block; corners are folded, never cut (a fold, then a 45° bend back), skilled work; lead or aluminium | F2 |
| Foil is either pre-coated with adhesive (Type I) or laid in varnish or another adhesive (Type II); 10 m may not exceed 12.5 ohm | F3 |
| A foil sensor goes back to the 1890s: a thin strip round the border near the frame, one break at an upper corner where the ends are taken off | F4 |
| Installers' recollection: diluted white glue to lay the tape, then varnish over it; old tape leaves "lines" on the glass | F11 |
| By the 2000s foil was a "lost art": delicate, high maintenance, replaced by zoned contacts and glass-break detectors; the old tape could be soldered, the later aluminium tape could not | F12 |

Era: every source here is dated 1974-1983 (F4: 1895). A building fitted out about 1975-1985 and still running its alarm in 1990 fits the era lock (≤ 1993).

## 2. How foil behaves when the glass breaks

- NBS (1978): "metallic window foil does not necessarily break when the glass upon which it is installed is broken"; a surprising number of samples did not (F5). The NIJ standard (1980) asks that at least 17 of 20 strips crossing a clean break open the circuit (F3).
- So the tape may bridge a fresh crack and parts when the pieces move. The game agrees: the map trips the device when the pane gives (stage 3) or the sash gives, never on a crack (关卡设计's s62 note, reading 3). On screen the tape is ON the glass, so at stage 2 it is cut wherever the pieces have moved, and at the shatter every shard carries its own scrap of tape.

## 3. The hardware

| Part | Facts | Source |
|---|---|---|
| Foil block | A moulded plastic base with a ramp up which the tape's end runs, a channel at its top, a "chrome plated bronze" clamp plate and a screw through plate and tape; the lead wire goes under the screw; an adhesive strip to stick it to the glass, or screw holes for the frame; single blocks, or **double blocks** when both ends leave on the same side | F6 |
| Foil block (alternative) | A one-piece polypropylene block with a snap lid; the prior art was "a metal clamp ... held by a small screw" | F7 |
| Movable sashes | Spring-contact units: one on each movable sash (upper and lower), one on the sill or the head; the foil block sits on the glass; the contacts part when the sash opens | F8 |
| Sliding sash take-off | Sentrol, 1981: spring contacts between the sash and its track carry the foil loop's circuit | F9 |
| Magnetic contact | The switch on the frame, the magnet on the moving part, "mounted symmetrically opposite the switch on a flush door or window"; surface or recessed; a simple switch must open between 2.0 and 5.0 cm of travel | F10 |
| Surface contact housing | Sentrol, 1981: a moulded housing on a hinged bracket; the prior art had "externally located screw-post terminals, typically left exposed" | F13 |
| Placement | The magnet on the edge of the door or window, the switch opposite it in the frame or casing; wiring hidden in the wall where it can be | F14 |

No source gives the dimensions of a 1980s block or contact. The sizes below are design values chosen from the parts' descriptions and marked as such.

## 4. Design calls (built)

1. **Tape:** 3/8 in (9.525 mm) lead-tin, its outer edge 1 in (25.4 mm) inside each lite's sight line. That keeps the tape in GD3's tooth band (83.5 mm on the sides and top without the sashes; 38.5 mm inside the sash stiles with them), so after a shatter the teeth kept in the stop still carry their torn scraps of tape, the image a broken foiled window is known by; the bottom run lies in the climb's clear zone and goes with the glass.
2. **Corners:** the mitre fold of F1. The tape folds on the diagonal of the corner square, so the outer corner triangle is bare glass, the inner triangle is two layers, and the crease is a fine bright line.
3. **Varnish:** the overcoat runs 1.5 to 4 mm past the tape, brushed (its edge wanders), glossy and amber; the amber deepens with a per-window age (0.45-0.85).
4. **From the other side** the tape is seen through 6 mm of glass: its underside, flat on the glass, is a slightly hazy mirror strip, shifted along the refracted eye ray.
5. **Blocks:** a double block (32 x 19 x 5.5 mm, design values) inline on the run nearest the alarm side (left or right by the edge's hash), at the contact's height; the tape's two ends run up its ramps under two chrome clamps; the lead's copper loops under the screw heads.
6. **Fixed glazing (sashPry off):** one loop; the lead (a 22 AWG pair) crosses 2 cm of glass and the room stop to a two-screw terminal block on the stop (wiring on into the wall). No magnetic contact: nothing moves.
7. **The pocket double-hung (sashPry on):** a loop on each lite; a block on each; the reed switch on the room stop just under the meeting rails (body 44 x 12.7 x 11 mm, end tabs with exposed binding-head posts and washers), the magnet on the room sash's stile beside it (36 x 12.7 x 10 mm with screw ears), 4.7 mm apart. Each lead runs over the glass, up the stile's edge, under a stapled clip, across the sash/frame joint and along the stop to a post. These parts sit on the alarm side clear of V6's lock, keeper and sash lifts, and of the steel stop's oval heads.
8. **The pry:** the room sash's 4 mm creep moves the magnet 4 mm (a reed opens only past 2-5 cm: F10), so nothing reads tripped while the bar works. When the sash gives, the leads tear at the joint (their stile ends go up with the sashes; their stop ends drop and hang from the last staple, bare copper at the tear), the blocks ride up on the glass into the head, and the magnet, proud of the sash face, is sheared off by the head stop (V6's rule: everything that rises stays within the stop's depth) and falls onto the room-side sill. `PartDropped` reports the moment for the sound line.
9. **The shatter:** each block pops off its glass and hangs on its lead from the stile's clip (or the terminal's edge), never floating where the glass was; the hanging blocks stay at |X| ≥ 0.635, outside the climb's clear zone.
10. **Rebuilt windows** take their state from the map: pried (blocks gone up with the glass, leads hanging, the magnet on the sill), broken (blocks hanging) or standing.

## 5. How the tape follows the glass

The tape is drawn by FrontRooms/Glass itself (`FrontRoomsGlassFoil.hlsl`), laid out in pane metres, the frame the slab, both crack stages, the shards, the teeth and the floor glass all carry in UV0 for the grime. The layout travels in the window's property block (`FrontRoomsGlassBreakable.Foil`), and the debris world copies that block onto every shard, tooth and floor-glass renderer, at the shatter and on the return after a walk away. So the tape rides up with a pried sash (`SashLift` moves the glass) and tears exactly where the glass tears, with no second copy of the fracture to keep in step. Only the hardware is separate geometry.

## 6. GlassRT (the ray-traced reflections)

- **On the foiled pane** (a receiver): the RT prepass takes the tape's own normal and smoothness. On the tape's face (crinkled varnish, smoothness 0.60) the traced reflection is weighted by the tape's reflectance and mostly faded by the roughness rule, so the varnish shows a soft sheen, not a mirror. Seen from the other side the near surface is the glass, so the trace keeps the glass's flat normal; the tape's underside behind it is a mirror of reflectance about 0.5-0.6 at smoothness 0.86, so there the traced reflection shows as a bright mirrored strip. The tape is opaque (alpha 1), so nothing behind it shows through.
- **In other glass's reflections:** the tracer treats this pane as glass (`KindGlass`) and does not know the tape, so a foiled window seen in another pane's traced reflection shows no tape. The hardware is ordinary opaque geometry under the chunk, registered by the chunk scan like other kit parts and seen in reflections; its moves (the pry, the drops) reach the traced copy at the static refresh rate, a few frames late.

## 7. Inert while foil is off (code reading)

`FrontRoomsLevelProfile.foil` defaults to false and `FrontRoomsLevel0.asset` has no foil line; `TakeProfile` then gets `FoilRules() == null`; `CreateCache` calls `Cache.SetFoil` only when the rules exist, so `Cache.Foil` is null; `BuildEdge` sets `Window.foiled = Cache.Foil != null && ...`, so false. `DressWindow` calls `FrontRoomsWindowFoil.Attach` only `if (window.foiled)`: no component, no mesh, no material. `FrontRoomsGlassBreakable.ApplyBlock` writes the foil vectors only when `foil.On`; no window sets it, so every block is as before, and the shader's `_FoilShape.z` stays at the material default 0, so both passes skip the foil branch (uniform, no new variant). What changes with foil off: the shader's per-material constant buffer grows by 80 bytes (UnityPerMaterial 416 bytes, the same in both passes, so the SRP batcher still takes it).

## 8. Tiers and budget

| | Desktop near (LOD0) | Desktop far (LOD1) / phone |
|---|---|---|
| sashPry on | 9,316 tris (blocks 2 x 1,036; switch 1,656; magnet 824; clips 3 x 212; leads 1,440 + 2,688) | parts 880 tris; leads culled past 8 m (phone leads 504) |
| sashPry off | 2,832 tris (block 1,036; terminal 1,092; lead 704) | 384 tris (+ phone lead 80) |

Draws near, sashPry on: about 26 (blocks 2 x 5 materials, switch 3, magnet 3, clips 6, leads 4); far: about 12. The part meshes are built once per tier and shared; each lead is its own tube (a few hundred to 2,700 tris), rebuilt only while it swings. Shadows: the blocks and the contact cast on desktop; leads, clips and every phone mesh do not. Shader cost: only the foiled pane's pixels run the foil branch (a band test, then three value noises within 6 mm of the tape). Frame time not measured (rule 25).

## 9. Honest limits

- No frame has been rendered: the look is unseen until the combined test (sec 10).
- Block and contact sizes are design values (no source gives dimensions). Each lead's two conductors both end under one post (or the two terminal screws); the zone wiring runs inside the frame and is not drawn.
- From the other side, a crack at stage 1 that crosses the tape is hidden behind the tape's mirror (the fins sit inside the glass, the tape is drawn on the near face).
- The tape casts no shadow (the glass draws no shadow caster). It does not appear in other panes' traced reflections (sec 6).
- 关卡设计's open strings (`HOLD E · PRY SASH · ALARMED`) are signed in `research/narrative/90_alarm_devices_and_door_strings_signoff.md` §3; no lettering is drawn by this look.

## 10. Frames for the combined test (with `foil` on, and `sashPry` on and off, on a scratch profile only)

1. A foiled W-L0 and W-OF window at 2 m from the room side and the hall side: the tape's band, mitred corners, block, lead and terminal or contact; nothing z-fights with the stops; the tape reads as tape, not a frame line.
2. Hero 0.3 m: a corner fold, the block (ramps, clamps, screws, copper loops), the switch's posts and the magnet; then 5 m (the LOD1 switch, no pop) and 10 m (leads culled).
3. Pry (sashPry on): the 4 mm creep moves the magnet; at the give, the leads tear and hang, the blocks rise with the glass, the magnet is sheared at the head and lands on the room sill; nothing pokes out above the head.
4. Shatter: S1, S2 (the tape cut where the pieces moved), S3 (scraps of tape on the shards and the floor glass, torn tape on the kept teeth, the blocks hanging on their leads); then a walk away and back (the floor glass returns with its tape).
5. Phone tier (iPhone, iPad): the same window at 1 m.
6. The flag off: a 2554 seed walk past windows shows today's glass, byte for byte in look.
