# 10 — Interactables kit spec: doors, locks, keys, windows

Status: SPEC v1, 2026-10-03 (written from `00`–`06`; nothing in the real project was changed). This file is the build contract for the kit builders (§10) and the integration contract for the map chat (§6).

**Binding inputs, in order of authority**
1. Red's decisions (relayed 2026-10-02 23:3x – 2026-10-03 00:0x): (i) **Option A, single-acting doors with real stops**; (ii) a **door family per level** (Lobby, Office, Run, Exit), each with a FREE and a LOCKED member; (iii) a **real keyable lock** with separate animatable parts; (iv) **windows with real structure**, one family per level; (v) **highest spec**: a hero LOD0 for 0.3 m inspection, plus LOD1 and LOD2; (vi) **glass fracture belongs to the glass-destruction track** (`../glass/destruction/`). This kit only provides the window assembly, the intact-pane slot, and the interface that the fracture set must fit.
2. `00_map_constraints.md` (map chat). It is obeyed everywhere below. Section §1.6 restates its rules in kit terms, and §6.6 lists the rule clarifications I ask for.
3. The research reports: `01_inventory.md`, `02_period_hardware.md`, `03_readability_placement_shots.md`, `04_door_re8_gap.md`, `05_locked_door_type.md`, `06_period_windows.md`.
4. `../interaction_audit/10_audit_report.md` §3–§4, `FrontRoomsShotTimings.proposal.cs.txt`, `../office_and_film/22_era_lock.md`, and `Documentation/FONTS_PERIOD_1990.md`.
5. 平面视觉's binding 1990 era notes (`VISUAL_CHAT_TASKS.md` R10):
   - **interchangeable-core (IC) cylinders** with the figure-8 core face, generic and unbranded;
   - push bars on exits;
   - plastic ring key tags with a paper insert;
   - 1-inch aluminium mini-blinds;
   - **no keypads, card readers or LED lock indicators.**

Red's relayed request for this run ("ChatGPT built an independent ray-traced glass track; understand it and add that task too") is covered in §7. The track already has a row in the visual chat's queue (G14, RUNNING in another workflow). §7 adds what this kit must do for it, plus four new defects found while writing this spec.

**Citations.** Code is cited as `file:line` in the real project, read 2026-10-03 10:3x. `MapWorld` = `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`. The map chat is editing it, so line numbers drift. Web claims are cited through the report that read them (`02` §13, `04` §10, `05` §10, `06` §8). I did not open new web pages for this spec.

**Tags used here**
- **ESTIMATE**: a designer number with no source.
- **UNVERIFIED**: a claim nobody has checked in a capture or a run.
- **NEEDS APPROVAL**: a change to `kitlib.py`, `build_asset.py`, the importer or a shader. This workflow must not make it; the visual chat decides.

---

## 0. Decisions in one table

| # | Question | Decision | § |
|---|---|---|---|
| D1 | Door construction | **Option A2**, RE8-derived: a single-acting leaf hung on three 4-1/2" 5-knuckle butts. The pivot moves 29.5 mm toward the swing side and 9.8 mm into the opening (a two-line map change). There are 16 mm stops on the push side, casing on both faces, 2 mm linings over the bare reveal, and a 12 mm saddle. The leaf is a 44 mm visual leaf that overlaps the stops by 13 mm. The 0.05 × 2.08 × 0.98 collider is unchanged. | 1 |
| D2 | See-through slits | None from either side at any angle. Every perimeter path is L-shaped through the stop, and the 3 mm floor gap needs a sightline ≤ 4°. | 1.4 |
| D3 | Clipping while swinging | Computed for 0–95° in 0.1° steps: the leaf clears the stop, linings and casing by **≥ 2.8 mm** everywhere. The lever and knob clear by ≥ 27 mm. | 1.5 |
| D4 | Handing | Assets are authored for one handing (swing side = +X). The map mirrors X for the other handing. Text parts cancel the mirror. | 1.2 |
| D5 | Door family | 8 members (Lobby, Office, Run, Exit × FREE/LOCKED), built from **2 frame meshes and 3 leaf meshes** (4 with the P2 lite leaf), plus shared hardware. FREE = warm wood leaf; LOCKED = light almond enamel steel leaf in a dark-bronze frame (`05`'s tested value rule). Lobby and Office are **P1** (the map builds these today); Run and Exit are **P2** (no map door exists yet). | 2 |
| D6 | Locked lockset | **Mortise lock with an IC mortise cylinder at the lock point (1.000 m), a knob below it on a tall escutcheon, a 25 mm deadbolt and a deadlocking latchbolt.** The plug turns 90° with the key, the deadbolt slides, and the knob turns. This answers Red's (iii) literally ("a deadbolt that slides, a lever/knob"), keeps `05`'s knob-versus-lever read, and keeps the keyway at 1.0 m for the strongest head dip. It replaces `05`'s bored key-in-knob, which has no deadbolt. | 2.1, 3.1, 3.4 |
| D7 | Free lockset | Bored passage lever on a round rose at 1.000 m; no keyway. | 2.1, 3.1 |
| D8 | Key | A brass IC-system key, 58 mm long, with a **25 mm blade from shoulder to tip** (= `Unlock.InsertDepth`). Origin at the shoulder on the turning axis. The blade section and the plug keyway are one shared profile (§3.2). | 3.2–3.3 |
| D9 | Key tags | A plastic tag with a paper insert (era note): 3 shapes × red/blue/white = 9 identities with existing slots. Numbers are typed in Courier Prime through a new number atlas. | 4.1 |
| D10 | Key hosts | Board, cabinet and single hook (`03`), wired to the map's existing **module `KeySpot` marker and its `host` field** (`FrontRoomsRoomModuleData.cs:66-78`). Hosted keys lie or hang still; there is no emission. | 4.2–4.3 |
| D11 | Windows | `06`'s family: wood (Lobby), dark-bronze steel (Office), white enamel steel (Run, P2) and clear aluminium (Exit, P2). They share the door's casing envelope and 16 mm stops. The optional 1" mini-blind is Office-only and always raised. | 5 |
| D12 | Glass | Out of this kit, except for the interface (§5.4): the 6 mm pane, the pocket, the bead sight line and the tooth band. The glass track owns the intact pane, the cracks, the shards and the floor glass. | 5.4 |
| D13 | LODs | Every asset declares LOD0/1/2 ratios and **switch distances**. Today the kit exports only LOD1, and the importer culls small objects at 3 % screen height (about 1.4 m for a knob). So small parts must ship **without** a LODGroup until the LOD change is approved. | 1.8, 8 |
| D14 | Integration | Recommended: a visual-chat facade, `FrontRoomsInteractableKit.DressDoor / DressWindow / DressKey`, called by the map. The raw `FrontRoomsKitLibrary.Spawn` recipe is given too. | 6 |
| D15 | Build | **Four groups:** G1 door construction + closers; G2 lock hardware; G3 keys, tags, hosts and signage; G4 windows. | 10 |

---

## 1. The door (top priority): gap-free, RE8-derived, single-acting

### 1.1 What RE8 gives us, and what we keep

`04` §2 is the evidence: the official Steam screenshot (V1) and walkthrough frames (V2). What we keep from it:

1. **A deep, contrasting surround.** Casing on both faces, a lined reveal and a threshold. The perimeter reads as a **dark line, never a lit one**.
2. **A thick leaf with relief.** The edge shows when it opens, and arrises catch light.
3. **A threshold under the leaf.**
4. **Hardware at the latch stile at hand height.** The hand pushes there, and the leaf swings away from the actor.
5. **Nothing open behind any gap.**

Red picked Option A (`VISUAL_CHAT_TASKS.md` W6). The 1990 alibi is the US commercial single-swing door:
- a 1-3/4" leaf;
- a 5/8" (16 mm) stop;
- 1/8" (3 mm) clearances;
- three 4-1/2" butts at "5-10-equal";
- a threshold ≤ 1/2" (`02` §2, §5; `04` §3.1).

**Option B is retired** by Red's choice: the double-acting door with a radius stile, seals and no latch (`04` §5). `04` §5 stays documented as the fallback if Option A is ever reverted. The frame envelope, linings, saddle and shadow proxy below are the same in both options, so a revert would change only the stile profiles, the seals-versus-stops choice and the hinges (`04` §7 item 2).

### 1.2 Frames and handing

**Door root frame D.** Every door asset is authored in this one frame, so the builders never convert between them.
- **Origin:** the opening's hinge-jamb edge, on the wall centre line, at floor level. This is today's `Door hinge` position, `start + along·(c − 0.5)` (`MapWorld:1017-1020`).
- **Rotation:** `door.closed` = `LookRotation(along, up)`.
- **Axes:**
  - **+Z** runs along the opening, from the hinge jamb (Z 0) to the latch jamb (Z 1.0);
  - **+Y** is up;
  - **+X is the swing side S**: the room the leaf opens into, also called the pull face;
  - **−X is the push side P**: where the stops are, also called the push face.
- **Handing.** Assets are authored for S = +X. Let `S_sign` = +1 when the swing room is on the hinge-local +X side, and −1 otherwise. In today's code `door.swing = +1` sends the leaf toward −X (`MapWorld:1870-1880`, `01` §1.6), so **`S_sign = −door.swing`**.
  - The map (or the facade, §6.1) applies `localScale = (S_sign, 1, 1)` to the frame root and to the leaf rig. Unity draws negative-scale renderers with the winding flipped, so they render correctly.
- **Face names.** `_s` is the swing/pull face (authoring +X). `_p` is the push/stop face (authoring −X). Because the whole door is mirrored with its handing, `_s` is always the pull face in the world.
- **Blender.** The kit exports Blender (x, y, z) as Unity (−x, z, −y) (`kitlib.py:789-806`). So for door and window assemblies, **Blender (x, y, z) = (−X, −Z, Y)**:
  - the opening runs from Blender y 0 to y −1.0;
  - the S face is Blender −x;
  - the P face (stops) is Blender +x.
  - Every assembly module starts with `def U(X, Y, Z): return (-X, -Z, Y)`, and a box of Unity size (SX, SY, SZ) is `kit.box((SX, SZ, SY), U(...), ...)`.

**Part frame** for small parts (lock parts, the key, tags, hosts, closer arms, plates):
- **Origin:** the part's pivot or mount point.
- **Front:** kit −Y = Unity **+Z**. It is the outward normal of the face the part mounts on, or the throw or insertion direction, as stated per part.
- The facade places parts on a leaf face like this:

| Placement | localRotation | localScale |
|---|---|---|
| hardware on face `_s` | `Euler(0, 90, 0)` | (1, 1, 1) |
| hardware on face `_p` | `Euler(0, −90, 0)` | (−1, 1, 1): keeps the handing, so the lever still points at the hinge |
| text parts (sign, number plate) on `_s` | `Euler(0, 90, 0)` | (`S_sign`, 1, 1): cancels the rig mirror |
| text parts on `_p` | `Euler(0, −90, 0)` | (`S_sign`, 1, 1) |
| edge parts (latchbolt, deadbolt) | identity: front +Z = the throw direction, door +Z | (1, 1, 1) |
| strikes (on the latch lining) | `Euler(0, 180, 0)`: front faces the leaf edge | (1, 1, 1) |

The face rows were worked through by hand for both handings. **They must be checked in a capture** with a mirrored door: §11 test T4.

### 1.3 A2: the pivot moves to the hinge knuckles (map change; part of Option A)

A butt hinge's axis is the knuckle centre beside the swing-side face, not the leaf's centre plane:
- **Axis (door root, S = +X): X = +0.0295, Z = +0.0098**, vertical.
- The knuckle barrel is R 0.0075. It spans X 0.022–0.037 and Z 0.0023–0.0173: tangent to the leaf's S face and standing just off the 0.002 lining. Nothing intersects.

**What the map changes (two lines, plus Option A's swing work):**
- `Door hinge` localPosition += `closed · (S_sign · 0.0295, 0, 0.0098)`.
- `Door leaf` (the collider cube) localPosition: (0, 1.04, 0.50) → **(−S_sign · 0.0295, 1.04, 0.4902)**. The collider's closed world pose is therefore **unchanged**. Its size stays 0.05 × 2.08 × 0.98.
- `LockPoint` is computed from `door.position` and `closed`, so it is unaffected by the move. It changes only as §6.4 asks.
- Sound reads the hinge by name and rotation (`SoundDirector:212`; `DoorSound` reads the rotation), so it is unaffected.

**Why A2 and not A1 (pivot left on the centre line):**
- With A1 the leaf's swing-side hinge corner digs into the hinge lining from about 7.5° open (computed).
- With A1, butt-hinge knuckles cannot sit on the axis, which is inside the leaf.
- At 95° the hinge end is buried about 30 mm in the S casing (`01` §1.6 F8; `04` frames 19–23).

**A1 fallback**, only if the map chat refuses A2:
- concealed pivots: caps at the head and foot, no knuckles;
- a leaf with a radius hinge stile, R 0.022 about (0, 0), in a lined slot with a black seal, as `04` §5.1 hinge row;
- stops and casing as below.

It is gap-free, but it loses the RE8 hinge read.

### 1.4 Section and gap closure (door root, S = +X, metres)

| Part | X | Y | Z | Note |
|---|---|---|---|---|
| **Lining** (2 mm skin over the wall end, trim face and soffit) | ±0.0795 | 0–2.098 | hinge −0.0005→**0.002**; latch **0.998**→1.0005 | Head skin Y 2.098→2.1005. It hides the coplanar reveal z-fight (`04` §1.1). |
| **Casing**, both faces, sleeve mode | S 0.0795→0.105; P −0.105→−0.0795 | jambs 0–2.175; head 2.098–2.175 | hinge −0.075→0.002; latch 0.998→1.075 | Encloses the map's 0.07 × 0.02 trims, which reach X ±0.10, Z −0.07 and Y 2.17 (`MapWorld:999-1009`). Head corners mitred. |
| **Stop**, push side only | **−0.0605→−0.025** (35.5 mm) | hinge/latch 0.012–2.098; head **2.082–2.098** | hinge **0.002→0.018**; latch **0.982→0.998** | 16 mm proud of the lining (SDI 5/8", `02` §2.2). Mitred at the head. Lands on the saddle. |
| **Saddle** (threshold) | ±0.076; flat top ±0.052; 1:2 bevels | 0→**0.012** | 0.002–0.998 | 12 mm, within ADA's ½" (`02` §2.3). Under both faces. |
| **Visual leaf** | **±0.022** (44 mm) | **0.015→2.095** | hinge edge **0.005**; latch edge **0.995** (S face) / **0.9927** (P face): a 3° lock-edge bevel | 1.5 mm × 45° arris chamfers on every face edge. The collider (±0.025, Y 0–2.08, Z 0.01–0.99) lies inside the visual leaf, except for 15 mm under its bottom, which is invisible behind the saddle. |
| **Hinges** | knuckles on the axis (0.0295, —, 0.0098) | **0.254–0.368, 1.0565–1.1705, 1.859–1.973** | — | 5-10-equal (`02` §4.3). The frame half (knuckles 1, 3, 5) is part of the frame mesh; the leaf half (knuckles 2, 4) is part of the leaf mesh. |

**Gaps when shut, and what closes each one:**

| Gap | Size | Closed by |
|---|---|---|
| Hinge stile | 3.0 mm (lining 0.002 → leaf 0.005) | the stop, Z 0.002–0.018, overlapping 13 mm at X −0.0605…−0.025 |
| Latch stile | 3.0 mm on the S face, 5.3 mm on the P face | the stop, Z 0.982–0.998 |
| Head | 3.0 mm (2.095 → 2.098) | the head stop, Y 2.082–2.098 |
| Floor | 3.0 mm above the saddle (0.012 → 0.015) | see the sightline note below |

**The path through every jamb or head gap is L-shaped:** across the 3 mm gap, then 90° through the 3 mm silencer gap between the leaf's P face (−0.022) and the stop face (−0.025). No straight line can follow it. This is `04` §3.1 applied to A.

**Floor.** A straight sightline under the 44 mm-deep leaf through a 3 mm gap needs a slope ≤ 3/44 (3.9°). From the 1.62 m eye that means standing 23 m away, where the slit is sub-pixel. A real ⅛" gap under a door is period-true (`02` §2.1), so I add no sweep.

**Test T1 (§11)** re-runs `04`'s ray harness on the real meshes, for both handings and both faces.

### 1.5 Swing clearance (computed, 0–95° in 0.1° steps, A2 axis)

Computed with a plan-section sampler (the leaf outline as 1,600 points, rotated about the A2 axis), against every static volume:

| Static part | Minimum clearance | Where / when |
|---|---|---|
| Push stop, hinge jamb | **2.8 mm** | the P-face hinge corner at 5.3° (it moves 0.2 mm toward the stop, then away) |
| Push stop, latch | 3.0 mm | at 0° |
| Hinge lining and wall | 3.0 mm | at 0° |
| Latch lining and wall | 3.0 mm | the S-face latch corner at 0.4° (the 3° bevel keeps the P corner ≥ 1.3 mm inside its arc) |
| S casing, latch side | 4.3 mm | at 3.3° |
| S casing, hinge side | **8.7 mm** | at 95°: the open leaf passes in front of the casing corner |
| Lever or knob, either face, vs every frame part | ≥ 26.7 mm | the P-face lever near the latch stop at 4° |

Head parts follow from the plan section, since the swing is about a vertical axis.

**Proud limits under A2.**
- At 95° the P face turns toward the opening and the S face toward empty room space. So in hinge terms the **only** clip zone is S-face hardware within about 0.15 m of the axis, where the S casing is.
- Kit rule:
  - **≤ 0.065 m proud** on both faces for locksets (the knob 0.065, the lever 0.064);
  - closer arms stand up to 0.20 m off the leaf when shut, **above 2.06 m only**;
  - the P2 exit device on the P face may stand 0.095 m.
- The map chat should confirm this reading of its "≤ ~0.07 per side" rule (`01` F8 asked the same).

### 1.6 What stays exactly as the map builds it (restating `00` in kit terms)

- **Colliders:** the leaf collider (0.05 × 2.08 × 0.98) and the window pane collider (1.4 × 1.65 × 0.03) are untouched except for A2's compensating offset.
  - Every kit asset calls `kit.no_collider()`, and the spawner passes `colliders: false`.
- **Names:** `Door hinge {a}-{b}` (pivot), `Door leaf` (collider; its `MeshRenderer.enabled = false`, never destroyed), `Window pane {a}-{b}` and `Key · zone {id}` keep their names.
  - Models go in as children.
  - No child name may start with `Door hinge`, equal `Light`, start with `fluorescent light`, or be `double door left/right hinge` (`01` F6).
  - Labels used: `Leaf rig`, `Door frame (kit)`, `Leaf (kit)`, `Leaf shadow`, `Lock *`, `Closer *`, `Door sign *`, `Window frame (kit)`, `Key ring`, `Key`, `Key tag`, `Key host (kit)`. The hinge halves are merged into the frame and leaf meshes, so no hinge object exists to misname.
- **Rendering:** no lights. Shared `Prop_*` materials (≤ 4 slots per asset, `kitlib.py:30-32`).

### 1.7 Shadow proxy (fixes the thin-leaf light leak, `04` §1.5)

- A shadows-only box, child of the `Leaf rig`: X ±0.05, Y 0.015–2.095, Z 0.02–0.98.
  - It is a primitive cube with its collider destroyed and `ShadowCastingMode.ShadowsOnly`, or a 12-triangle mesh from the facade.
  - It is not a kit asset: the importer has no shadows-only path.
- Name it `Leaf shadow`.
- If the 0.10 width shows in an open door's shadow, use 0.08.

### 1.8 Detail, normals, wear, LOD: rules for every door asset

**Hero LOD0** (0.3 m inspection; the head dip ends about 0.57 m from the lock, FOV 62°, about 1,600 px/m, audit §3.2):
- Every light-catching edge is bevelled: ≥ 1.0 mm on hardware and ≥ 1.5 mm on leaves and frames, with 2–3 segments.
- Curved parts get ≥ 48 segments on anything ≤ 0.07 m across, and ≥ 96 on roses, knobs and collars, so nothing facets at 0.3 m.

**Normals.**
- Weighted normals are wanted on every bevelled part.
- **Probe first** (G1, task 0): put a `WEIGHTED_NORMAL` modifier on one part before `finish()`, export, re-import, and compare the normals.
  - If custom normals survive the join, `shade_smooth()` and `set_sharp_from_angle()` (`kitlib.py:671-673`), every module may use the modifier per part, with no kitlib change.
  - If they do not, use support loops instead and report the smallest change: a module flag `WEIGHTED_NORMALS = True` that `finish()` applies after the join. **NEEDS APPROVAL.**

**Wear.**
- **W1 (now):** geometry-led.
  - The latch-stile arris is softer, with a 3 mm radius, where hands go.
  - Kick-zone edges are softened on the leaves.
  - Knurls and slotted screw heads are real geometry.
  - Grime comes from the slot textures' cavity channel (`_MaskMap` G, `FrontRoomsSurface.shader:17, 208`) and the world macro wear (`:189-197`).
- **W2 (NEEDS APPROVAL):** the surface shader reads a vertex colour: R = hand grime, G = edge wear, B = cavity.
  - Modules paint an `fr_wear` colour attribute on **every** part now (default white). It costs nothing until W2 lands and saves re-modelling.
  - Whether a joined mesh fills missing colours with white is UNVERIFIED, which is why every part gets the attribute.

**LOD.** Every module declares:
- `LOD1_RATIO`, `LOD2_RATIO`;
- `LOD_DISTANCES = (d01, d12, dcull)` in metres at `lodBias` 1 and FOV 76°;
- `kit.meta["lodDistances"]`, so the values reach the sidecar (`export()` copies `meta`, `kitlib.py:797`).

The current pipeline exports only `_LOD1`, and the importer sets LOD0→1 at 10 % of screen height and culls at 2–3 % (`FrontRoomsKitImporter.cs:67-79`). For a 0.067 m knob that cull happens at about 1.4 m.

So, **until the §8 change (P-1) is approved**:
- assets under 1.0 m set `LOD1 = None`, so no LODGroup is made and nothing is culled;
- assets of 1.0 m or more may set `LOD1 = LOD1_RATIO`;
- parts to drop at LOD2 get `obj["fr_lod2_drop"] = True`. kitlib ignores the flag today and uses it after the change.

Standalone defaults to Ultra (`lodBias` 2) and WebGL to High (`lodBias` 1) (`ProjectSettings/QualitySettings.asset:195, 301, 338-339`), so desktop switch distances are double the table's.

---

## 2. The door family (Red ii)

### 2.1 One grammar, eight members

**Shared by every member (the family grammar).**
- §1.4's section: casing envelope, 2 mm lining, 16 mm push-side stop, 12 mm saddle.
- The 44 mm visual leaf, with the same envelope and bevels.
- Three butts at the same heights on the same A2 axis.
- **Operating hardware centred at Y 1.000, Z 0.920** on both faces: the free lever, the locked keyway.
- Kick and armor plates 3 mm above the leaf bottom.
- Signs centred at Y 1.524 (60"), Z 0.500.
- The zone number plate at Y 1.000, Z 0.790, beside the cylinder toward the hinge (`05` §6.2).
- Hardware finish per level; no maker marks anywhere.

**FREE vs LOCKED rule** (`05` §1, measured in engine at 6/12/20 m):
- a **FREE** door has a warm wood leaf (albedo luminance ≈ 0.17–0.19);
- a **LOCKED** door has an **almond enamel steel leaf** (≥ 0.50; the tested almond is sRGB 205/197/176, ≈ 0.56) in a **dark-bronze frame** (0.03).
- Value carries the read through the Office grade's −22 % saturation. Hardware confirms it at 2–5 m (lever = bar, knob on a tall escutcheon = dot plus plate). Text is a ≤ 1.5 m detail.

| Code | Member (where) | Leaf asset | Frame asset | Lockset (both faces unless noted) | Plates and extras | Closer | Priority |
|---|---|---|---|---|---|---|---|
| **L0-F** | Lobby office door. Map doors whose Standard side is Level 0; stream Lobby and Shift | `Kit_DoorLeaf_Veneer` (`Door_Veneer`, brass) | `Kit_DoorFrame_Wood` (walnut casing, lining and stop; oak saddle) | `Kit_Lock_Rose` + `Kit_Lock_Lever` (brass); `Kit_Lock_Latchbolt_Bored`; `Kit_Lock_StrikeBored` (brass) | none (worn kick zone, §1.8) | no | **P1** |
| **L0-K** | Lobby stockroom door | `Kit_DoorLeaf_Steel` (almond enamel, stainless kick plates both faces) | `Kit_DoorFrame_Steel` (dark bronze, silencers, aluminium saddle) | `Kit_Lock_Escutcheon` + `Kit_Lock_Knob` + `Kit_Lock_CylinderShell` + `Kit_Lock_Plug`; `Kit_Lock_Deadbolt`; `Kit_Lock_Latchbolt_Mortise`; `Kit_Lock_StrikeMortise` (satin chrome) | `Kit_DoorSign` "EMPLOYEES ONLY" + `Kit_DoorNumberPlate`, both faces | no (an older back room) | **P1** |
| **OF-F** | Office door. Map doors whose Standard side is Office | `Kit_DoorLeaf_Veneer_Oak` (VARIANT: `Prop_WoodOak`, chrome) | `Kit_DoorFrame_Steel` | rose + lever (chrome), bored latch, bored strike | — | **yes** (S face) | **P1** |
| **OF-K** | Office storage door | `Kit_DoorLeaf_Steel` | `Kit_DoorFrame_Steel` | mortise set (chrome) | sign "EMPLOYEES ONLY" + number plate | **yes** | **P1** |
| **RN-F** | Run ward door (white hospital corridor; no map door yet) | `Kit_DoorLeaf_Ward` (wood-grain laminate; stainless armor plate on P, kick plate on S, push plate on P, offset D-pull on S) | `Kit_DoorFrame_Steel` | none: push and pull, no latch (`04` §5.3) | — | **yes** | P2 |
| **RN-K** | Run utility door | `Kit_DoorLeaf_Steel` (Red's option: `Kit_DoorLeaf_SteelLite` with a 4 × 25" wired lite) | `Kit_DoorFrame_Steel` | mortise set | sign "STAFF ONLY" + number plate | **yes** | P2 |
| **EX-F** | Exit door (cyan Exit; the true exit) | `Kit_DoorLeaf_Veneer_Oak` | `Kit_DoorFrame_Steel_Alu` (VARIANT: clear-anodised look) | `Kit_ExitDevice_Crossbar` on P; rose + lever on S (exit-device trim); bored latch and strike | `Kit_ExitSign` (lit EXIT) on the wall above the P face | **yes** | P2 |
| **EX-K** | Exit, locked | `Kit_DoorLeaf_Steel` | `Kit_DoorFrame_Steel_Alu` | mortise set | `Kit_ExitSign_Dead` (unlit) above P; number plate | **yes** | P2 |

**Which member the map picks.**
- **Level.** Every map door joins a Low zone (always Level 0) to a Standard zone (Level 0 or Office; `05` §2). The member is **Office if the Standard side is Office, otherwise Lobby**. The office fit-out installs the doors on its own boundary. Run and Exit apply when those zones get map doors, or for the title stream (P3; its 2.4 m double doors would need a pair version of these profiles).
- **Lock.** FREE or LOCKED comes from the map's per-door lock flag (`VISUAL_CHAT_TASKS.md` W5; `00` "Locked vs free doors"). An unlocked locked door keeps the LOCKED model (`00`).

**Why no panelled doors.**
- RE8's raised panels are a château's (`04` §2.2). The period-correct 1955–93 US commercial door is flush (`02` §2.1; IP canon "solid-core doors", `05` §3).
- RE8's relief is translated into:
  - the casing profile;
  - the lining depth;
  - the leaf's edge bands, seams and the 3° lock-edge bevel;
  - mortised hinge plates;
  - plates with bevelled, screwed edges;
  - the hardware itself.
- That keeps the leaf honest and gives the 0.3 m detail Red asked for.

### 2.2 Readability per level (value numbers)

Mean albedo luminance, measured from `Assets/Resources/Surfaces/Textures/*_A.png` (64×64-sample means, sRGB → linear, Rec. 709):

| Surface | Luminance | Used by |
|---|---|---|
| L0 wallpaper (Chevron) | 0.449 | Lobby walls |
| Office drywall | 0.426 | Office walls |
| Run hospital wall | **0.738** | Run walls |
| Exit wallpaper (Chevron Cold) | 0.419 | Exit walls |
| `Door_Veneer` | 0.186 | L0-F leaf |
| `Prop_WoodOak` | 0.175 | OF-F and EX-F leaves |
| `Prop_WoodLaminate` | 0.174 | RN-F leaf |
| Almond enamel (`05` tested) | ≈ 0.56 | locked leaves |
| `Painted_Metal` (fallback) | 0.613 | locked leaves |
| `Prop_SteelBrown` | 0.030 | steel frames |
| `Prop_WoodWalnut` | 0.036 | Lobby casing |

- Lobby and Office pass at 6, 12 and 20 m in lit, dim and Office-graded light (`05` §5.2: +1.05 to +1.7 stops against the free door; +2.25 to +6.6 stops against an open dark doorway).
- **Run and Exit are UNVERIFIED.**
  - In Run the white wall (0.74) sits above the almond leaf (−0.4 stop). The bronze frame must draw the outline, as it does in the Office (`05` §5.3 item 4).
  - Run's red emergency light keeps value relationships but removes hue.
  - **Test T6 (§11) re-runs `05`'s harness in Run and Exit before those members are signed off.**
- The Exit pair adds a lit versus dead EXIT sign. That is an emissive face, not a light, and reads past 20 m.

### 2.3 Frames and leaves: what each mesh contains (door root, S = +X)

**`Kit_DoorFrame_Wood`** (L0-F). Slots: `Prop_WoodWalnut`, `Prop_Brass` (frame hinge halves), `Prop_WoodOak` (saddle).
- **Casing, both faces.** A ranch casing with a back band, swept round three sides and mitred at the head.
  - Profile in (u, w): u = distance outward from the lining face; w = height above the wall face, X 0.0795.
  - Points: (0, 0) → (0, 0.0195) → R 3 mm round-over → (0.003, 0.0225) → (0.058, 0.0245) → 1.5 mm quirk → (0.0605, 0.0255) → (0.0735, 0.0255) → R 3.5 mm → (0.077, 0.022) → (0.077, 0).
  - **Hard rule:** w ≥ 0.0215 for u ∈ [0.0015, 0.0735], so it stays clear of the map trim face at X 0.100. The module asserts this.
  - Grain runs along each piece (`obj["fr_grain"]`).
- **Lining.** 2 mm skins (§1.4). Plinth blocks: none (no plinths in 1990 commercial).
- **Stop.** Applied wood stop, 35.5 × 16 mm, with a 3 mm round-over on the exposed leaf-side arris and a 1.5 mm chamfer on the room-side arris. Mitred at the head.
- **Saddle.** Oak, 0.152 × 0.012, 1:2 bevels, 1 mm eased edges, end-grain caps.
- **Frame hinge halves**, ×3: knuckles 1, 3, 5, each 0.0228 tall less 0.4 mm gaps, R 0.0075. Pin button tips Ø 0.0095 × 0.0035 domed at the top and bottom of each hinge. Plate 0.114 × 0.046 × 0.0034, mortised flush into the hinge lining at Z 0.002, spanning X −0.024 → 0.022. Four countersunk #12 slotted screws (Ø 0.0095 heads).
- **Anchors:** `strike` (0, 1.000, 0.998), `hinge_axis` (0.0295, 0, 0.0098) + `_dir` (0.0295, 0.10, 0.0098), `head_dust_a` (0, 2.098, 0.05), `head_dust_b` (0, 2.098, 0.95), `threshold` (0, 0.012, 0.50).

**`Kit_DoorFrame_Steel`** (all other members). Slots: `Prop_SteelBrown`, `Prop_Rubber` (silencers), `Prop_Chrome` (hinge halves), `Prop_Aluminium` (saddle).
- **One pressed-steel jamb section** (16 ga, 1.5 mm inside bend radii on every bend), swept round three sides with hairline mitres:
  - P face band (X −0.105) → return → lining/soffit (Z 0.002) → **formed stop** (§1.4) → opposite rabbet → S face band (X +0.105) → return.
  - Sleeve mode: face 0.077 (u 0 → 0.077), return to the wall face.
- **Silencers.** Three rubber bumpers, Ø 0.008 × 0.0025, on the latch stop face (X −0.025 → −0.0225) at Y 0.35, 1.30 and 1.90, Z 0.988.
- **Saddle.** Aluminium, 0.152 × 0.012, with 12 longitudinal flutes 0.8 mm deep at 6 mm pitch on the flat. Countersunk screws at 0.15, 0.50 and 0.85.
- **Frame hinge halves** as in the wood frame, in chrome.
- **Strike mortise prep:** a 0.032 × 0.200 pocket in the latch lining, centred Y 0.968.
- **Closer shoe mount:** `closer_shoe` anchor (0.125, 2.130, 0.100), on the S head casing face.
- **Anchors:** as the wood frame, plus `strike` = **(0, 0.968, 0.998)** for the mortise strike (the bored strike uses (0, 1.000, 0.998)), `closer_shoe`, and `exit_sign_p` (−0.080, 2.280, 0.500): on the P-side wall face, above the head casing.
- **VARIANTS:** `Kit_DoorFrame_Steel_Alu`: `Prop_SteelBrown` → `Prop_Aluminium`.
- **Optional P3 "true mode":** a 0.051 face and 0.011 return, used only if the map stops building door trims (§6.5).

**`Kit_DoorLeaf_Veneer`** (L0-F; VARIANT `_Oak`: `Door_Veneer` → `Prop_WoodOak` and `Prop_Brass` → `Prop_Chrome`). Slots: `Door_Veneer` (via `kitlib.register_slot("Door_Veneer", ...)`; the importer maps it to `Resources/Surfaces/Door_Veneer.mat`, `05` §6.1), `Prop_Brass`.
- **Flush slab** to §1.4's envelope. Faces carry metre UVs, grain vertical, which fixes the 2.08× veneer stretch (`01` §1.7).
  - 6 mm hardwood edge bands on both stiles, their own pieces with grain along Y and a 0.3 mm seam line on the edge face.
  - Top and bottom edges plain.
  - 1.5 mm × 45° arris chamfers on every face edge, 2 segments. A 3 mm radius on the latch stile's face arrises between Y 0.85 and 1.20 (hand wear, §1.8).
- **Bored latch faceplate** on the latch edge: 0.0286 × 0.0572 × 0.0015, square corners, two slotted screws, mortised flush, centred Y 1.000, at the edge's mid-bevel Z 0.9939.
- **Leaf hinge halves**, ×3: knuckles 2 and 4. Plate mortised flush into the hinge edge (Z 0.005), X −0.024 → 0.022, with 4 screws.
- **No lever bore:** the rose covers it.
- **Anchors:**
  - `rose_s` (0.022, 1.000, 0.920), `rose_p` (−0.022, 1.000, 0.920);
  - `latchbolt` (0, 1.000, 0.9939) + `_dir` (0, 1.000, 1.0939);
  - `latch_edge_bottom` (0, 0.015, 0.9939), `latch_edge_top` (0, 2.095, 0.9939);
  - `damage_latch` (0, 1.10, 0.9939);
  - `hinge_axis` + `_dir`;
  - `closer_mount_s` (0.022, 2.0275, 0.545).

**`Kit_DoorLeaf_Steel`** (all locked members). Slots: `Door_Enamel` (new surface, `05` §6.2; fallback `Painted_Metal`, existing; registered with `register_slot`), `Prop_Aluminium` (kick plates), `Prop_Chrome` (mortise front, hinge halves, screws).
- **Flush hollow-metal leaf** to §1.4's envelope.
  - A vertical **edge seam**: a 0.5 mm groove on each stile's edge face, X 0.
  - Square edges with 1.5 mm bevels. The 3° lock-edge bevel.
  - Faint top and bottom channel lines: 1 mm recessed lines, 10 mm in from the top and bottom edges.
- **Kick plates, both faces.** 0.00127 thick, Y 0.018 → 0.272, 45° bevelled edges.
  - Push face: Z 0.0305 → 0.9695 (leaf width less 2").
  - Pull face: Z 0.024 → 0.976 (less 1-1/2"); `02` §5.4.
  - Six #6 oval-head screws per plate, at 15 mm from the edges.
- **Mortise armor front** on the latch edge: 0.032 × 0.2032 × 0.003, centred Y 0.968. It has openings for the latchbolt (Y 0.9365) and the deadbolt (Y 1.000), plus two slotted screws.
- **Leaf hinge halves** in chrome.
- **Cylinder and knob bores** are covered by the escutcheon.
- **Anchors:**
  - `escutcheon_s` (0.022, 0.968, 0.920), `escutcheon_p` (−0.022, 0.968, 0.920);
  - `latchbolt` (0, 0.9365, 0.9939) + `_dir`, `deadbolt` (0, 1.000, 0.9939) + `_dir` (each `_dir` +0.10 along Z);
  - `sign_s` (0.022, 1.524, 0.500), `sign_p`;
  - `tagplate_s` (0.022, 1.000, 0.790), `tagplate_p`;
  - `kick_s` (0.0226, 0.145, 0.500), `kick_p`;
  - `latch_edge_*`, `damage_latch`, `hinge_axis`, `closer_mount_s`.
  - The keyhole and knob anchors live on the escutcheon (§3.1). The facade composes them; §6.4 lists the composed door-root values the shots read.

**`Kit_DoorLeaf_Ward`** (RN-F, P2). As the veneer leaf with `Prop_WoodLaminate`, plus, as static parts:
- a **stainless armor plate** on the P face, Y 0.018 → 0.880 (34");
- a **kick plate** on the S face, 0.254 high;
- a **push plate** on the P face, 0.102 × 0.406 × 0.0013, rounded corners, Y 0.850 → 1.256, Z 0.820 → 0.922;
- an **offset D-pull** on the S face: Ø 0.025 bar, 0.254 centres, 0.064 standoff, vertical, centred Y 1.000, Z 0.920.

There is no latch, rose or strike. Slots: `Prop_WoodLaminate`, `Prop_Aluminium`, `Prop_Chrome`.

**`Kit_DoorLeaf_SteelLite`** (RN-K option, P2; Red's call: a see-through hole, `05` §6.4).
- The steel leaf with a 0.102 × 0.635 visible lite, centred Y 1.575, Z 0.755.
- A steel lite kit on both faces: 0.019 face, 45° bevelled glazing stops.
- 6 mm glass: `Prop_Glass`. The wired-glass look needs `Glass_Wired` from the glass track.
- **Gameplay flag:** the Relay's sight stops at the collider (`02` §6.1).

### 2.4 Closers, exit device, signs and plates

**Regular-arm surface closer** (Office, Run and Exit members; S face; `02` §5.3: allowed once the door is single-acting).
- **`Kit_DoorCloser_Body`**, static on the leaf rig at `closer_mount_s`.
  - Body 0.29 (Z) × 0.065 (Y) × 0.050 proud: X 0.022 → 0.072, Y 1.995 → 2.060, Z 0.400 → 0.690.
  - Cast end caps, a cover seam, two slotted valve screws on the hinge end, a spindle cap.
  - **Spindle** (vertical) at (0.062, 2.060 → 2.075, 0.520). Anchor `spindle` (0.062, 2.068, 0.520).
- **`Kit_DoorCloser_Arm`** (main arm, a = **0.240**). Origin on the spindle axis; the arm lies in the plane Y 2.068–2.080. It rotates about Y. Anchor `elbow` at (a, 0, 0) part-local (Unity +X).
- **`Kit_DoorCloser_Forearm`** (b = **0.260**). Origin at the elbow, rising to the shoe pivot (+Y 0.050). It rotates about Y. Anchor `shoe_end`.
- **`Kit_DoorCloser_Shoe`**, static on the frame at `closer_shoe` (0.125, 2.130, 0.100). A 0.030 × 0.040 × 0.020 bracket on the S head casing, with two screws.
- **Linkage (checked numerically for 0–95°).**
  - The elbow is the intersection of the circles (spindle(θ), a) and (shoe pivot, b), taking the root **farther from the leaf's S-face plane**.
  - Closed, the elbow is at plan (X 0.222, Z 0.341): **0.20 m proud, above Y 2.06**.
  - At 45°: (0.385, 0.101). At 95°: (0.297, −0.095).
  - Both arms stay on the room side of the leaf and clear of the casing (X ≥ 0.14) at every angle.
  - The solver is a visual-chat component (§6.3) that reads the leaf rig each LateUpdate. It is render-only.
- **ESTIMATE:** the arm lengths and mount points come from the linkage solve, not from a catalogue template. Check them against an LCN 4010-type regular-arm template before final modelling (UNVERIFIED).

**`Kit_ExitDevice_Crossbar`** (EX-F, P face, P2; era note "push bars on exits").
- A rim crossbar exit device: a Ø 0.032 tube from Z 0.10 to 0.90 at Y 1.000, on two end cases (0.075 × 0.120 × 0.095 proud), with the latch case at the latch end.
- Origin at the latch-end case mount. The bar pivots 12 mm toward the leaf when pushed.
- 0.095 proud: allowed on the P face (§1.5).
- Slots: `Prop_Chrome`, `Prop_Aluminium`.

**`Kit_DoorSign`** (locked members; both faces; text; `05` §6.2).
- A two-ply engraved plate, 0.254 × 0.076 × 0.003, with 1 mm bevels and 4 oval-head screws.
- The text is a decal quad on **`Prop_SignEngraved`** (new slot, NEEDS APPROVAL). Its atlas cells are: 0 "EMPLOYEES ONLY", 1 "STAFF ONLY", 2 "STORAGE", 3 spare. The cell is set per renderer through a MaterialPropertyBlock on `_BaseMap_ST`; `FrontRoomsSurface.shader:183` applies `_BaseMap_ST` to mesh UVs.
- Lettering: TeX Gyre Heros Bold, 19 mm caps, ivory core on a dark-brown face (`05` §6.2). No Braille (pre-ADAAG) and no date.
- Fallback: the plate in `Prop_PlasticBlack` with no text.

**`Kit_DoorNumberPlate`** (locked members; both faces; text).
- A two-ply engraved plate, 0.100 × 0.050 × 0.003, rounded corners R 0.004, two screws.
- The colour family matches the zone's tag colour through VARIANTS: red, blue, white.
- Digits: a decal quad on **`Prop_KeyTagNo`** (new slot, NEEDS APPROVAL; a 10 × 10 atlas of "00"–"99", `03` §2.5), cell set by MaterialPropertyBlock.

**`Kit_ExitSign`** (EX-F) and **`Kit_ExitSign_Dead`** (EX-K; VARIANT that swaps the face slot).
- A 1990 stamped-steel wall exit sign: 0.330 × 0.200 × 0.060 housing with a stencil face, "EXIT" in 6" letters.
- The face uses the existing `Run_ExitSign` surface (emissive, red letters, as the Run level's signs, `Stream:1470-1479`) through `register_slot`.
- The dead variant needs a non-emissive copy of that surface, `Run_ExitSign_Dead` (new material, NEEDS APPROVAL).
- **No Light component** (`00`).
- Mounted on the wall above the P-face head casing at `exit_sign_p`. Its top at 2.38 m is under the Low ceiling of 2.4 m.
- Red's call: red letters, or green to suit the cyan Exit. Green needs one more emissive surface.

---

## 3. The keyable lock and the key (Red iii)

### 3.1 Lock parts, pivots and motion

**Function.** A mortise lock.
- A key in either face's IC cylinder throws or retracts a **25 mm deadbolt**.
- Either knob retracts the **deadlocking latchbolt**: a passage knob, so the deadbolt is what locks the door.
- Real storeroom hardware is keyed on one side only. Keying both faces is a game liberty (`02` §3.3), because the key works from the side the player stands on (`00`).
- Period:
  - mortise locks "widely installed in industrial, commercial, and institutional environments" (`02` §4.4, Wikipedia [read there]);
  - knob trim was standard before the ADA (`02` §3.1);
  - IC cylinders: Best, since the 1920s (`02` §13); the era note.
- No brand marks.

**Static parts:** the escutcheon and cylinder shell (leaf-rig children, face placement per §1.2) and the strike (frame child).
**Moving parts:** the knob, plug, deadbolt and latchbolt (leaf-rig children); the key.

Every part file stores its motion in `kit.meta["motion"]` (copied to the sidecar), for example `{"type": "rotate", "axis": [0,0,1], "min": -40, "max": 40}` or `{"type": "slide", "axis": [0,0,-1], "travel": 0.025}`.

| Asset | Origin (part frame, §1.2) | Geometry (hero LOD0) | Motion |
|---|---|---|---|
| `Kit_Lock_Escutcheon` | centre of its back face, on the leaf face; front = outward | Plate 0.0572 (X) × 0.2032 (Y) × 0.0020, 1.0 mm bevelled edge, 3 segments. Knob boss Ø 0.030 × 0.006 at Y −0.0315. Two oval-head **slotted** screws (Ø 0.0070 head, 1.2 mm dome, 0.8 mm slot) at Y ±0.088. A shallow 0.3 mm recess ring for the cylinder collar at Y +0.032. Anchors `knob_pivot` (0, −0.0315, 0.002), `cylinder` (0, +0.032, 0.002) (the collar seat), `keyhole` (0, +0.032, **0.0095**): the shell and plug origins go here, 0.0095 off the leaf face, which is door X ±0.0315. | static |
| `Kit_Lock_CylinderShell` | **the keyhole**: core-face centre on the plug axis; front = outward | Cylinder ring (collar) Ø 0.044, Z −0.0075 → −0.0015, domed front with a 1 mm radius. Housing face Ø 0.035, Z −0.0015 → 0. **IC figure-8 core face**, flush at Z 0: lower lobe Ø 0.0127 centred on the axis, upper lobe Ø 0.0127 centred Y +0.0095 (the pin-stack lobe: the "pin chambers suggested"), with a 0.25 mm groove round the figure-8. Plug bore Ø 0.0118. Control-lug notch at the top of the upper lobe. No brand. (Core dimensions ESTIMATE, UNVERIFIED against a real SFIC.) | static |
| `Kit_Lock_Plug` | the keyhole (same point as the shell); front = outward | Plug face Ø 0.0115 at Z 0, 0.3 mm chamfer, with a 0.5 mm-wide shear line visible against the core. **Keyway slot** cut through the face (profile §3.2) and running **0.027 deep** as an open cavity with dark walls, so the blade can be seen going in. **Six pin tips** (Ø 0.0029, domed) hang into the slot top at Z −0.0040, −0.0078, −0.0116, −0.0154, −0.0192, −0.0230. | rotate about part Z, **0 → 90°** with the key; returns with it |
| `Kit_Lock_Knob` | spindle axis on the escutcheon face (door X ±0.024); front = outward | Lathe, 96 segments. Shank Ø 0.019 (Z 0 → 0.018). Ball knob Ø **0.054**, face at Z **0.063**, so it is **0.065 proud** of the leaf face. A **knurled band** (72 straight flutes, 0.6 mm deep, Z 0.040 → 0.054): the UFAS tactile mark, `02` §3.1. A 0.8 mm crown chamfer. | rotate about part Z, **±40°** (it retracts the latch) |
| `Kit_Lock_Deadbolt` | on the lock front surface, bolt axis (door (0, 1.000, 0.9939)); front = throw (door +Z) | Bolt 0.0125 (X) × 0.030 (Y), square end, 1 mm chamfer, two hardened-insert dots on the end face. The modelled length 0.045 extends back into the leaf. | slide along part −Z, **0.025** (thrown ↔ retracted) |
| `Kit_Lock_Latchbolt_Mortise` | lock front, bolt axis (door (0, 0.9365, 0.9939)) | Latch 0.0125 × 0.030, throw **0.019**, 30° bevel facing **P** (part −X before mirroring), with an auxiliary deadlatch plunger 0.005 × 0.018 beside it, fixed | slide −Z 0.019 |
| `Kit_Lock_Latchbolt_Bored` (free doors, EX-F) | faceplate surface, bolt axis (door (0, 1.000, 0.9939)) | Latch 0.010 × 0.020, throw **0.013** (½", `02` §4.1), 30° bevel facing P, deadlatch plunger 0.004 × 0.012 | slide −Z 0.013 |
| `Kit_Lock_Rose` (free doors) | back-face centre on the leaf face; front = outward | Stepped lathe, 96 segments: flange Ø **0.070** × 0.003 with a 1 mm round, a cove, then a boss Ø 0.030 to Z **0.010**. Two oval-head slotted screws at Y ±0.025. Anchor `spindle` (0, 0, 0.010). | static |
| `Kit_Lock_Lever` (free doors) | spindle axis on the rose front (door X ±0.032); front = outward | Hub Ø 0.024 (Z 0 → 0.012), neck to Z 0.040. The grip runs along **part +X** (= toward the hinge on both faces after placement) to **0.121**, section 0.020 × 0.014 with soft radii, returning toward the door over its last 0.015. Front at Z 0.054, so **0.064 proud**. | rotate about part Z; "down" = the grip end moves −Y; **35°** (Open), spring back |
| `Kit_Lock_StrikeMortise` | plate centre on the latch lining (door (0, 0.968, 0.998)); placed with `Euler(0, 180, 0)`; front faces the leaf edge | Plate 0.032 × 0.200 × 0.0016, mortised flush. Openings for the latch (part Y −0.0315) and the deadbolt (+0.032), each with a dark dust box 0.015 deep. Curved lip toward door **+X** (= part −X), ≤ 1 mm curl. Two countersunk screws. | static; it detaches on the Relay break (audit §3.7) |
| `Kit_Lock_StrikeBored` | door (0, 1.000, 0.998), as above | ANSI curved-lip strike 0.032 × 0.124 (`02` §4.1), one opening, dust box, two screws | static; detaches |

**Clearances.**
- The lip's ≤ 1 mm curl leaves ≥ 2 mm to the swinging S-face latch corner (§1.5: 3.0 mm before the lip).
- The thrown deadbolt and latch enter the strike and the wall end. They are hidden, render-only.

**Finishes.**
- Lobby free: `Prop_Brass` (US3/US4).
- Every other member: `Prop_Chrome` (US26D).
- VARIANTS: `_Brass` / `_Chrome` on the rose, lever, strikes and the leaves' hardware slot.
- Locked hardware stays chrome in every level (`05`: chrome against brass is a 2–5 m cue in the Lobby).

### 3.2 The keyway profile (one profile for the key and the plug)

**Key-local frame:** origin on the turning axis at the shoulder; X across the flats; Y up, with the cuts up (US pins-up, `02` §4.2, UNVERIFIED convention); Z toward the tip.

**Blade cross-section** (metres; counter-clockwise; Z-extruded from 0 to 0.025):

```
(-0.0010,-0.0040) (+0.0010,-0.0040)            spine, 0.2 mm chamfers on both corners
(+0.0010,-0.0020) (+0.0006,-0.0020) (+0.0006,-0.0008) (+0.0010,-0.0008)   right-flat groove, 0.4 deep
(+0.0010,+0.0046) (-0.0010,+0.0046)            bitting edge (cuts are subtracted from here)
(-0.0010,+0.0018) (-0.0006,+0.0018) (-0.0006,+0.0006) (-0.0010,+0.0006)   left-flat groove, 0.4 deep
```

**Keyway slot in the plug** = the blade section offset outward by **0.15 mm**:
- walls at X ±0.00115;
- a ward rib on the +X wall at Y −0.00185 → −0.00095, reaching X +0.00075;
- a ward rib on the −X wall at Y +0.00075 → +0.00165, reaching X −0.00075;
- Y from −0.00415 to **+0.0048**. The top is open to the pin tips.

**Bitting** (6 cuts, ESTIMATE in the IC-system manner):
- Cut centres at Z **0.0040, 0.0078, 0.0116, 0.0154, 0.0192, 0.0230** (0.0038 ≈ 0.150" pitch).
- Each cut is a 0.0008 flat with 50° flanks.
- Depths below Y +0.0046, in 0.00038 steps (rule-15-style, `02` §8.1 [search]): **[2, 4, 1, 5, 3, 2] × 0.00038 + 0.0002**.
- One bitting for every zone key: the number tag is the identity, not the cuts.
- The tip bevels from Z 0.0215 down to Y +0.0005 at Z 0.025; 0.5 mm nose radius at the spine.

The plug's pin tips hang at the same Z values, each 0.2 mm above its cut floor once the key is fully in.

### 3.3 `Kit_Key_Zone` (and the nickel variant)

| Item | Value |
|---|---|
| Origin | **the shoulder, on the turning axis** (part-local (0, 0, 0)). At full insertion the origin equals the lock's `keyhole` anchor |
| Axes (Unity) | +Z = insertion direction (toward the tip; kit front = Blender −Y); +Y = cuts up; X = the flat normal |
| Blade | Z 0 → **0.025** (= `Unlock.InsertDepth`), 2.0 mm thick, section §3.2, 6 cuts |
| Shoulder | the bow neck, Z −0.003 → 0, Y −0.0045 → +0.0085. Its front face at Z 0 above the blade is the stop that seats on the core face |
| Bow | Z −0.003 → −0.033; Y −0.0125 → +0.0135 (0.026 wide); a generic rounded "paddle" outline, R 0.008 corners with a 1 mm flare at the neck (no maker's outline); 2.2 mm thick; 0.4 mm edge rounds in 3 segments (the glint, `03` P5); ring hole Ø 0.0048 at (0, 0.0005, −0.0265) with 0.3 mm chamfers |
| Length | **0.058** overall (`02` §8.1: 55–60 mm) |
| Anchors | `shoulder` (0, 0, 0), `tip` (0, 0, 0.025), `insert_dir` (0, 0, 0.10), `cuts_up` (0, 0.10, 0), `grip` (0, 0.0005, −0.018), `ring_hole` (0, 0.0005, −0.0265), `ring_hole_dir` (0.10, 0.0005, −0.0265) |
| One mesh | Bow and blade never move apart, so the key is one rigid asset. The "separable parts" the audit needs are the anchors above. A posed hand (audit §3.0) would attach at `grip` |
| Slots | `Prop_Brass` (a cut brass duplicate, `02` §8.1). VARIANT `Kit_Key_Zone_Nickel`: `Prop_Brass` → `Prop_Aluminium` (the satin nickel-silver look; for a later master key) |
| Stamping | "DO NOT DUPLICATE" plus a keyset code on the bow: W2 or a normal map later. Not in v1 |
| Budget | LOD0 **2,400**, LOD1 900, LOD2 150; LOD distances 1.0 / 3.0 / 12 m |

### 3.4 Shot mapping (`FrontRoomsShotTimings.Unlock`, Red's head dip)

All positions are door-root coordinates. For `S_sign` = −1, mirror X; for an open leaf, go through the leaf rig's transform.

| Beat (s) | Part | Motion |
|---|---|---|
| framing pose P | camera | `keyhole_f + out_f·0.45`, eye 1.37 m, looking at the keyhole. For the pull face: keyhole_s (0.0315, 1.000, 0.920), out = +X, so the camera is at about (0.4815, 1.37, 0.92) and pitches **≈ 39.4° down** (audit §3.2: about 38°) |
| 0.30–0.55 `KeyApproach` | key | from lower right to the shoulder 0.055 out from the keyhole, so the tip is 0.030 out. Rotation `LookRotation(−out_f, up)`, with the cuts on the key's +Y up |
| 0.55–0.68 `Insert` | key | the shoulder slides `keyhole + out·0.025` → `keyhole` (`InsertDepth` .025); 30 ms catch at 60 % |
| 0.68–0.90 `Turn` | key + `Kit_Lock_Plug` | both rotate **90°** about the keyhole axis, the first 10° slow (`TurnResistDeg`). Direction: the key's +Y moves toward the hinge (door −Z). The real direction depends on handing (UNVERIFIED, `05` §9) |
| 0.86 `Commit` | `Kit_Lock_Deadbolt` | retracts 0.025 in 0.08 s (`BoltJolt`); `DoorUnlocked` fires |
| 0.86 | leaf rig | shifts **1.5 mm** (`LeafShiftMetres`) toward +X, settling off the stop; render-only (§6.5 item 6) |
| 0.90–1.15 `KeyOut` | key + plug | key and plug turn back to 0° (a key only leaves at 0°); the key withdraws |
| **0.92–1.00 (NEW)** | `Kit_Lock_Knob` (opener's face) + mortise latch | the knob turns 40° and the latch retracts 0.019. Proposed constants: `KnobTurnStart .92`, `KnobTurnEnd 1.00`, `KnobDeg 40` |
| 1.00–1.25 `Ajar` | hinge (map) | to 10° plus a 1° overshoot. `DoorSound` already fires Handle and Unlatch when the leaf leaves closed (`DoorSound.cs:89-92`) |
| **1.20–1.35 (NEW)** | knob + latch | return. Proposed: `KnobReturnStart 1.20`, `KnobReturnEnd 1.35` |

**Pull-face caution (Option A).** Unlocked from the pull side, the leaf pops ajar **toward** the camera. At 10° the cylinder moves about 0.16 m closer: from 0.45 m to about 0.29 m. Either start the camera return at 1.00 on the pull side, or use 5° there. The map chat owns the shot code.

**Rattle (locked, no key).** The knob turns 40° in 0.05 s (the latch retracts). The leaf rig jolts 2 mm toward the opener against the thrown deadbolt, twice (`Jolt1`, `Jolt2`). The knob returns.
- On locked doors this replaces the lever's 20° (`Rattle.LeverDeg`). Proposed: `Rattle.KnobDeg 40`.
- This supersedes `05` §7's fixed-knob 3–5° twist: our locked function is a passage knob plus a deadbolt, not a storeroom knob.

**Open (free doors).** The lever goes down 35° in 0.08 s, and the bored latch retracts 0.013 with it. Spring back at 0.12–0.22 (`Open`).

**Relay break (audit §3.7).** Locked doors break with the deadbolt thrown. On the final blow the strike detaches, as a rigid render-only part, and the leaf throws. Damage variants (`_Dmg1` / `_Dmg2`, audit §3.7) are **not** in v1 (P3, open item §13).

---

## 4. Keys: ring, tags, hosts, placement, readability

### 4.1 The key assembly

`Key · zone {id}` becomes an **unscaled empty** (map change; today it is the scaled cube, `MapWorld:1953-1972`). It is the pickup point: horizontal ≤ 0.9 m (`00`). The facade parents three assets under it:

| Asset | Origin and axes (part frame) | Geometry (hero LOD0) | Slots | Anchors | Budget LOD0/1/2 |
|---|---|---|---|---|---|
| `Kit_KeyRing` | the **top inner point** of the ring (where it sits on a hook); the ring lies in the part XY plane, front +Z = ring normal | A flat-wire split ring, two turns: Ø 0.025 outside, 1.6 × 0.9 mm section. A helix of 128 segments per turn with an 8-sided section. Both ends cut at 30° (the split shows) | `Prop_Chrome` | `hook_contact` (0, 0, 0); `key_contact` (−0.004, −0.0215, 0); `tag_contact` (+0.004, −0.0213, 0) | 2,000 / 600 / 96 |
| `Kit_Key_Zone` | §3.3 | §3.3 | `Prop_Brass` | §3.3 | 2,400 / 900 / 150 |
| `Kit_KeyTag_Rect` | the tag's **ring hole** (its swing pivot); hangs along −Y; front +Z | **A plastic tag with a paper insert** (era note): body 0.057 × 0.029 × 0.0042, R 0.005 corners, 0.6 mm edge bevels; a ring tab with a Ø 0.005 hole; a recessed window 0.040 × 0.019, 0.8 mm deep, with a 0.6 mm retaining lip; the insert is one quad | body `Prop_PlasticRed` (VARIANTS `_Blue`: `Prop_PlasticBlue`; `_White`: `Prop_PlasticWhite`); insert `Prop_KeyTagNo` (NEW; fallback `Prop_Paper`, blank) | `hole` (0, 0, 0), `face` (centre of the insert), `number` (= face) | 1,200 / 400 / 60 |
| `Kit_KeyTag_Round` | as above | Ø 0.038 body, window Ø 0.026 | as above | as above | 1,200 / 400 / 60 |
| `Kit_KeyTag_Long` | as above | 0.076 × 0.022 "valet" body, window 0.050 × 0.014 | as above | as above | 1,200 / 400 / 60 |

**The insert's number.**
- Typed in **Courier Prime** (`FONTS_PERIOD_1990.md:46`, the typewriter row), two digits.
- One `Prop_KeyTagNo` cell per number, 00–99, on a paper ground. Hand-lettered numbers are a second 10 × 10 page if 平面视觉 wants them.
- The cell is set per renderer by a MaterialPropertyBlock on `_BaseMap_ST` (`FrontRoomsSurface.shader:183`). The SRP Batcher is broken only for these renderers.

**Identity** (`03` §2.5):
- 3 shapes × red, blue, white = **9 identities** with existing slots (12 with a `Prop_PlasticGreen`, which is optional and new). No yellow, orange or manila tags (`03` §1.2).
- The number comes from the zone hash.
- **The locked door's number plate shows the same number and colour** (§2.4).
- The map picks shape and colour so that two zones sharing a door never share both.
- HUD: "KEY 14" instead of "LEVEL 0 KEY" (`03` §2.5; the map chat's HUD).

**Poses** (the facade builds them from the anchors; no extra meshes):
- **hung:** the ring's `hook_contact` sits on the host's `key_hook`. The key hangs from `key_contact` by its `ring_hole`, tip down, flats facing the room (key X = room normal). The tag hangs from `tag_contact`, face to the room.
- **flat:** the key lies flats-down on the surface, with the ring and tag beside it.
- **over-edge:** the key lies just behind a support's front edge, and the tag hangs down the front face (`03` §2.2).
- Hung keys **do not spin and have no emission** (`00`; the map already keeps spot keys still, `MapWorld:155, 1973`).
- Optional: a single 1 s sway after a door slams within 3 m, from `DoorMoved` (`03` P6).

### 4.2 Hosts (render-only; heights baked in)

**Shared convention for wall hosts** (`03` §2.3):
- origin on the **wall face plane, at floor level, centred**; front +Z faces the room;
- tags `interactable`, `key_host`, `wall_decor`;
- `kit.no_collider()`;
- proud ≤ 0.10;
- LOD distances 3 / 8 / 40 m.

| Asset | What it is (period) | Geometry (hero LOD0) | Slots | Anchors | Budget |
|---|---|---|---|---|---|
| `Kit_KeyBoard` (Lobby default) | A dark painted key board with brass cup hooks. Timeless 1950–2000 (`03` §2.3) | Board 0.30 × 0.40 × 0.018, top at **1.65**, 3 mm round-overs. Two rows × 4 brass cup hooks at Y **1.54 / 1.42**, X ±0.035 and ±0.105: Ø 0.003 wire, screw shank, open cup 0.020 proud with the tip up. A paper number strip under each hook (cells 01–08 via `uv_rect` on `Prop_KeyTagNo`; fallback blank `Prop_Paper`). Two slotted mounting screws | `Prop_WoodDark` (board, lum 0.014), `Prop_Brass`, `Prop_KeyTagNo`/`Prop_Paper` | `hook_0`…`hook_7` (the inside bottom of each cup, Z ≈ 0.036 from the wall); `key_hook` = `hook_6` by default (the map may pick any hook per seed) | 4,000 / 1,400 / 150 |
| `Kit_KeyCabinet` (Office default) | A steel wall key cabinet, door swung about 175° flat to the wall (`03` §2.3; Telkee type, no brand) | Body 0.36 × 0.46 × 0.08, Y 1.24–1.70. Door 0.36 × 0.46 × 0.02 hinged on the left and lying open. A light back panel with 4 rows × 6 chrome hooks on hook strips. An index card in the door (blank ruled `Prop_Paper`). A cam lock on the door with an IC face (the same figure-8 language). 1 mm folded edges | `Prop_SteelAlmond`, `Prop_PlasticWhite` (back panel), `Prop_Chrome`, `Prop_Paper` | `hook_r{0..3}_c{0..5}`; `key_hook` = `hook_r1_c4`; `door_hinge` | 7,000 / 2,400 / 250 |
| `Kit_KeyHook` (≤ 1 key in 4; red or blue tag only) | A single brass cup hook in the wall | Shank and cup, cup inside bottom at Y **1.476**, Z 0.018 | `Prop_Brass` | `key_hook` (0, 1.476, 0.018) | 600 / 200 / — (cull at 12 m) |

### 4.3 Placement: wiring to the map's `KeySpot` markers

The map already has the hook for this:
- the Level Designer's `KeySpot` marker carries x, z, y, yaw and **`host`**: "the kit asset the key hangs on or lies on (the visual chat's key hosts). Empty: the default" (`FrontRoomsRoomModuleData.cs:66-78`);
- `PlaceKeySpot` copies it into `chunk.keyHost` (`FrontRoomsMap.cs:679-714`);
- `SpawnKey` places spot keys still (`MapWorld:1946-1974`).

**Contract:**

| Case | Marker | What spawns | Key pose |
|---|---|---|---|
| A. Wall host | `host` ∈ {`Kit_KeyBoard`, `Kit_KeyCabinet`, `Kit_KeyHook`}. (x, z) is **on a wall face**; yaw = the wall's outward normal (0 = +Z); y ignored (0) | the host at (x, 0, z), yaw; render-only | `Key · zone` at the host's `key_hook`; **hung** |
| B. Surface spot | `host` empty; y = the surface height under the marker (desk 0.74–0.76, cabinet top 1.20, floor 0) | nothing extra | **flat**. **Over-edge** if within 0.05 m of a support's front edge (`03` §2.3 B) |
| C. No module spot (procedural rooms) | — | today: spinning at 1.05 m over the cell centre (`MapWorld:1967-1970`). **Replace** with `Kit_KeyHook` on the cell's longest plain wall stretch (≥ 0.6 m, ≥ 0.35 m from openings, outside keep-clear), facing the main entry. If there is none, the key lies flat on the carpet under the lamp, tag up. **Never spinning, never floating** | hung or flat |

**Rules for every case:**
- `key_hook` or the rest point must be within **0.55 m horizontally** of floor a 0.3 m capsule can stand on (`03` §2.2 item 4).
- Hook heights are 1.40–1.55 m.
- **Lobby** prefers `Kit_KeyBoard`: a dark board on yellow paper.
- **Office** prefers `Kit_KeyCabinet` on a perimeter wall, never behind 1.52–1.65 m cubicle panels. Pass a (host width + 0.6) × 1.0 m keep-clear to the dresser (`03` §2.4).
- **The key cell's lamp is forced Steady** (`03` §1.4 P2; map).

**Readability** (the emissive cube is replaced):
- No emission, no spin, no sprite.
- The glint is the lamp's highlight on ≥ 0.4 mm rounds and ≥ 1 mm bevels, with `Prop_Brass` smoothness 0.6 (`Prop_Brass.mat:59-66`).
- The host carries the read at 6–12 m (`03` §1.1: a 0.30 m board is recognised to 26 m; a 60 mm key only to 5 m).
- The "Item glint" accessibility option is Red's call (`03` P5).
- **No key collider** (`00`). "E · TAKE KEY" (audit §3.1) needs one and is not part of this kit.

---

## 5. Windows (Red iv; fracture excluded by Red vi)

### 5.1 Family and frame

`06` is adopted as written, with final names. Every member uses the door grammar:
- the same casing envelope: 0.0755 wide, 0.025 proud, enclosing the map's trims;
- the same linings;
- **16 mm glazing stops on both faces, all four sides.**

**Window root W:**
- the map's proposed unscaled `Window {a}-{b}`, at the opening centre on the wall line at floor level (= `Window.position`; `01` §2.5);
- the opening is X ±0.700, Y 0.350–2.000;
- the wall faces are at Z ±0.080 and the map trims reach Z ±0.100;
- **face A** (kit front, Unity +Z) is turned by the map toward the **non-tall** cell (`06` §3.0).

**Blender:** (x, y, z) = (−X, −Z, Y), as for doors.

| Code | Asset | Member | Frame | Slots | Budget LOD0/1/2 | Priority |
|---|---|---|---|---|---|---|
| W-L0 | `Kit_WindowFrame_Wood` | Lobby: an older back-office light (1955–85) | Walnut jamb liner; ranch casing with back band on both faces (the same profile as `Kit_DoorFrame_Wood`, §2.3); **through-stool with horns** on both faces, X ±0.800, Z ±0.121, Y 0.3255–0.3505, half-round nosings; **apron** on both faces; 16 × 16 mm wood stops with a 3 mm ovolo, mitred | `Prop_WoodWalnut` (+ `Prop_Rubber` for the glazing compound line) | **2,600** / 1,000 / 200 | P1 |
| W-OF | `Kit_WindowFrame_Steel` | Office: an SDI borrowed-light frame | Pressed steel, one section round all four sides, 1.5 mm bend radii, hairline mitres. **Integral stop on face B** (hall); **removable channel stop on face A** with **30 oval-head slotted screws** (8 per jamb, 7 on head and sill; ≤ 9" centres, ≤ 2" from corners; `06` §3.2); 1.5 mm black glazing-tape line | `Prop_SteelBrown`, `Prop_Rubber` | **3,600** / 1,300 / 220 | P1 |
| W-RN | `Kit_WindowFrame_Steel_Enamel` (VARIANT: `Prop_SteelBrown` → `Door_Enamel`, fallback `Painted_Metal`) | Run: a corridor wire light | the W-OF mesh | as W-OF | as W-OF | P2 |
| W-EX | `Kit_WindowFrame_Alu` | Exit: an aluminium office front | Clear-anodised wrap casing (US4443984 manner, `06` §2.1) with a 6 mm shadow groove 0.040 from the opening edge; snap-in 16 × 16 beads with a 45° bevel; 3 mm black vinyl gasket wedge on both faces | `Prop_Aluminium`, `Prop_Rubber` | **2,800** / 1,000 / 200 | P2 |
| — | `Kit_MiniBlind_Raised` (face A of Office windows, by edge hash; optional) | a 1" aluminium mini-blind, **raised**, generic Riviera type (`06` §2.5, §3.2) | Head rail 0.035 × 0.035 × 1.55 at Y 2.160–2.195, Z 0.110–0.145, on box brackets; slat stack Y 2.035–2.160 (12 grouped slabs with real slat edges at the ends); bottom rail Y 2.006–2.035; Ø 8 mm hex tilt wand from X +0.72 down to Y 1.14; lift cords and tassel at X +0.74 down to Y 1.20; ladder strings at 22 mm pitch at the stack ends | `Prop_SteelAlmond`, `Prop_PlasticWhite` | 2,400 / 800 / 120 | P1 (option) |
| — | `Kit_MiniBlind_Lowered` | decor only, for `Kit_InteriorWindow` (G5) | 2.40 m wide, lowered, half-tilted, 1" slats as real geometry | as above | 6,000 / 1,500 / 200 | P2 |

**Rules.**
- **Never a lowered blind on a breakable map window**: it would hang in the opening (`00`).
- One deliberately bent slat at the same index in every Office blind is a "countable wrongness" (HR03 via `05` §1). It is Red's call.
- LOD distances 4 / 12 / none for the frames, 3 / 10 / 30 for the blinds.
- Tags: `interactable`, `window`, `frame_wood` / `frame_steel` / `frame_alu`. The frame tags are for the sound chat's `ClimbSill` and `ToothSnap` (`06` §3.5).

### 5.2 Section (S1 stop band, sleeve mode; all members; window root, metres)

| Item | Jambs (X) | Head (Y) | Sill (Y) | Across (Z) |
|---|---|---|---|---|
| Opening (wall cut) | ±0.700 | 2.000 | 0.350 | wall ±0.080 |
| Lining or soffit, visible face | ±0.6995 | 1.9995 | 0.3505 (the stool top on W-L0) | ±0.1005 wood / ±0.105 steel and aluminium |
| **Stop line = sight line** (exposed glass edge) | **±0.6835** | **1.9835** | **0.3665** | stops at Z ±(0.006 → 0.022) |
| **Glass edge in the pocket** | ±0.6955 | 1.996 | 0.354 (on 3.5 mm setting blocks) | glass Z **±0.003** |
| Face band / casing | 0.6995 → 0.775 | 1.9995 → 2.075 | W-L0: stool and apron; others: 0.2745 → 0.3505 | 0.080 → 0.105 |
| Glass bite (edge hidden behind the stop) | **12.0 mm** | **12.5 mm** | **12.5 mm** | — |

- Nothing visible sits inside X ±0.6835 × Y 0.3665–1.9835 except glass.
- The map trims lie wholly inside the sleeve.
- **The S1 stop band** (16 mm of render-only stop inside the opening edge) **needs the map chat's rule clarification** (§6.6; `06` §4.1).
  - If it is refused, use **S0**: a flush lined reveal with a dark 12 mm gasket line, no stops. The structure read is lost.

### 5.3 What the window kit does not do

The window kit does not provide:
- the visible pane;
- crack stages;
- pre-fracture;
- shards;
- teeth;
- floor glass;
- the Glass material.

All of these belong to the glass-destruction track (`../glass/destruction/04_unity_implementation.md` §4, §8.4: "Intact pane (6 mm, chamfered rim)" and the stage meshes are generated there).

### 5.4 The interface the glass track's fracture set must fit (Red vi)

| Interface item | Value (window root) | Notes |
|---|---|---|
| **Pane origin and frame** | the window root W: (0, 0, 0) at the opening centre, wall centre line, floor; +Z = face A | The glass track hangs everything from the unscaled root, never from the scaled pane cube that `Kill` destroys (`04_unity_implementation.md` §4) |
| **Pane (intact 6 mm slab)** | **1.391 × 1.642 × 0.006**, centred **(0, 1.175, 0)**, glass Z ±0.003 | Sits wholly inside the 1.4 × 1.65 × 0.03 pane collider. Anchor `glass_slab`; size in `meta["glassSlab"]` |
| **Pane-space metres** (the glass track's UV0) | u = X + 0.6955, v = Y − 0.354 (0 … 1.391, 0 … 1.642) | One frame for cracks, smudge, dust and pieces |
| **Rebate (pocket) depth** | 12.0 mm at the jambs, 12.5 mm at head and sill, from the stop line to the glass edge; a further 4.0 mm (jambs) / 3.5 mm (head, sill blocks) to the soffit | Every tooth's root is drawn inside the pocket, so it reads as held by the stop |
| **Bead or stop profile and position** | 16 × 16 mm on both faces, Z ±(0.006 → 0.022): 3 mm between the glass face and the stop's inner face (glazing tape or gasket). Wood: square bead with a 3 mm ovolo. Steel: formed channel with 1.5 mm radii (face B integral, face A screwed). Aluminium: snap bead with a 45° bevel and a 3 mm gasket | The fracture pieces must not intersect the stops: piece Z within ±0.003 at rest, plus the stage-2 push of ≤ 3 mm toward the far side (`04_unity_implementation.md` §4), which stays inside the 3 mm tape zone up to the stop face |
| **Sight line** | X ±0.6835; Y 0.3665 / 1.9835 | Anchors `stop_l/r/t/b`: (∓0.6835, 1.175, 0), (0, 1.9835, 0), (0, 0.3665, 0) |
| **Pocket mid-points** | `pocket_l/r/t/b`: (∓0.6955, 1.175, 0), (0, 1.996, 0), (0, 0.354, 0) | tooth roots |
| **Tooth band** (only if the map chat approves `03`'s exception) | jambs \|X\| 0.600 → 0.6835; head Y 1.900 → 1.9835; sill Y 0.3665 → 0.390 | Anchors `tooth_band_l/r/t/b`: (∓0.600, 1.175, 0), (0, 1.900, 0), (0, 0.390, 0). Without approval, teeth are clipped at the stop line |
| **Floor glass patch centres** | `floor_a` (0, 0, +0.45), `floor_b` (0, 0, −0.45) | Glass-track floor scatters, about 2/3 on the far side |
| **Climb plant** | `sill_plant_a` (0, 0.3505, +0.05), `sill_plant_b` (0, 0.3505, −0.05) | audit §3.6 |
| **RT target** | on the **visible** pane renderer, never on the disabled pane cube | §7 |

**Interim, until the glass track's pane lands.** The map spawns `06` §4.3's 6 mm slab as a render-only child of `Window pane {a}-{b}`, a unit cube scaled to the slab, and moves `FrontRoomsMetalGlassTarget` onto it. Its dimensions are the table's, so nothing in the frame changes when the glass track takes over.

### 5.5 Window frame anchors (sidecar, Unity, window root)

`glass_slab` (0, 1.175, 0); `stop_l/r/t/b`; `pocket_l/r/t/b`; `tooth_band_l/r/t/b`; `face_a` (0, 1.175, +0.105); `blind_rail` (0, 2.195, 0.1275); `sill_plant_a/b`; `floor_a/b`. `meta["glassSlab"] = [1.391, 1.642, 0.006]`.

---

## 6. Integration contract for the map chat

The API is the real one (`Assets/Scripts/Office/FrontRoomsKitLibrary.cs`, identical in the clone):
- `Spawn(name, parent, localPosition, Quaternion localRotation, Vector3? scale, bool colliders, string label)` (`:195-207`);
- `GetInfo(name).TryAnchor(anchor, out pos)` (`:82-89`; anchors are Unity-space, asset-local);
- `Spawn` names the instance `label ?? name` (`:200`), remaps slots to `Resources/Surfaces/<slot>.mat` (`:215-239`), and adds colliders only when `colliders` is true (`:205, 241-251`).

### 6.1 Recommended: a visual-chat facade (render-only; the map keeps all gameplay)

`FrontRoomsInteractableKit` (new, `Assets/Scripts/Office/`, owned by the visual chat). It is three calls that the map makes where it builds the primitives today.

```csharp
// BuildEdge, door branch, after the hinge and the leaf collider exist (MapWorld:1015-1036):
DoorRig rig = FrontRoomsInteractableKit.DressDoor(
    hinge, leafCollider, chunkRoot, doorRootLocal, door.closed,
    sSign,                      // = -door.swing once Option A fixes the side
    member,                     // Lobby | Office | Run | Exit
    locked,                     // per-door lock flag (W5)
    idS, idP);                  // zone identity shown on each face's number plate (number, colour, shape)
// BuildEdge, window branch, BEFORE the brokenWindows early return (MapWorld:1042):
WindowRig w = FrontRoomsInteractableKit.DressWindow(windowRoot, member, faceATowardB, blind);
// SpawnKey (MapWorld:1946):
KeyRig k = FrontRoomsInteractableKit.DressKey(keyRoot, pose, id, data.keyHost);
```

- `DressDoor` creates the `Leaf rig`, spawns every asset (§6.2) with `colliders: false`, disables the `Door leaf` MeshRenderer, and builds the shadow proxy.
- It returns handles for the shots:
  - `rig.Keyhole(face)`, `rig.KeyholeDir(face)`, `rig.KeyholeUp(face)` in world space, from the current pose;
  - `rig.SetPlug(face, deg)`, `rig.SetKnob(face, deg)`, `rig.SetLever(face, deg)`;
  - `rig.SetDeadbolt(t)`, `rig.SetLatch(t)`;
  - `rig.Jolt(metres)`, which moves the leaf rig only;
  - `rig.DetachStrike()`.
- `DressWindow` spawns the frame member and the optional blind. It returns the pane-interface anchors (§5.4) in world space.
- `DressKey` spawns the ring, key and tag (and the host for case A, §4.3). It applies the pose and the zone identity (VARIANT colour, MaterialPropertyBlock number cell).
- Edit mode works: nothing relies on `Start`. The Level Designer preview builds through `BuildForCapture` (`01` §7).

### 6.2 Raw recipe (if the map spawns directly)

All spawns use `colliders: false`. `R = door.closed` (the door root's rotation); `M = (S_sign, 1, 1)`.

| What | Parent | localPosition | localRotation | localScale | label |
|---|---|---|---|---|---|
| frame (`Kit_DoorFrame_*`) | chunk root | `doorRootLocal` (= today's hinge position) | `R` | `M` | `Door frame (kit)` |
| **A2:** hinge | chunk root | `doorRootLocal + R·(S_sign·0.0295, 0, 0.0098)` | `R` (rotates when the door moves) | 1 | `Door hinge {a}-{b}` (unchanged) |
| **A2:** `Door leaf` collider | hinge | **(−S_sign·0.0295, 1.04, 0.4902)** | identity | (0.05, 2.08, 0.98) (unchanged) | `Door leaf` (renderer **disabled**) |
| `Leaf rig` (empty) | hinge | (−S_sign·0.0295, 0, −0.0098) | identity | `M` | `Leaf rig` |
| leaf (`Kit_DoorLeaf_*`) | Leaf rig | 0 | identity | 1 | `Leaf (kit)` |
| shadow proxy (primitive cube, collider destroyed, ShadowsOnly) | Leaf rig | (0, 1.055, 0.500) | identity | (0.10, 2.08, 0.96) | `Leaf shadow` |
| face hardware (rose, lever, escutcheon, knob, shell, plug, closer body) | Leaf rig | the leaf anchor (`rose_s`, `escutcheon_p` …), then the part offsets from the escutcheon anchors (§3.1) | face `_s`: `Euler(0, 90, 0)`; face `_p`: `Euler(0, −90, 0)` | `_s`: 1; `_p`: (−1, 1, 1) | `Lock rose s`, `Lock knob p` … |
| text parts (sign, number plate) | Leaf rig | `sign_*`, `tagplate_*` | as the face | (`S_sign`, 1, 1) | `Door sign s` … |
| bolts | Leaf rig | `deadbolt`, `latchbolt` | identity | 1 | `Lock deadbolt` … |
| strike | frame | frame `strike` | `Euler(0, 180, 0)` | 1 | `Lock strike` |
| closer arm and forearm | Leaf rig / free (solver) | body `spindle` | solved each LateUpdate (§2.4) | 1 | `Closer arm` … |
| window frame (`Kit_WindowFrame_*`) | `Window {a}-{b}` (unscaled root, map change) | 0 | identity, or `Euler(0, 180, 0)` so face A looks into the non-tall cell | 1 | `Window frame (kit)` |
| blind | window root | `blind_rail` | as the frame | 1 | `Window blind (kit)` |
| key parts | `Key · zone {id}` (unscaled empty) | per pose (§4.1) | per pose | 1 | `Key ring`, `Key`, `Key tag` |
| key host | chunk root | the marker (x, 0, z) | `Euler(0, yaw, 0)` | 1 | `Key host (kit)` |

### 6.3 Render-only components (visual chat)

| Component | On | Does |
|---|---|---|
| `FrontRoomsDoorRig` | Leaf rig | Holds part references and the `motion` metadata; implements the `Set*` calls, the jolt and the strike detach |
| `FrontRoomsDoorCloserLinkage` | closer arm | Solves the elbow (§2.4) from the leaf rig's pose in LateUpdate |
| `FrontRoomsKeyAssembly` | `Key · zone` | Builds the hung, flat and over-edge poses; the optional one-swing sway on `DoorMoved` |

### 6.4 What the camera takeovers read (door root, S = +X)

Mirror X for `S_sign` = −1. For a moving leaf, use the leaf rig's transform, which the facade does.

| Anchor | Value | Used by |
|---|---|---|
| `keyhole_s` / `keyhole_p` | (**±0.0315**, 1.000, 0.920) | head dip: framing pose P, key alignment |
| `keyhole_*_dir` (into the lock) | ∓X | key insertion axis |
| `keyhole_*_up` | +Y | key roll (cuts up) |
| `knob_s` / `knob_p` pivot | (±0.024, 0.9365, 0.920), axis ±X | rattle; the new knob beats |
| `lever_s` / `lever_p` pivot (free) | (±0.032, 1.000, 0.920), axis ±X | Open: 35° down |
| `deadbolt` / `latchbolt` | (0, 1.000 / 0.9365 (mortise) or 1.000 (bored), 0.9939), throw +Z | bolt beats |
| `latch_edge_bottom` / `_top` | (0, 0.015 / 2.095, 0.9939) | the Relay-break light sliver; jolt reference |
| `hinge_axis` | (0.0295, 0, 0.0098) | everything that rotates with the door |
| `head_dust_a/b` | (0, 2.098, 0.05 / 0.95) | dust on Relay blows |
| key `shoulder`, `tip`, `grip`, `ring_hole` | §3.3 | key shots |
| window `glass_slab`, `stop_*`, `pocket_*`, `tooth_band_*`, `sill_plant_*`, `floor_*` | §5.4 | glass shots (the glass track), climb |

**`LockPoint` change.**
- Today it returns ±(0.025 + 0.03) = ±0.055 off the centre plane (`MapWorld:1855-1862`).
- For locked doors it should return **the `keyhole` anchor (±0.0315)**, so that `DoorUnlocked`'s point and the sound come from the keyway (`05` §8 item 3, updated for the mortise cylinder).
- Simplest form: `DoorHandleProud` 0.03 → **0.0065** on locked doors, or read it from the sidecar.

### 6.5 Map-side changes, in build order

1. **Option A** (the map chat's own plan, `00`): a fixed swing side per door. Suggestion: open into the Standard-height room (`04` §6.3 item 1), or an edge hash.
   - Pulls: a ≈ 0.45–0.5 m step-back takeover, or a partial open; a `DoorPulled` event.
   - The Relay breaks toward the swing side.
   - Expose `S_sign = −swing`.
2. **A2 pivot** (§1.3): the hinge position and the collider's compensating offset. The collider's closed world pose is unchanged.
3. **Dress doors** (facade, or §6.2), and **disable** (do not destroy) the `Door leaf` renderer.
4. **Per-door lock flag** (W5) → the FREE or LOCKED member; **member level** = Office if the Standard side is Office, else Lobby (§2.1).
5. **`LockPoint` → keyhole** (§6.4).
6. **Jolts go on the `Leaf rig`**, not the hinge (`DoorSound` would hear a hinge move, audit F11) and not the collider. The unlock shift (1.5 mm), rattle (2 mm) and Relay blows (4–8 mm) are visual only.
7. **Windows:** an unscaled `Window {a}-{b}` root; the frame spawned **before** the `brokenWindows` return (`MapWorld:1042`); the pane renderer disabled (collider kept); the interim slab with `FrontRoomsMetalGlassTarget` moved onto it (§5.4).
8. **Keys:** an unscaled `Key · zone {id}` empty; host spawn for case A; the procedural fallback C replaces the spinning key; the key cell's lamp forced Steady (§4.3).
9. **Optional:** skip the jamb and head trims (`MapWorld:999-1009`) on door and window edges once a kit frame is placed. This removes hidden overdraw and allows the true 2" steel profile (P3).
10. **Level Designer palette:** skip assets tagged `interactable` (door frames would otherwise appear as Floor placements, and hosts as Wall decor). The test helpers' floor-kit and desk-kit pickers are safe, because their footprint filters exclude these assets (`Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs:677-701`).
11. **Shots:** add the knob constants (§3.4) to `FrontRoomsShotTimings`, owned by the map chat. On locked doors, `Rattle` uses the knob.

### 6.6 Rule clarifications I ask the map chat to confirm

1. **Door stops inside the opening.**
   - The push-side stop stands 18 mm into the 1.0 m opening at the jambs and head (Z 0.002–0.018, 0.982–0.998; Y 2.082+).
   - It is render-only and never touched by the player capsule, whose centre stays ≥ 0.3 m from the reveal.
   - `00` says "Nothing collides inside it", which this keeps.
2. **The window stop band** (16 mm, render-only, inside the 1.4 × 1.65 opening) and `03`'s tooth band (≤ 0.10 m at the jambs and head, ≤ 0.04 m at the sill).
3. **Proud limits under A2** (§1.5):
   - ≤ 0.065 for locksets;
   - closer arms up to 0.20 m but only above 2.06 m;
   - the P-face exit device 0.095 m.

### 6.7 What must not change

- The leaf collider size and closed pose, and the pane collider.
- `doorByCollider` and `windowByCollider` (`MapWorld:1037-1038, 1052`).
- The names `Door hinge*`, `Door leaf`, `Window pane {a}-{b}`, `Key · zone {id}`.
- No kit child may take a hazard name (§1.6).
- No colliders on any kit part.
- No Light components.
- Event points: `Door.position` and `Window.position` stay at floor level (`01` F4). The shots add the anchor heights.

---

## 7. The independent ray-traced glass track (ChatGPT/Codex; Red's request for this run)

### 7.1 What it is

ChatGPT built a third route to ray-traced reflections on Red's M3 Max. It goes around Unity, whose own ray-tracing API reports no RT on Metal (`../glass/11_reflections_and_raytracing.md`).

**The pieces:**
- **Native plugin.** `NativePlugin/FrontRoomsMetalGlassRT.mm`, built into `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib`. It takes Unity's Metal device and buffers, builds its own BLAS/TLAS, and traces with Metal's ray intersector.
- **Controller** (`Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs`):
  - every 1 s it finds the `FrontRoomsMetalGlassTarget` nearest the camera (`:126-142`);
  - it registers every **enabled** `MeshRenderer` within 18 m, up to 256 (`:144-157`);
  - it rebuilds every BLAS and the TLAS.
- **Per frame:** one primary ray per pixel. A glass hit gets one reflection ray, shaded flat with a fixed "sun" and a sky gradient on a miss.
- **Composite:** a full-screen blend after post-processing (`FrontRoomsMetalGlassRTRendererFeature.cs`, `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader`).
- **Map edits:** two lines in the map chat's file: `Ensure()` in standalone play (`MapWorld:347`) and the target tag on every pane (`MapWorld:1049`).
- **Git:** committed in `edfbc92` and `c69c7d7`, both with the message "1".

**Defects already found, read from the code** (`06` §5.2; `../glass/destruction/04_unity_implementation.md` §1.3):
- it paints glass opaque (alpha 1 over the scene);
- it passes the FOV in radians where the kernel expects tan(fov/2);
- it composites after post with ZTest Always;
- flat shading with a fake sky;
- a main-thread rebuild every second that blinks every window for one frame;
- the composite shader is probably stripped in builds;
- it ignores cracks;
- `supportsRaytracing` is the wrong hardware gate;
- it runs only in the map test scene;
- the 256-instance cap is filled at random;
- no `useResource`;
- skinned meshes (the Relay) are invisible to it.

### 7.2 Where it stands

- `VISUAL_CHAT_TASKS.md` **G14** already tracks it: RUNNING in the workflow "glass-rt-track" on the private clone `proj_rt`, with baseline hashes in the scratchpad.
- The work includes an adversarial review, a runtime probe, a production design, P0 correctness, P1 hit shading, and output into the glass shader hook `_FR_GlassRTReflection` / `_FR_GlassRTWeight`.
- This workflow does not touch those files. What follows is what **this kit** needs from that track, and what the kit does for it.

### 7.3 New findings while writing this spec (code-read, UNVERIFIED in a run)

1. **Every LOD level would enter the TLAS at once.**
   - Registration only checks `renderer.enabled` (`FrontRoomsMetalGlassRT.cs:147-149`).
   - A LODGroup culls LOD1 and LOD2 without disabling their renderers.
   - So each kit asset with LODs registers 2–3 overlapping meshes. That means z-fighting duplicates in the reflection, and 2–3× the instances against the 256 cap.
   - **Fix (G14):** register only the LOD the camera uses, or only LOD0 (skip names ending `_LOD1`/`_LOD2`, or read `LODGroup.GetLODs()[0]`).
2. **Only submesh 0 and `sharedMaterial` are traced** (`:180-187, 191-197`).
   - A kit asset with 2–4 slots shows only its first slot in reflections.
   - **Kit rule until G14 registers all submeshes:** each module adds its **dominant slot's parts first**. `finish()` joins into `parts[0]` (`kitlib.py:663-667`), so the first slot becomes submesh 0. For example: the enamel skin before the kick plates; the walnut casing before the brass hinges.
3. **Only `_BaseColor` is read** (`:322`). Kit surfaces keep their colour in `_BaseMap`, with `_BaseColor` white: `Prop_SteelBrown.mat:66`, `Door_Veneer.mat:66`, `Painted_Metal.mat:66`, `Prop_WoodWalnut.mat:66`. A dark-bronze frame would therefore reflect as white.
   - **Fix (G14 P1):** a per-material mean albedo.
   - Measured means (sRGB): `Door_Veneer` (157, 109, 59); `Prop_WoodOak` (154, 106, 57); `Prop_WoodLaminate` (142, 109, 75); `Prop_WoodWalnut` (73, 47, 31); `Prop_SteelBrown` (58, 45, 36); `Painted_Metal` (210, 205, 189); `Prop_Aluminium` (184, 183, 180); `Prop_PlasticWhite` (217, 213, 200).
4. **Mirrored doors are negative-scale instances** (§1.2).
   - Metal intersects them, but the kernel's normal must be flipped when det(M) < 0, or the mirrored frames shade inside-out in reflections.
   - Test with one mirrored door in the probe.

### 7.4 What the kit does for the RT track

- The **visible** glass renderer carries `FrontRoomsMetalGlassTarget`: the glass track's intact pane, or §5.4's interim slab. It must never sit on the pane cube, whose renderer the map disables. A disabled renderer is skipped (`:149`), so every window would silently drop out of the trace.
- Frames, blinds, doors and hardware are ordinary MeshRenderers. They enter the BLAS within 18 m with no extra work, and each asset is one mesh, so its BLAS is built once (`meshIndices`, `:173`).
- Vertex format: kit FBX meshes import with Float32 × 3 positions in stream 0 and triangle topology, which is what the bridge accepts (`:175-179`). Index format is 16-bit under 65k vertices. Every kit asset is under 65k vertices.
- A near door adds about 20 small renderers (§9). G14's instance budget should take glass and fracture pieces first, then the nearest (`04_unity_implementation.md` §7).

### 7.5 Task rows to add (for the visual chat's queue; this workflow does not edit `VISUAL_CHAT_TASKS.md`)

| # | Task | Owner | Status |
|---|---|---|---|
| G14-K1 | RT: register only LOD0 (or the active LOD) of LODGroup assets (§7.3 item 1) | visual (G14) | QUEUED |
| G14-K2 | RT: all submeshes and materials per renderer; until then the kit puts the dominant slot in submesh 0 (§7.3 item 2) | visual (G14) + kit builders | QUEUED |
| G14-K3 | RT: per-material mean albedo, or texture sampling, for hit shading; the measured means are in §7.3 item 3 | visual (G14 P1) | QUEUED |
| G14-K4 | RT: negative-determinant instances (mirrored doors) shade correctly; probe with one mirrored door | visual (G14) | QUEUED |
| G14-K5 | RT target moves to the visible glass (interim slab, then the glass track's pane), with the map's line `MapWorld:1049` | visual + map | QUEUED (`06` §5.4 step 3) |

---

## 8. Pipeline changes that NEED APPROVAL (visual chat; not made by this workflow)

| # | Change | Smallest form | Why |
|---|---|---|---|
| P-1 | **LOD2 and per-asset switch distances** | `kitlib.py`: generalise `make_lod1` to `make_lods(ratios)`, about 40 lines. Each level n copies LOD0, deletes parts flagged `fr_lod{n}_drop` (the existing `fr_lod1_drop` mechanism, `:650-661, 701-760`), decimates the rest to the ratio while protecting `fr_lod_keep`, and names the copies `<NAME>_LOD0/1/2`. `export()` selects every LOD object (`:768-772`) and writes `trianglesLod2` and `lodDistances`. `build_asset.py:51`: read `LODS = (r1, r2)`, falling back to `LOD1`. `FrontRoomsKitImporter.OnPostprocessModel` (`:67-79`): if the sidecar has `lodDistances`, set `lods[i].screenRelativeTransitionHeight = group.size / (2 · d_i · tan 38°)`; otherwise keep today's two-level defaults. Optional: `FrontRoomsKitLibrary.Info` gains `trianglesLod2` and `lodDistances` (JsonUtility ignores unknown fields) | Red's v: LOD1 (~40–50 %) and LOD2 (~10–20 %) with screen-size switch points. **Today's importer culls small objects at 3 % screen height** (`:76`): a 0.067 m knob would vanish at about 1.4 m. So small parts ship without LODs until this lands (§1.8) |
| P-2 | Weighted normals | Only if the G1 probe (§1.8) shows custom normals do not survive `finish()`: a module flag `WEIGHTED_NORMALS = True`, and `finish()` adds a WeightedNormal modifier (face area with angle, keep sharp) after the join. About 8 lines | Hero bevels read flat without it |
| P-3 | Vertex-colour wear (W2) | `FrontRoomsSurface.shader`: an optional keyword reads COLOR (R hand grime, G edge wear, B cavity), off by default. The kit's FBX export already writes colour attributes (`export_scene.fbx` `colors_type` defaults to `'SRGB'`, Blender 4.3 `io_scene_fbx/__init__.py:399-406`) | Wear "where hands and keys go" without unique textures. Whether meshes without colours read white must be checked |
| P-4 | New Unity surfaces and atlases | `Door_Enamel` (almond enamel; fallback `Painted_Metal`), `Prop_SignEngraved` (2 × 2 atlas), `Prop_KeyTagNo` (10 × 10 typed numbers on paper, Courier Prime), `Run_ExitSign_Dead` (the non-emissive EXIT face), optional `Prop_PlasticGreen`. **No `kitlib.py` edit is needed**: modules call `kitlib.register_slot(...)` (`:132-134`), and the importer remaps any slot that has `Resources/Surfaces/<slot>.mat` (`FrontRoomsKitImporter.cs:36-46`). The work is the textures and materials | §2.4, §4.1 |
| P-5 | Glass materials | `Glass_Window`, `Glass_Edge`, `Glass_Shard`, `Glass_Wired`: glass track, not this kit | §5.3 |

---

## 9. Master asset list (final names, budgets, LODs)

**Common to every asset:** render-only (`kit.no_collider()`); tag `interactable`; front axis +Z (kit −Y).

**Column key:**
- **Size** is the bounds, Unity X × Y × Z.
- **Origin:** D = door root (§1.2); W = window root (§5.1); P = part frame (§1.2).
- **Tris** = LOD0 / LOD1 / LOD2.
- **LOD dist** = switch distances in metres at `lodBias` 1 (doubled on Standalone Ultra).
- **LOD1 now?** "yes" = may set `LOD1` today; "no" = ships without a LODGroup until P-1 is approved.

### 9.1 Doors (group G1)

| Asset | Module | Pri | Size (m) | Origin | Slots | Tris | LOD dist | LOD1 now? |
|---|---|---|---|---|---|---|---|---|
| `Kit_DoorFrame_Wood` | `interact_door_frame_wood.py` | P1 | 0.21 × 2.175 × 1.15 | D | WoodWalnut, Brass, WoodOak | 2,800 / 1,100 / 250 | 4 / 12 / — | yes (0.40) |
| `Kit_DoorFrame_Steel` (+ `_Alu`) | `interact_door_frame_steel.py` | P1 | 0.21 × 2.175 × 1.15 | D | SteelBrown, Rubber, Chrome, Aluminium | 3,600 / 1,300 / 280 | 4 / 12 / — | yes (0.40) |
| `Kit_DoorLeaf_Veneer` (+ `_Oak`) | `interact_door_leaf_veneer.py` | P1 | 0.044 × 2.08 × 0.99 (+ knuckles) | D | Door_Veneer, Brass | 2,600 / 900 / 120 | 4 / 12 / — | yes (0.40) |
| `Kit_DoorLeaf_Steel` | `interact_door_leaf_steel.py` | P1 | as above | D | Door_Enamel, Aluminium, Chrome | 4,200 / 1,500 / 150 | 4 / 12 / — | yes (0.40) |
| `Kit_DoorLeaf_Ward` | `interact_door_leaf_ward.py` | P2 | 0.15 × 2.08 × 0.99 | D | WoodLaminate, Aluminium, Chrome | 5,000 / 1,800 / 180 | 4 / 12 / — | yes |
| `Kit_DoorLeaf_SteelLite` | `interact_door_leaf_steel_lite.py` | P2 | as Steel | D | Door_Enamel, Aluminium, Chrome, Prop_Glass | 5,200 / 1,900 / 200 | 4 / 12 / — | yes |
| `Kit_DoorCloser_Body` | `interact_door_closer_body.py` | P1 | 0.05 × 0.065 × 0.29 | P (on `closer_mount_s`) | SteelBrown (painted aluminium), Chrome | 3,000 / 1,000 / 150 | 1.5 / 5 / 20 | no |
| `Kit_DoorCloser_Arm` | `interact_door_closer_arm.py` | P1 | 0.26 × 0.02 × 0.03 | P (spindle) | SteelBrown | 1,200 / 400 / 60 | 1.5 / 5 / 20 | no |
| `Kit_DoorCloser_Forearm` | `interact_door_closer_forearm.py` | P1 | 0.27 × 0.06 × 0.02 | P (elbow) | SteelBrown, Chrome | 900 / 300 / 50 | 1.5 / 5 / 20 | no |
| `Kit_DoorCloser_Shoe` | `interact_door_closer_shoe.py` | P1 | 0.03 × 0.04 × 0.02 | P (`closer_shoe`) | SteelBrown, Chrome | 500 / 200 / 40 | 1.5 / 5 / 20 | no |
| `Kit_ExitDevice_Crossbar` | `interact_exit_device.py` | P2 | 0.095 × 0.12 × 0.86 | P (latch-end mount) | Chrome, Aluminium | 3,500 / 1,200 / 200 | 2 / 6 / 25 | no |

### 9.2 Locks (group G2)

| Asset | Module | Pri | Size (m) | Origin | Slots | Tris | LOD dist | LOD1 now? |
|---|---|---|---|---|---|---|---|---|
| `Kit_Lock_Escutcheon` (+ `_Brass`) | `interact_lock_escutcheon.py` | P1 | 0.057 × 0.203 × 0.008 | P | Chrome | 3,000 / 1,200 / 200 | 1.5 / 4 / 12 | no |
| `Kit_Lock_CylinderShell` (+ `_Brass`) | `interact_lock_cylinder_shell.py` | P1 | 0.044 × 0.044 × 0.0075 | P (keyhole) | Chrome, Brass (core face) | 2,600 / 1,000 / 150 | 1.5 / 4 / 12 | no |
| `Kit_Lock_Plug` | `interact_lock_plug.py` | P1 | 0.0115 × 0.0115 × 0.030 | P (keyhole) | Brass, PlasticBlack (cavity) | 1,800 / 700 / 80 | 1.0 / 3 / 8 | no |
| `Kit_Lock_Knob` (+ `_Brass`) | `interact_lock_knob.py` | P1 | 0.054 × 0.054 × 0.063 | P (spindle) | Chrome | 4,500 / 1,800 / 300 | 1.5 / 4 / 12 | no |
| `Kit_Lock_Deadbolt` | `interact_lock_deadbolt.py` | P1 | 0.0125 × 0.030 × 0.045 | P (lock front) | Chrome | 400 / 150 / 30 | 1.0 / 3 / 8 | no |
| `Kit_Lock_Latchbolt_Mortise` | `interact_lock_latch_mortise.py` | P1 | 0.0125 × 0.030 × 0.040 | P | Chrome | 600 / 220 / 40 | 1.0 / 3 / 8 | no |
| `Kit_Lock_Latchbolt_Bored` (+ `_Brass`) | `interact_lock_latch_bored.py` | P1 | 0.010 × 0.020 × 0.030 | P | Chrome | 500 / 200 / 40 | 1.0 / 3 / 8 | no |
| `Kit_Lock_Rose` (+ `_Brass`) | `interact_lock_rose.py` | P1 | 0.070 × 0.070 × 0.010 | P | Chrome | 2,400 / 900 / 120 | 1.5 / 4 / 12 | no |
| `Kit_Lock_Lever` (+ `_Brass`) | `interact_lock_lever.py` | P1 | 0.13 × 0.03 × 0.054 | P (spindle) | Chrome | 4,000 / 1,600 / 300 | 1.5 / 4 / 15 | no |
| `Kit_Lock_StrikeMortise` | `interact_lock_strike_mortise.py` | P1 | 0.056 × 0.200 × 0.016 | P (on the lining) | Chrome, PlasticBlack (dust box) | 900 / 350 / 60 | 1.5 / 4 / 12 | no |
| `Kit_Lock_StrikeBored` (+ `_Brass`) | `interact_lock_strike_bored.py` | P1 | 0.056 × 0.124 × 0.016 | P | Chrome, PlasticBlack | 600 / 250 / 40 | 1.5 / 4 / 12 | no |

**Per door at LOD0** (the heaviest, OF-K, viewed within 1.5 m):
- frame 3.6k + leaf 4.2k;
- lockset 2 × (escutcheon 3.0k + shell 2.6k + plug 1.8k + knob 4.5k) = 23.8k;
- bolts and strike 1.9k; closer 5.6k; signs and plates 2.2k;
- total **≈ 41k triangles** and **≈ 21 renderers**.

**Beyond 12 m:** frame + leaf + shadow proxy = 3 renderers, ≈ 0.7k triangles at LOD2. This only holds once P-1 lands. Until then, small parts are never culled: draw cost for desktop only. The WebGL tier is the WebGL track's decision (memory rule: never lower desktop for WebGL).

### 9.3 Keys and identity (group G3)

| Asset | Module | Pri | Size (m) | Origin | Slots | Tris | LOD dist | LOD1 now? |
|---|---|---|---|---|---|---|---|---|
| `Kit_Key_Zone` (+ `_Nickel`) | `interact_key.py` | P1 | 0.0022 × 0.026 × 0.058 | P (shoulder) | Brass | 2,400 / 900 / 150 | 1.0 / 3 / 12 | no |
| `Kit_KeyRing` | `interact_key_ring.py` | P1 | 0.025 × 0.025 × 0.0018 | P (hook contact) | Chrome | 2,000 / 600 / 96 | 1.0 / 3 / 12 | no |
| `Kit_KeyTag_Rect` (+ `_Blue`, `_White`) | `interact_key_tag_rect.py` | P1 | 0.057 × 0.029 × 0.0042 | P (hole) | PlasticRed, KeyTagNo | 1,200 / 400 / 60 | 1.0 / 3 / 15 | no |
| `Kit_KeyTag_Round` (+ `_Blue`, `_White`) | `interact_key_tag_round.py` | P1 | Ø 0.038 | P | as above | 1,200 / 400 / 60 | as above | no |
| `Kit_KeyTag_Long` (+ `_Blue`, `_White`) | `interact_key_tag_long.py` | P1 | 0.076 × 0.022 | P | as above | 1,200 / 400 / 60 | as above | no |
| `Kit_KeyBoard` | `interact_key_board.py` | P1 | 0.30 × 1.65 (top) × 0.05 | wall face, floor | WoodDark, Brass, KeyTagNo | 4,000 / 1,400 / 150 | 3 / 8 / 40 | no (0.4 m < 1.0) |
| `Kit_KeyCabinet` | `interact_key_cabinet.py` | P1 | 0.75 (door open) × 1.70 × 0.10 | wall face, floor | SteelAlmond, PlasticWhite, Chrome, Paper | 7,000 / 2,400 / 250 | 3 / 8 / 40 | no |
| `Kit_KeyHook` | `interact_key_hook.py` | P1 | 0.01 × 1.49 × 0.03 | wall face, floor | Brass | 600 / 200 / — | 1.5 / 4 / 12 | no |
| `Kit_DoorSign` | `interact_door_sign.py` | P1 | 0.254 × 0.076 × 0.004 | P | PlasticBlack, SignEngraved | 600 / 200 / 12 | 2 / 6 / 25 | no |
| `Kit_DoorNumberPlate` (+ `_Blue`, `_White`) | `interact_door_number_plate.py` | P1 | 0.100 × 0.050 × 0.004 | P | PlasticRed, KeyTagNo | 500 / 180 / 12 | 1.5 / 5 / 15 | no |
| `Kit_ExitSign` (+ `_Dead`) | `interact_exit_sign.py` | P2 | 0.330 × 0.200 × 0.060 | P (on the wall) | SteelBlack, Run_ExitSign | 2,000 / 700 / 100 | 3 / 10 / — | no |

Wall hosts have their origin at the floor, so the Size column gives the top height. The key board's mesh spans Y 1.25–1.65 (0.30 × 0.40). Its sidecar `minCeiling` is 1.70 (`kitlib.py:823`).

### 9.4 Windows (group G4)

| Asset | Module | Pri | Size (m) | Origin | Slots | Tris | LOD dist | LOD1 now? |
|---|---|---|---|---|---|---|---|---|
| `Kit_WindowFrame_Wood` | `interact_window_frame_wood.py` | P1 | 1.60 × 2.075 × 0.242 | W | WoodWalnut, Rubber | 2,600 / 1,000 / 200 | 4 / 12 / — | yes (0.40) |
| `Kit_WindowFrame_Steel` (+ `_Enamel`) | `interact_window_frame_steel.py` | P1 | 1.55 × 2.075 × 0.21 | W | SteelBrown, Rubber | 3,600 / 1,300 / 220 | 4 / 12 / — | yes (0.36) |
| `Kit_WindowFrame_Alu` | `interact_window_frame_alu.py` | P2 | 1.55 × 2.075 × 0.21 | W | Aluminium, Rubber | 2,800 / 1,000 / 200 | 4 / 12 / — | yes |
| `Kit_MiniBlind_Raised` | `interact_mini_blind_raised.py` | P1 (opt.) | 1.55 × 0.19 × 0.04 | P (`blind_rail`) | SteelAlmond, PlasticWhite | 2,400 / 800 / 120 | 3 / 10 / 30 | yes (1.55 m) |
| `Kit_MiniBlind_Lowered` | `interact_mini_blind_lowered.py` | P2 | 2.40 × 1.30 × 0.04 | P | SteelAlmond, PlasticWhite | 6,000 / 1,500 / 200 | 3 / 10 / 30 | yes |

---

## 10. Build groups (parallel; each self-contained)

### 10.0 Rules for every group

**Where files go.**
- Write **new files only**, in `Tools/Blender/frontrooms_kit/assets/`, named `interact_*.py`.
- One `NAME` per module. Shared code lives in one helper module per group (`interact_<group>_common.py`, which has no `NAME` and no `build`).
- **Never edit `kitlib.py` or `build_asset.py`.** Never open Unity on the real project.

**Building.**
- Build into the private clone only:
  `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python "<project>/Tools/Blender/frontrooms_kit/build_asset.py" -- <modules…> --out-root <proj_int> --preview-dir <scratchpad>/interact_previews`
- `build_asset.py` writes to `<out-root>/Assets/Resources/Props/Models` (`build_asset.py:42-44`). Without `--out-root` it would write into the real project.

**Modelling.**
- Use §1.2's frames and the `U()` mapping. Every number in this spec is a Unity-space metre.
- `kit.no_collider()`; tags `interactable` plus the group tags; `kit.meta["lodDistances"]`, `LOD1_RATIO`, `LOD2_RATIO`; `LOD1` only where §9 says "yes".
- Mark parts to drop at LOD2 with `obj["fr_lod2_drop"] = True`; use `kit.lod1_drop()` for LOD1 drops.
- Paint an `fr_wear` colour attribute (white by default; darker where hands go) on **every** part (§1.8).
- **Add the dominant slot's parts first** (§7.3 item 2).
- New surfaces through `kitlib.register_slot(...)` with the preview colours of §2–§4: `Door_Veneer`, `Door_Enamel`, `Prop_KeyTagNo`, `Prop_SignEngraved`, `Run_ExitSign`. Never edit `SLOTS`.
- Every module docstring states the real-world reference, the origin, the front, the budget and the era fit, as `assets/crt_monitor.py` does.

**Checking.**
- Each module **asserts its own key numbers**: envelopes, anchors, the keyway polygon, proud limits.
- Triangle counts must land within ±15 % of §9.
- Previews are Cycles stills (`kitlib.py:828-879`) plus the group's close-up renders, saved in the scratchpad.
- Report every assumption marked ESTIMATE that turned into a number.

### 10.1 G1: door construction (frames, leaves, hinges, closers)

| | |
|---|---|
| **Modules (P1)** | `interact_door_common.py` (helper), `interact_door_frame_wood.py`, `interact_door_frame_steel.py`, `interact_door_leaf_veneer.py`, `interact_door_leaf_steel.py`, `interact_door_closer_body.py`, `interact_door_closer_arm.py`, `interact_door_closer_forearm.py`, `interact_door_closer_shoe.py` |
| **Modules (P2, after P1)** | `interact_door_leaf_ward.py`, `interact_door_leaf_steel_lite.py`, `interact_exit_device.py` |
| **Assets** | `Kit_DoorFrame_Wood`, `Kit_DoorFrame_Steel` (+ `_Alu`), `Kit_DoorLeaf_Veneer` (+ `_Oak`), `Kit_DoorLeaf_Steel`, `Kit_DoorCloser_Body/Arm/Forearm/Shoe`; P2: `Kit_DoorLeaf_Ward`, `Kit_DoorLeaf_SteelLite`, `Kit_ExitDevice_Crossbar` |
| **Spec** | §1 (all), §2.1, §2.3, §2.4 (closer and exit device), §9.1 |
| **Task 0** | The weighted-normal probe (§1.8), run as a scratchpad script that imports `kitlib` directly, not as a file in `assets/`. Decide per-part modifiers or support loops |
| **Self-check** | (a) **Gap test T1** in Blender: BVH rays from both faces across a 0.5° grid, through every perimeter gap, for both handings → zero hits through. (b) **Swing test T2**: rotate the leaf-side meshes about `hinge_axis` from 0 to 95° in 1° steps, both handings; zero BVH overlaps with the frame or with a stand-in wall (\|X\| ≤ 0.08 outside the opening); report the minimum clearance (≥ 2.5 mm expected, §1.5). (c) The closer linkage solves 0–95° with the elbow on the room side and X ≥ 0.14 (§2.4). (d) The casing encloses a stand-in of the map trims (0.07 × 0.20 jambs, 1.14 × 0.07 head). (e) Renders: both faces at 1.5 m, the latch edge at 0.3 m, the hinge knuckles at 0.3 m, and the door at 45° and 95° |

### 10.2 G2: the keyable lock and door hardware

| | |
|---|---|
| **Modules (P1)** | `interact_lock_common.py` (helper: §3.2 keyway polygon and bitting, lathe, knurl and slotted-screw builders, `motion` meta), `interact_lock_escutcheon.py`, `interact_lock_cylinder_shell.py`, `interact_lock_plug.py`, `interact_lock_knob.py`, `interact_lock_deadbolt.py`, `interact_lock_latch_mortise.py`, `interact_lock_latch_bored.py`, `interact_lock_rose.py`, `interact_lock_lever.py`, `interact_lock_strike_mortise.py`, `interact_lock_strike_bored.py` |
| **Assets** | `Kit_Lock_Escutcheon`, `Kit_Lock_CylinderShell`, `Kit_Lock_Plug`, `Kit_Lock_Knob`, `Kit_Lock_Deadbolt`, `Kit_Lock_Latchbolt_Mortise`, `Kit_Lock_Latchbolt_Bored`, `Kit_Lock_Rose`, `Kit_Lock_Lever`, `Kit_Lock_StrikeMortise`, `Kit_Lock_StrikeBored`; VARIANTS `_Brass` where §9.2 lists them |
| **Spec** | §3 (all), §2.1 hardware positions, §6.4, §9.2 |
| **Self-check** | (a) Assemble a scratch test door in Blender: a 44 mm slab with every part placed at §6.4's anchors on **both** faces by §1.2's face rules (rotation and mirror). Proud ≤ 0.065; nothing overlaps. (b) **Key fit T8**: extrude the §3.2 blade polygon (with cuts) 25 mm into the plug at full insertion. Zero overlap with the plug walls and pin tips at 0°, 45° and 90° of a joint rotation. (c) The knob turns ±40° and the lever 35° with no overlap against the escutcheon or rose. (d) The deadbolt and latch slide their travel inside the armor-front opening. (e) Renders: pose P (0.57 m, FOV 62°, pitch 39°) of the escutcheon with the key at 0° and 90°; the IC face at 0.3 m; the knob knurl at 0.3 m |

### 10.3 G3: keys, tags, hosts and door signage

| | |
|---|---|
| **Modules (P1)** | `interact_key_common.py` (helper: §3.2 blade polygon, identical numbers to G2's; pose helpers), `interact_key.py`, `interact_key_ring.py`, `interact_key_tag_rect.py`, `interact_key_tag_round.py`, `interact_key_tag_long.py`, `interact_key_board.py`, `interact_key_cabinet.py`, `interact_key_hook.py`, `interact_door_sign.py`, `interact_door_number_plate.py` |
| **Modules (P2)** | `interact_exit_sign.py` |
| **Assets** | `Kit_Key_Zone` (+ `_Nickel`), `Kit_KeyRing`, `Kit_KeyTag_Rect/Round/Long` (+ `_Blue`, `_White`), `Kit_KeyBoard`, `Kit_KeyCabinet`, `Kit_KeyHook`, `Kit_DoorSign`, `Kit_DoorNumberPlate` (+ `_Blue`, `_White`); P2: `Kit_ExitSign` (+ `_Dead`) |
| **Spec** | §3.2, §3.3, §4 (all), §2.4 (sign, number plate, exit sign), §9.3 |
| **Also** | **Proposal textures for approval (P-4)**, written to the scratchpad only, never to `Assets/`: `Prop_KeyTagNo` as a 2048² 10 × 10 atlas of "00"–"99", typed in Courier Prime (`Assets/Fonts/Period1990/CourierPrime/`) on a paper ground; and `Prop_SignEngraved` as a 2 × 2 atlas (EMPLOYEES ONLY, STAFF ONLY, STORAGE, spare) in TeX Gyre Heros Bold, ivory on dark brown (`05` §6.2). No dates, no logos |
| **Self-check** | (a) The blade section equals §3.2's polygon (assert). If G2's `Kit_Lock_Plug` FBX exists in the clone, run T8 against it. (b) A hung-pose assembly (ring, key and tag on `Kit_KeyBoard`'s `key_hook`) rendered at 0.35 m (the number must be readable), 2 m and 6 m. (c) A flat pose on a 0.74 m desk top. (d) Each host's `key_hook` is ≤ 0.10 proud and at its stated height |

### 10.4 G4: windows

| | |
|---|---|
| **Modules (P1)** | `interact_window_common.py` (helper: §5.2 section and the shared ranch casing profile from §2.3), `interact_window_frame_wood.py`, `interact_window_frame_steel.py`, `interact_mini_blind_raised.py` |
| **Modules (P2)** | `interact_window_frame_alu.py`, `interact_mini_blind_lowered.py` |
| **Assets** | `Kit_WindowFrame_Wood`, `Kit_WindowFrame_Steel` (+ `_Enamel`), `Kit_MiniBlind_Raised`; P2: `Kit_WindowFrame_Alu`, `Kit_MiniBlind_Lowered` |
| **Spec** | §5 (all), `06` §3–§4 (detail), §9.4 |
| **Self-check** | (a) Nothing inside X ±0.6835 × Y 0.3665–1.9835 at any Z (assert on the vertices). (b) The sleeve encloses a stand-in of the map trims (`MapWorld:999-1009`: jambs 0.07 × 0.20 from 0.35 to 2.0; head 1.54 × 0.07 × 0.20). (c) The anchors of §5.5, with `meta["glassSlab"]`. (d) A 6 mm stand-in slab of 1.391 × 1.642 placed at `glass_slab` sits in the pocket with 0 overlaps and its edges hidden behind the stops from both faces at 0°, 30° and 60° viewing angles. (e) The raised blind's lowest point is ≥ 2.000. (f) Renders: both faces at 1.5 m, a corner at 0.3 m, the screw row at 0.3 m |

### 10.5 After the groups (the workflow's next stages; not part of the groups)

1. Facade code (§6.1) and the component scripts (§6.3), in the clone.
2. Integration tests T1–T10 in Unity (§11).
3. A critic pass.
4. The three-view hand-off for Figma R10 (`VISUAL_CHAT_TASKS.md` R10).
5. The map-chat contract message (§6, §12).

---

## 11. Verification (acceptance tests)

| # | Test | Pass |
|---|---|---|
| T1 | Gap rays (Blender in G1; again in Unity with `harness/FrontRoomsDoorGapCapture.cs.txt`). Both handings, both faces, a dark near room and a lit far room (`04` §1.4 method) | no lit line at any jamb, the head or the floor; the eye can be placed anywhere |
| T2 | Swing clip, 0–95° in 1° steps, both handings, pushed and pulled | no mesh overlap; minimum clearance ≥ 2.5 mm |
| T3 | Gameplay unchanged: `Door leaf` collider bounds when shut equal today's ±0.1 mm; pane collider untouched; E-ray hits and Relay sight rays unchanged in `FrontRoomsMapInteractionTests` | identical |
| T4 | Handing and text: a door with `S_sign` = −1 | knuckles on the pull face; the lever points at the hinge on both faces; sign and number read correctly on both faces |
| T5 | Head dip at pose P (§3.4), both faces, `05`'s strip method | keyway, plug and knob in frame; the key enters without intersecting; 90° turn; the deadbolt beat |
| T6 | Readability at 6/12/20 m: `05`'s harness, plus **Run** and **Exit** rooms | locked ≥ +1 stop over free in every level; ≥ +1.5 stops over an open dark doorway |
| T7 | LOD counts and switch distances (after P-1) | within ±15 % of §9; no small part culled within its stated distance |
| T8 | Key fit (blade vs keyway at 0°, 45° and 90°) | zero overlap |
| T9 | Glass interface: a stand-in slab in every window member | hidden edges from both faces at 0–60°; nothing in the clear zone |
| T10 | Cost: seed 4242 frames 01/04/23/34/35 (audit harness): draws and frame time before and after | report; no budget set until measured |

---

## 12. Asks

**Map chat (关卡设计):** §6.5 items 1–11, and the clarifications in §6.6. **A2 is the one new geometric change**: two offsets. The rest is Option A's own work, plus spawning.

**Sound chat (声音):**
- Door frame and leaf tags give a `doorType` (wood vs hollow metal) and a `frame` (wood/steel/alu) for `Mechanism/Door/*` (`05` §8 item 9).
- New knob beats (§3.4): `Mechanism/Lock/KnobTurn` (NEW) on locked doors.
- The deadbolt `BoltRetract` stays at the 0.86 commit.
- Option A adds a pull event (`04` §8).
- Free Run doors are push/pull with no latch: no `Unlatch`.

**平面视觉 (graphic-visual):**
- Approve or redraw the `Prop_KeyTagNo` and `Prop_SignEngraved` proposals (G3).
- Place the R10 three-views when the workflow hands them off.

**Red:**
1. The Run and Exit members (P2), including the Exit's lit versus dead EXIT sign, and red versus green letters.
2. The wired lite on RN-K (a see-through hole).
3. The bent slat in Office blinds.
4. Hammered glass in some Lobby windows (`06` §6).
5. Whether a shut, already-unlocked locked door may still look locked (`05` §8 item 2).
6. The locked function: **this spec picks a deadbolt plus a passage knob** over `05`'s storeroom knob, because of Red's "deadbolt that slides". The cost is that the rattle shows the knob turning freely, then the leaf stopping on the bolt.

---

## 13. Open items and UNVERIFIED

1. RE8 single-swing versus double-acting behaviour, and its true stops, are still unconfirmed by a text source (`04` §2.3). Option A is Red's choice either way.
2. Several numbers are ESTIMATES, to measure on real hardware before final modelling:
   - the IC core figure-8 dimensions, the keyway profile and the bitting (§3.1–3.2);
   - the mortise deadbolt and latch sections;
   - the escutcheon cylinder-to-hub distance (0.0635);
   - knuckle Ø 15 mm;
   - the closer arm lengths and mount points (§2.4).
3. Custom normals surviving `finish()`: the probe decides (§1.8).
4. Vertex colours on meshes that have none: does Unity read white or black? It decides P-3's default.
5. Run and Exit readability (T6). The pull-face head dip with a 10° ajar toward the camera (§3.4).
6. The mirrored-door (negative scale) path through the RT bridge (§7.3 item 4) and through static batching (doors are dynamic, so it should not apply).
7. The map trims' coplanar z-fight is hidden by the 2 mm lining. That is computed, not captured (`04` §1.1).
8. Leaf damage variants and splinters (audit §3.7) are not in v1.
9. Line numbers in `MapWorld` drift: the map chat has uncommitted edits.

---

## 14. Sources

**Project files read for this spec (2026-10-03):**
- `research/interactables/00`–`06` (all, in full);
- `interaction_audit/10_audit_report.md` §3–§4 and `FrontRoomsShotTimings.proposal.cs.txt`;
- `glass/destruction/04_unity_implementation.md` §0, §1.3, §4, §7, §8, §10–§11;
- `office_and_film/22_era_lock.md` §1–§3;
- `Documentation/BACKROOMS_VISUAL_SPEC.md`, `FONTS_PERIOD_1990.md`, `VISUAL_CHAT_TASKS.md`;
- `Tools/Blender/frontrooms_kit/kitlib.py` (all), `build_asset.py`, `assets/crt_monitor.py`, `assets/interior_window.py`, `assets/doorway_studs.py`;
- `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` and `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` (both identical in `proj_int`);
- `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` (§1–§6 citations), `FrontRoomsMap.cs:676-714`, `FrontRoomsRoomModuleData.cs:52-78`, `FrontRoomsModuleUnits.cs:33-94`;
- `Assets/Scripts/FrontRoomsRoomStream.cs:1341-1500`, `Assets/Scripts/FrontRoomsRoomRule.cs`, `Assets/Scripts/Rendering/FrontRoomsSurfaces.cs:20-29`;
- `Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs:117-330`;
- `Assets/Resources/Rendering/FrontRoomsSurface.shader:1-60, 183, 189-208`;
- `Assets/Resources/Surfaces/*.mat` (shader and `_BaseColor`);
- `ProjectSettings/QualitySettings.asset` (`lodBias`);
- `Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs:677-701`;
- `Office/FrontRoomsFurniturePile.cs:140-160`;
- Blender 4.3 `io_scene_fbx/__init__.py:399-406`.

**Measured for this spec:**
- texture mean albedos (§2.2, §7.3), with Blender 4.3 headless over 4,096 samples per texture;
- the A2 swing clearances (§1.5), with a plan-section sampler, 0.1° steps;
- the closer linkage (§2.4), with a grid search under leaf and casing constraints.

The scripts are in the session scratchpad (`texmean*.py`, `swing.py`, `closer2.py`).

**Figma:** not re-read for this spec. The IP rules (HR03, IR01–IR06, K00, R44) come through `05` §3, which read them with `get_metadata` and `get_screenshot`.

**Web:** no new pages. Every web claim is carried from `02` §13, `04` §10, `05` §10 and `06` §8 with their [read] or [search] status.
