# D1.6 doors · locks · keys — design (stage: Design, workflow d16-doors-locks-keys, 游戏视觉)

Written 2026-10-22 for the relaunch of `wf_c071884a-103` (the first run built nothing, `30_status.md`). Nothing was built, run or changed by this stage. It is the build contract for the Build, Capture and Verify stages and the source of the map contract for 关卡设计.

**Mode decided (A12, Red 2026-10-22 07:5x): every map door is double-acting (DA).** The single-acting stop door of `10_spec.md` §1 stays only as a dormant rig mode. Wherever this file and `10_spec.md` disagree, this file wins for D1.6.

## 0. Sources (the tags used on every requirement)

| Tag | Source (read 2026-10-22) |
|---|---|
| `[A12]` | Task paragraph "A12 IS DECIDED"; `proposal/03_for_red.md:3`; `system_design/systems.json` 2026-10-22f A12 + **A19** ("Double-acting everywhere, EXIT and stairwell too … stays where it is left; handles kept … shutting from inside its sweep keeps today's step-aside; after the Alpha") |
| `[IXa]`…`[IXe]` | Task paragraph "交互设计 REQUIREMENTS FOR A12", items (a)–(e) |
| `[RULE1]`…`[RULE4]` | Task paragraph "RED CHANGED THE DOOR RULE", items (1)–(4) |
| `[CTX]` | Task paragraph CONTEXT: A09 / SD04 calls (deadbolt + knob, still-looks-locked, LOD0/1/2, "key rack", C14 look + E, C15 1 in 3, broken stays broken, B·4, no motor or closer) |
| `[RES]` | Task paragraph RESUME NOTE (bases; KeyAssembly keeps a trigger) |
| `[HR]` | Task HARD RULES (`Takeover_2026-10-18/briefs/COMMON.md`) |
| `[VCT]` | `Documentation/VISUAL_CHAT_TASKS.md` §0a′ D1.6 row, lines 191–200 |
| `[SEAM:n]` | `Assets/Scripts/FrontRoomsMap/FrontRoomsKitSeam.cs` (sha1 b33b1d8d), line n |
| `[MW:n]` | `FrontRoomsMapWorld.cs` main **7ef0ca45**, line n |
| `[DS:n]` / `[TIM:n]` | `FrontRoomsDoorShot.cs` **b1b230e8** / `FrontRoomsShotTimings.cs` **865e3e10** |
| `[G:n]` | `FrontRooms3DGame.cs` **88c93b09** |
| `[SPEC §]` | `research/interactables/10_spec.md` |
| `[04 §5]` / `[05 §6]` | `04_door_re8_gap.md` §5 (Option B) / `05_locked_door_type.md` §6 |
| `[PLAN]` | `~/FrontRoomsMapWork/notes/door_shots_plan.md` §3.3–§3.4, §4 (V1–V6), §8 |
| `[G2 §4.n]` | `build_g2_the_keyable_lock_and_door_hardware.md` §4 |
| `[40 §n]` | `40_merged.md` |
| `[DEC #n]` | `research/ui_key_icon/hud_build/DECISIONS.md` |
| `[KP1]` | `research/ui_key_icon/lock_motion/contract_map_KP1.md` |
| `[HELD]` | `research/ui_key_icon/held_pose/held_arrangement.json` (2026-10-14) |
| `[INSP K]` | `research/inspect/10_design.md:159` (row K, first key) |
| `[G12]` | `research/inspect/contracts/graphic_inspect.md:29`; `GRAPHIC_VISUAL_TASKS.md:366` |
| `[RD10]` | `research/codex_audit/00_main_state.md:3, :63` |
| `[SYS x]` | `system_design/systems.json` 2026-10-22f decision x (A13, C14, C15, C23) |
| `[A12MAP]` | **Not a source of authority**, a fact: the map-side A12 swing is being built in `W/proj_a12` (another session; files dated 08:05–08:13): `SwingAwayFrom`, a geometric `FaceOf`, `doorSides`, `DoorMotion.Through`, `IsLatched`. D1.6 uses only what `[A12]` states and re-checks against whatever lands |

Main bases for the contract (re-check every sha1 at package time `[HR]`): MapWorld 7ef0ca45, Game 88c93b09, Game.Mobile 1f6124fa, CameraRig a77221fb, ShotTimings 865e3e10, DoorShot b1b230e8, GlassShot 7f3c2929, KitSeam b33b1d8d, KitLibrary 1f7dac20, InteractableKit.Window 07dc067e, RenderSetup 256cf3a4, KitImporter 08daf35f. Main HEAD 0ea38bb7 (from `W/proj_a12/MKCLONE.txt`).

---

## 1. Decisions in one table

| # | Decision | Source |
|---|---|---|
| D1 | `FrontRoomsDoorRig.Acting = DoorActing.Double` for every map door, Exit and stairwell included. `DoorActing.Single` (stops, butts, A2, handing mirror, closers) stays in code as a dormant parameter and is never selected by D1.6 | `[A12]`, `[RULE1]` |
| D2 | DA geometry = Option B: top and bottom pivots (caps only), a radius hinge stile, 3 mm gaps, black seal fins on the wall centre plane, the frame sleeve covering the map trims, a 12 mm saddle, casing on both faces | `[A12]`, `[04 §5.1]`, `[SPEC §1.4]` |
| D3 | The pivot stays on the wall centre plane at the map's hinge point: **no A2 offset, no handing mirror**, S = the door root's +X face, P = −X, `S_sign` fixed at +1 | `[A12]` (map contract), `[MW:1471-1474]` |
| D4 | No self-centring, no spring hinges, no closers on map doors. The leaf rests where the map leaves it | `[IXa]`, `[CTX]` |
| D5 | Handles kept: lever on free doors; knob + deadbolt + keyed cylinder on locked doors, keyhole at Y 1.000 on **both** faces; `KnobTurn` at 0.92 stays | `[A12]`, `[IXb]`, `[TIM:71]` |
| D6 | DA latch: double-bevel latchbolts and symmetric-lip strikes (new `_DA` lock parts). The brush fins are continuous; a thrown bolt passes through the brush (render-only) | `[IXb]`, D2 (a single-bevel latch cannot be closed from both faces) |
| D7 | The rebuilt kits get **new `_DA` names**. The 9 stop-frame kits in main are not overwritten (they are the dormant single mode; main's GUIDs untouched) | `[RULE1]`, `[A12]` "dormant" |
| D8 | The leaf rig follows the map's `Door leaf` collider offset (crooked hang, rattle and blow jolts) and adds its own `Jolt` (the unlock shift). No map change for jolts | `[MW:2954-3030]`, `[SEAM:34-35]` |
| D9 | Lever/knob beats on open and rattle, the latch click, the strike detach and the broken-leaf wobble run in the facade on map events. No map change for them | `[PLAN §3.4]`, `[MW:96-197]` |
| D10 | Keys: unscaled `Key · zone {id}` root, ring + key + tag, the trigger collider **on the root object** | `[RES]`, `[MW:3782]`, `[SPEC §4.1]` |
| D11 | HeldItem: layer **30**, a renderer feature that depth-punches and redraws HeldItem after opaques; desktop + iOS; off on WebGL | `[VCT:196-197]`, `[PLAN V3]`, `[HR]` |
| D12 | One shot-look driver for V4 (unlock + glass) and V5 (the 0.6 shot light) | `[VCT:198-199]`, `[PLAN V4-V5]` |
| D13 | The 3 RD10 materials: `Door_Enamel`, `Prop_KeyTagNo` (Courier Prime Bold atlas, 4096²), `Prop_SignEngraved` (2×2 atlas) | `[RD10]`, `[G12]`, `[SPEC §2.4, §4.1]` |
| D14 | LOD0/1/2 on every D1.6 model (P-1 is in main: `kitlib.make_lods`, `build_asset.py:51-55`) | `[CTX]` call 8, `[SPEC §8 P-1]` |

---

## 2. The DA door kit (Blender, `Tools/Blender/frontrooms_kit/assets/`)

### 2.1 Assets to build (new files only; `kitlib.py` and `build_asset.py` untouched `[SPEC §10.0]`)

| New kit | Module | From (today's single-acting module) | Notes |
|---|---|---|---|
| `Kit_DoorFrame_Wood_DA` | `interact_door_frame_wood_da.py` | `interact_door_frame_wood.py` | walnut casing, oak saddle, brass pivot caps |
| `Kit_DoorFrame_Steel_DA` (+ VARIANT `Kit_DoorFrame_Steel_Alu_DA`: `Prop_SteelBrown` → `Prop_Aluminium`) | `interact_door_frame_steel_da.py` | `interact_door_frame_steel.py` | pressed-steel sleeve, bronze saddle (merge M1 rule, `[40 §2]`), chrome caps |
| `Kit_DoorLeaf_Veneer_DA` | `interact_door_leaf_veneer_da.py` | `…_veneer.py` | `Door_Veneer`, brass |
| `Kit_DoorLeaf_Veneer_Oak_DA` | `interact_door_leaf_veneer_oak_da.py` | `…_veneer_oak.py` (its own mesh, grain fix H3) | `Prop_WoodOak`, chrome |
| `Kit_DoorLeaf_Steel_DA` (+ VARIANT `Kit_DoorLeaf_Steel_PaintedMetal_DA`: `Door_Enamel` → `Painted_Metal`) | `interact_door_leaf_steel_da.py` | `…_steel.py` | kick plates both faces (as today) |
| `Kit_DoorLeaf_SteelLite_DA` | `interact_door_leaf_steel_lite_da.py` | `…_steel_lite.py` | P2 (RN-K option, Red's call 9) |
| `Kit_DoorLeaf_Ward_DA` | `interact_door_leaf_ward_da.py` | `…_ward.py` | P2; laminate, stainless armor plate on **both** faces, **no** push plate and **no** D-pull: it takes the lever set (Q-RN, §11) |
| `Kit_Lock_Latchbolt_Bored_DA` (+ `_Brass`) | `interact_lock_latch_bored_da.py` | `…_latch_bored.py` | double bevel 30°/30°, no deadlatch plunger; throw 0.013 |
| `Kit_Lock_Latchbolt_Mortise_DA` | `interact_lock_latch_mortise_da.py` | `…_latch_mortise.py` | double bevel, no plunger; throw 0.019 |
| `Kit_Lock_StrikeBored_DA` (+ `_Brass`) | `interact_lock_strike_bored_da.py` | `…_strike_bored.py` | flat plate, a ≤ 1 mm eased lip on **both** edges, dust box |
| `Kit_Lock_StrikeMortise_DA` | `interact_lock_strike_mortise_da.py` | `…_strike_mortise.py` | as above; latch box 0.015, deadbolt box 0.023 `[G2 §4.4]` |

Shared code goes in `interact_door_da_common.py` (no `NAME`), importing `interact_door_common` (972 lines; never edited). Each `_DA` module copies the single-acting module's profile code it needs rather than patching it, so the single kits rebuild byte-identically. All parts: `kit.no_collider()`, tags `interactable`, `door`, plus **`double_acting`** and the existing `door_type_*` / `frame_*` tags (the sound chat reads them, `[SPEC §12]`). Hard rules: ≤ 4 slots per asset, dominant slot first, `fr_wear` on every part, `fr_lod2_drop` on screws and caps `[SPEC §1.8, §10.0]`.

### 2.2 DA section (door root D, S = +X, metres; pivot = root origin)

| Part | X | Y | Z | Source |
|---|---|---|---|---|
| Pivot axis | 0 | vertical | 0 | `[A12]`, `[MW:1473]` (hinge at `start + along·(c − w/2)`, wall centre line) |
| Visual leaf | ±0.022 (44 mm) | 0.015 → 2.095 | hinge stile = half-cylinder **R 0.022 about (0, 0)** (reaches Z −0.022), 96 segments; latch end flat at **0.995** (the arc r 0.995 is within 0.25 mm) with 1.5 mm arrises; no 3° bevel (a DA leaf is symmetric) | `[04 §5.1]`, `[SPEC §1.4]` |
| Lining | ±0.0795 | 0 → 2.098 | hinge −0.0005 → 0.002 with a **seal slot \|X\| < 0.025**; latch 0.998 → 1.0005; head skin 2.098 → 2.1005 | `[04 §5.1]`, `[SPEC §1.4]` |
| Hinge seal (black brush, `Prop_PlasticBlack`) | \|X\| ≤ 0.025 | 0.012 → 2.098 | 0 → 0.0015 | `[04 §5.1]` |
| Latch fin (brush) | \|X\| ≤ 0.004 | 0.011 → 2.098 | 0.994 → 0.998 (1 mm into the shut leaf end) | `[04 §5.1]` |
| Head fin (brush) | \|X\| ≤ 0.004 | 2.093 → 2.098 | 0 → 0.998 | `[04 §5.1]` |
| Casing, both faces, sleeve mode | S 0.0795 → 0.105; P −0.105 → −0.0795 | jambs 0–2.175; head 2.098–2.175 | hinge −0.075 → 0.002; latch 0.998 → 1.075 | encloses the map trims (0.07 × 0.02 proud, `[MW:1454-1463]`) `[A12]`, `[SPEC §1.4]` |
| Saddle | ±0.076, flat top ±0.052, 1:2 bevels | 0 → **0.012** | 0.002 → 0.998 | `[A12]`, `[SPEC §1.4]`; 3 mm under the leaf both ways |
| Pivot caps | on the axis | head: a Ø 0.032 × 0.0015 cap flush in the head soffit; leaf top: a 0.060 × 0.022 top-pivot plate; floor: a Ø 0.038 × 0.0015 cover plate at the saddle's hinge end; leaf foot: a 0.060 × 0.022 bottom-arm plate (ESTIMATE, centre-hung floor pivot) | — | `[A12]` "caps only" |
| Removed vs single | — | — | stops, silencers, butt hinges (frame and leaf halves), `closer_shoe` / `closer_mount_s` anchors | `[A12]`, `[IXa]` |
| Shadow proxy (facade, not kit) | ±0.05 | 0.015 → 2.095 | 0.02 → 0.98 | `[SPEC §1.7]` |

**Anchors (sidecar, Unity, door root).** Frames: `pivot_axis` (0, 0, 0) + `pivot_axis_dir` (0, 0.10, 0); `strike` (0, 0.968, 0.998); `strike_bored` (0, 1.000, 0.998); `head_dust_a/b` (0, 2.098, 0.05 / 0.95); `threshold` (0, 0.012, 0.50); `exit_sign_p` (−0.080, 2.18, 0.50) on `_Alu_DA` (the 10-08 value, `[40 §0]`). Leaves: `pivot_axis` + `_dir`; `rose_s/p` (±0.022, 1.000, 0.920); `escutcheon_s/p` (±0.022, 0.968, 0.920); `latchbolt` (0, 1.000 bored / 0.9365 mortise, 0.995) + `_dir` (+0.10 Z); `deadbolt` (0, 1.000, 0.995) + `_dir`; `sign_s/p` (±0.022, 1.524, 0.500); `tagplate_s/p` (±0.022, 1.000, 0.790); `kick_s/p`; `latch_edge_bottom/top` (0, 0.015 / 2.095, 0.995); `damage_latch` (0, 1.10, 0.995). `meta["acting"] = "double"`. Mortise armor-front and faceplate openings as `[G2 §4.6]`, centred at Z 0.995.

**Budgets (LOD0 / LOD1 / LOD2 tris, ±15 %; `LODS` and `LOD_DISTANCES = (4, 12, None)` as today):** Wood_DA 3,000 / 1,200 / 270; Steel_DA 3,400 / 1,300 / 280; Veneer_DA and Veneer_Oak_DA 2,900 / 1,050 / 140; Steel_DA leaf 4,400 / 1,600 / 170; SteelLite_DA 5,400 / 2,000 / 210; Ward_DA 4,800 / 1,700 / 180; DA latches 520 / 210 / 40 (LOD dist 1 / 3 / 8); DA strikes 700 / 280 / 50 (1.5 / 4 / 12). Hero LOD0 rules of `[SPEC §1.8]` (≥ 1.5 mm bevels on leaves, ≥ 96 segments on the radius stile and caps) hold at 0.3 m `[HR]`.

### 2.3 Blender self-checks (scratchpad; Build stage)

- **T1-DA gap rays** (BVH, both faces, 0.5° grid, through hinge, latch, head and floor gaps): run twice, **fins on** and **fins off** (the "brush strip" of `[IXc]`). Report per gap: through-ray count, the widest see-through slit (mm) and its angular window. Prediction to confirm or refute: fins off, the latch and head slits are straight 3 mm × 44 mm channels, open within ±3.9° (atan 3/44) of their plane; the hinge has no straight path (the leaf runs into the wall end); the floor needs ≤ 3.9° (eye 1.62 m → ≥ 23 m away). Fins on: zero rays through.
- **T2-DA swing** (0–95° in 0.5°, **both directions**): leaf + face hardware against frame parts, a stand-in wall (\|X\| ≤ 0.08 outside the opening) and a stand-in of the map trims. The latch end must clear the latch lining (r ≤ 0.995 < 0.998) at every angle. The near-pivot overlap with the hinge jamb, casing and trim beyond ~60° is Option B's known junction (`[04 §5.2]`, "unchanged from today"); report its depth and whether it is **visible from any eye point outside the wall volume**. Fix ladder if visible: (1) a concave cove on the hinge-side casing's reveal arris, both faces, without exposing the trims; (2) ask the map for 90° rest in DA. No other overlap is allowed.
- Asserts per module: envelopes, anchors, fins on the centre plane, slots ≤ 4, budgets.
- Renders: both faces at 1.5 m, the hinge stile and caps at 0.3 m, the latch edge with the DA latch at 0.3 m, 45° each way.

---

## 3. Door family: what `DressDoor` builds, per member, mode and side

### 3.1 Member and lock choice

- **Member** (`FrontRoomsDoorMember { Lobby, Office, Run, Exit }`): Office when the door's Standard (non-Low) side has `ZoneTheme.Office`, else Lobby (`[SPEC §2.1]`; computed from `map.Cache.ZoneOf(...)` as `DressWindow` does). Run and Exit come from a map hook when those doors exist (`map.DoorMemberFor(door)`, contract M8); A19 puts the EXIT and the stairwell doors in DA too `[A12]`.
- **Lock** = `map.LockedByRule(door)` at build time (`[MW:2644]`; C15 1 in 3 and A13's floor rule are the map's `[SYS C15, A13]`). A door unlocked by a key **keeps the LOCKED model** and opens on E `[CTX]` call 5: its deadbolt is set retracted at dress time (`map.IsUnlockedForTools(door)` today, a non-tools `IsUnlocked` asked in M7) and stays retracted for the run, from both faces `[IXe]`.
- **Broken** (`door.broken`): the leaf follows the crooked collider (§4.4); the strike lies detached on the floor (deterministic pose from the edge id); the deadbolt stays thrown on a broken locked door `[SPEC §3.4]`, `[CTX]`.

### 3.2 Parts table (DA mode; data, not code branches, so a variant is a row `[RULE1]`)

| Code | Frame | Leaf | Lockset, both faces | Bolts / strike | Plates | Pri |
|---|---|---|---|---|---|---|
| **L0-F** | `Kit_DoorFrame_Wood_DA` | `Kit_DoorLeaf_Veneer_DA` | `Kit_Lock_Rose_Brass` + `Kit_Lock_Lever_Brass` | `Kit_Lock_Latchbolt_Bored_Brass_DA`; `Kit_Lock_StrikeBored_Brass_DA` | — | P1 |
| **L0-K** | `Kit_DoorFrame_Steel_DA` | `Kit_DoorLeaf_Steel_DA` | `Kit_Lock_Escutcheon` + `Kit_Lock_CylinderShell` + `Kit_Lock_Plug` + `Kit_Lock_Knob` (chrome) | `Kit_Lock_Deadbolt`; `Kit_Lock_Latchbolt_Mortise_DA`; `Kit_Lock_StrikeMortise_DA` | `Kit_DoorSign` cell 0 EMPLOYEES ONLY; `Kit_DoorNumberPlate{,_Blue,_White}` | P1 |
| **OF-F** | `Kit_DoorFrame_Steel_DA` | `Kit_DoorLeaf_Veneer_Oak_DA` | `Kit_Lock_Rose` + `Kit_Lock_Lever` (chrome) | bored DA latch + strike (chrome) | — | P1 |
| **OF-K** | `Kit_DoorFrame_Steel_DA` | `Kit_DoorLeaf_Steel_DA` | mortise set (chrome) | as L0-K | as L0-K | P1 |
| RN-F | `Kit_DoorFrame_Steel_DA` | `Kit_DoorLeaf_Ward_DA` | rose + lever (chrome) | bored DA | — | P2 (Q-RN) |
| RN-K | `Kit_DoorFrame_Steel_DA` | `Kit_DoorLeaf_Steel_DA` (`SteelLite_DA` only if Red picks call 9) | mortise set | as L0-K | sign cell 1 STAFF ONLY; plates | P2 |
| EX-F / EX-K | `Kit_DoorFrame_Steel_Alu_DA` | `Veneer_Oak_DA` / `Steel_DA` | lever set / mortise set | bored / mortise DA | plates on EX-K; the lit EXIT sign is the exit-sign track's wall mount (C23 `LockLit`) | P2 (Q-EX) |

No closers on any member `[IXa]`. `Kit_DoorCloser_*` and `Kit_ExitDevice_Crossbar` are spawned by no DA row (Q-EX below).

### 3.3 Hierarchy and placement (all `FrontRoomsKitLibrary.Spawn(..., colliders: false)`, `[SPEC §6.2]`)

| Object | Parent | localPosition | localRotation | localScale | Label |
|---|---|---|---|---|---|
| frame | chunk root | `hinge.localPosition` (the door root) | `door.closed` | 1 | `Door frame (kit)` |
| leaf rig (empty, `FrontRoomsDoorRig`) | `Door hinge {a}-{b}` | 0 (DA) | identity | 1 (DA) | `Leaf rig` |
| leaf | leaf rig | 0 | identity | 1 | `Leaf (kit)` |
| shadow proxy (cube, collider destroyed, ShadowsOnly) | leaf rig | (0, 1.055, 0.500) | identity | (0.10, 2.08, 0.96) | `Leaf shadow` |
| face hardware: rose, lever, escutcheon, shell, knob | leaf rig | leaf anchor + the part's offset from the escutcheon anchors (`keyhole` (0, 0.032, 0.0095), `knob_pivot` (0, −0.0315, 0.002)) | S `Euler(0, 90, 0)`; P `Euler(0, −90, 0)` | S 1; P (−1, 1, 1) (the lever still points at the hinge) | `Lock rose s`, `Lock knob p`, … |
| plug | leaf rig | the escutcheon's `keyhole` | as the face | (S_sign = 1, 1, 1) on both faces: **never mirrored** | `Lock plug s/p` `[G2 §4.1]` |
| sign, number plate | leaf rig | `sign_*`, `tagplate_*` | as the face | (1, 1, 1) | `Door sign s`, `Door plate p` |
| deadbolt, latchbolt | leaf rig | `deadbolt`, `latchbolt` | identity | 1 | `Lock deadbolt`, `Lock latch` |
| strike | frame | `strike` / `strike_bored` | `Euler(0, 180, 0)` | 1 | `Lock strike` |

Then: the `Door leaf` MeshRenderer is **disabled, never destroyed**; its collider (0.05 × 2.08 × 0.98 at (0, 1.04, 0.50)) is untouched `[SPEC §6.7]`, `[MW:1475-1479]`. No child name starts with `Door hinge`, equals `Light`, or starts with `fluorescent light` `[SPEC §1.6]`. Spawns pass `inspectable: true` (the sign's EXAMINE block is `ship: false`; door signs are read in place, `[INSP]` row D).

**Dormant single mode (documented, not selected):** the 9 stop kits of `[SPEC §2.3]`, leaf rig at (−S_sign·0.0295, 0, −0.0098) with scale (S_sign, 1, 1), frame scale (S_sign, 1, 1), `S_sign = −door.swing`, closers on Office/Run/Exit, and the map's A2 two-line change (§9 M-A2). Selecting it needs the map's single-acting swing back; D1.6 ships neither.

---

## 4. Component APIs (exact members)

### 4.1 `FrontRoomsDoorRig : MonoBehaviour, IFrontRoomsDoorRig` — `Assets/Scripts/Rendering/Doors/FrontRoomsDoorRig.cs`, on `Leaf rig`

Interface members, exactly `[SEAM:19-36]`:

```csharp
public Vector3 Keyhole(DoorFace face);      // world: that face's escutcheon `keyhole` (door X ±0.0315, Y 1.000, Z 0.920) through the leaf rig's current pose
public Vector3 KeyholeDir(DoorFace face);   // world unit into the lock from that face: leaf rig −X for S, +X for P
public Vector3 KeyholeUp(DoorFace face);    // world unit "cuts up": leaf rig +Y
public void SetPlug(DoorFace face, float degrees);  // that face's plug only; + = cuts toward the hinge; negated on P [G2 §4.7]
public void SetKnob(DoorFace face, float degrees);  // locked model: both knobs (passage through-spindle), + = retract, negated on the mirrored P part; free: no-op
public void SetLever(DoorFace face, float degrees); // free model: both levers, + = grip down, negated on P; locked: no-op
public void SetDeadbolt(float t);           // 0 thrown .. 1 retracted: slide part −Z by 0.025·t; the value persists (unlocked doors stay drawn)
public void SetLatch(float t);              // 0 out .. 1 drawn: slide −Z by 0.013 (bored) or 0.019 (mortise)·t
public void Jolt(float metres);             // see below
```

- **Free-door keyhole fallback:** `Keyhole(face)` on a free door returns the rose spindle point 0.0065 proud, so a caller never gets garbage `[MW:2738]`.
- **`Jolt(metres)`** (DA): turns the leaf rig about the pivot so the latch end moves |metres| toward **−door.swing·X** (the side the leaf opens to, i.e. away from the opener once the map has turned `swing` for them `[A12]`, `[A12MAP]`), with the map's jolt shape (out in the first fifth, eased home) over a spring chosen by size: ≤ 1.6 mm `Unlock.BoltJolt` 0.08 s, ≤ 2.5 mm `Rattle.JoltSpring` 0.06 s, else `DoorBreak.LeafJoltSpring` 0.12 s `[TIM:56, 94, 171]`. Never moves the hinge or the collider `[SEAM:34-35]`. The map's only call today is the unlock shift `[MW:2682]`, unchanged.
- **Collider follow (D8):** each LateUpdate the rig composes `M = colliderLocal ∘ home⁻¹` (home = `door.leafHome`, identity rotation) onto its base pose, so the map's crooked hang (`PoseLeaf`, `[MW:3007-3030]`), rattle jolts (`Rattle`, `[MW:2954]`) and Relay blows (`JoltForBlow`) show on the kit leaf with no map change. Applied once synchronously in `DressDoor` (edit-mode preview, rebuilt broken doors).

Concrete members beyond the seam:

```csharp
public FrontRoomsMapWorld.Door Door { get; }
public DoorActing Acting { get; }            // Double (D1.6) | Single (dormant)
public float SSign { get; }                  // +1 in DA (no handing mirror)
public FrontRoomsDoorMember Member { get; }
public bool LockedModel { get; }
public void DetachStrike(Vector3 from);      // render-only scripted fall (0.4 s) to a floor rest pose; idempotent
public void ForceLod0(bool on);              // LODGroup.ForceLOD(0 / -1) on every LODGroup under the frame and the leaf rig
public static FrontRoomsDoorRig Nearest(Vector3 point, float within);
public static IReadOnlyList<FrontRoomsDoorRig> All { get; }
```

One hidden runtime host (`FrontRoomsDoorRigDriver`, DontSave, like `FrontRoomsInspectLook`'s) runs all rigs' LateUpdate work: the follow, jolt springs and the event beats of §5.2. Beats advance on `Time.deltaTime` only while `!map.DoorsPaused` `[MW:220]`.

### 4.2 Facade — `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs` (partial of the window facade, `[40 §1]`)

```csharp
public enum DoorActing { Double, Single }
public enum FrontRoomsDoorMember { Lobby, Office, Run, Exit }
public static DoorActing Acting = DoorActing.Double;
public static bool DisableDoorDressForTools { get; set; }
public static Func<FrontRoomsMapWorld.Door, FrontRoomsDoorMember?> DoorMemberOverride { get; set; } // tools / LD
public static FrontRoomsDoorRig DressDoor(FrontRoomsMapWorld map, FrontRoomsMapWorld.Door door, Transform chunkRoot);
```

`DressDoor` returns null (and leaves the cube leaf visible) when tools disable it, an input is null, or any kit of the row is missing; on an exception it destroys what it spawned and re-enables the cube renderer, as `DressWindow` does `[Window.cs:62-92]`. On its first call per map it binds the map's events (§5.2) through a hidden host on the map's GameObject (unbinds on destroy; Play Mode only).

### 4.3 `FrontRoomsKeyAssembly : MonoBehaviour, IFrontRoomsKeyRig` — `Assets/Scripts/Rendering/Keys/FrontRoomsKeyAssembly.cs`, on `Key · zone {id}`

Interface members, exactly `[SEAM:44-56]`:

```csharp
public Transform Root { get; }              // this transform: the unscaled empty the map moves
public void PoseInLock(Pose shoulder);      // Kit_Key_Zone origin (the shoulder) at the pose, +Z insertion, +Y cuts up; ring through `ring_hole` hanging in the plane (key X, down); tag hanging from the ring's `tag_contact`
public void PoseHeld(Pose lensFacing);      // root = pose (the [HELD] anchor, +Z to the lens); parts in [HELD]'s per-shape local poses
public void Sway(float degrees, float hz, float decay); // θ(t) = degrees·e^(−decay·t)·sin(2π·hz·t) on the tag about its `hole` (and the ring about the key's hole when posed in the lock)
public void Release();                      // stop the sway, parts back to the Default layer
```

Concrete members:

```csharp
public FrontRoomsKeyIdentity Identity { get; }
public KeyPose Pose { get; }                 // Hung | Flat | OverEdge | Held | InLock
public string TagKit { get; }                // e.g. "Kit_KeyTag_Rect_Blue": FrontRoomsInspect.OpenHeld's kit for the first-key close-up [INSP K]
public BoxCollider Trigger { get; }          // on Root itself; null when the map asked for none
public Bounds LocalBounds { get; }
```

Facade (`FrontRoomsInteractableKit.Key.cs`):

```csharp
public enum KeyPlacement { HostHung, Surface, SurfaceOverEdge, Floor }
public static FrontRoomsKeyAssembly DressKey(Transform root, FrontRoomsKeyIdentity id, KeyPlacement placement, bool trigger);
public static Transform DressKeyHost(Transform chunkRoot, string hostKit, Vector3 localPos, float yaw, out Pose keyHook); // Kit_KeyHookBoard | Kit_KeyCabinet | Kit_KeyHook, label "Key host (kit)"
public static FrontRoomsKeyAssembly DressShotKey(Transform parent, FrontRoomsKeyIdentity id); // the unlock shot's key: no trigger
```

`FrontRoomsKeyIdentity` (`Rendering/Keys/FrontRoomsKeyIdentity.cs`): `Shape { Rect, Round, Long }`, `Tint { Red, Blue, White }`, `int number` 0–99, and `static FrontRoomsKeyIdentity Stub(GridCoord zone)` = **the same hash as the HUD's `FrontRoomsKeyHud.StubIdentity`** (`h = (uint)(x·73856093 ^ y·19349663)·2654435761u`; shape (h/100)%3, tint (h/300)%3, number h%100), so the tag, the door plates and the HUD agree until the map's rule lands (`[DEC #20]`; the HUD workflow is told to convert to this type or keep the identical hash).

---

## 5. Lock parts, beats and motion

### 5.1 Animatable parts (sidecar `motion`, `[SPEC §3.1]`)

Plug (rotate part −Z, 0→90°, `turnResistDeg` 10), knob (±40°, spring return), lever (0→35°, spring back), deadbolt (slide −Z 0.025, authored thrown), latchbolt (slide −Z 0.013 / 0.019, authored extended), strike (static, detaches on a break). The key is one rigid mesh with anchors `shoulder`, `tip`, `grip`, `ring_hole` `[SPEC §3.3]`.

### 5.2 Beats (who drives what)

| Beat | Time (s) | Driver | Parts | Source |
|---|---|---|---|---|
| `KeyApproach` | 0.30–0.55 | DoorShot hunk M4 | shot key from viewport (0.85, 0.10) at 0.30 m to the shoulder 0.055 out (tip 0.030 out), `LookRotation(−out, up)` | `[SPEC §3.4]`, `[TIM:52, 69]` |
| `KeyInsert` (+ `PinCatch` at 60 % depth, 30 ms) | 0.55–0.68 | M4 | shoulder `keyhole + out·0.025` → `keyhole` | `[TIM:54]`, `[VCT:200]` |
| `KeyTurn` | 0.68–0.90 | DoorShot `DriveHardware` (exists, `[DS:638-661]`) + M4 | plug and key `PlugDegrees(t)` to 90°, key +Y toward the hinge; `Sway` on the ring and tag | `[DS:187-204]` |
| `BoltRetract` (commit) | 0.86 | DoorShot (exists) + map `CommitUnlock` → `rig.Jolt(LeafShift 1.5 mm)` | deadbolt 0 → 1 over 0.08 s | `[MW:2676-2685]` |
| `KeyWithdraw` | 0.90–1.15 | DoorShot + M4 | plug back by 1.00, key out by 1.15, then `Release` and destroy | `[TIM:57]` |
| `KnobTurn` / `KnobReturn` | 0.92–1.00 / 1.20–1.35 | DoorShot (exists; push side, which every DA unlock from a latched leaf is `[A12MAP]`) | knob 40°, latch follows | `[TIM:71-72]`, `[IXb]` |
| Leaf leaves the frame | 1.00 | map `ReleaseKeyTurn(latchDrawn)` | hinge (map) | `[MW:2699]` |
| Open, free door | E | facade on `DoorHandleTurned(door, point)` | both levers 0→35° over `Open.LeverDown` 0.08, held until the hinge passes 0.5°, back over 0.10 starting 0.02 s later; latch follows | `[PLAN §3.4]`, `[TIM:83]` |
| Open, locked-model door already unlocked | E | same handler | both knobs 0→40° over 0.08, same hold and return | `[PLAN §3.4]` |
| Rattle | E without key | facade on `DoorRattled(door, point)`; jolts from the collider follow | knobs 0→40° over 0.05, held to 0.20, back 0.20–0.40; latch follows; leaf 2 × 2 mm away from the trier | `[TIM:94-97]`, `[A12MAP]` (`SwingAwayFrom` before `Rattle`) |
| Latch click | the leaf lands shut | facade on `DoorLatched(door, pos)` | latch 0 → 1 → 0 in 0.06 s (the double bevel riding the strike) | D6 |
| Break | Relay | facade on `DoorBrokenFrom(door, from, …)` | `DetachStrike(from)`; leaf crooked via the follow | `[SPEC §3.4]`, `[MW:84-86]` |
| Broken, tried | E | facade on `DoorBrokenTried(door, pos)` | optional ±0.6° damped wobble of the hanging leaf (2.4 Hz, 0.3 s) | `[DEC #11, #20]` |

Face for event handlers = the sign of `(point − door.position)·rootX` (geometric, D3). Sound is unchanged: the beats are `FrontRoomsDoorShot.Beat` and the map's events; no motor or closer layers; DA has no self-return `[RULE4 Foley]`, `[IXa]`.

---

## 6. The key assembly

- **Root** = an unscaled empty named `Key · zone {id}` (map contract M3). Parts under it: `Kit_KeyRing` (`Key ring`), `Kit_Key_Zone` (`Key`), `Kit_KeyTag_{Rect,Round,Long}{,_Blue,_White}` (`Key tag`), all `Spawn(colliders: false, inspectable: false)` (a lying key shows `E · TAKE KEY`, no eye, `[DEC #18]`). The tag's `Prop_KeyTagNo` submesh gets a MaterialPropertyBlock `_BaseMap_ST = (1, 1, (n % 10)/10, −(n / 10)/10)` (sidecar `numberAtlas.setCell`); the shape and tint pick the variant `[SPEC §4.1]`.
- **Trigger** (`[RES]`, `[MW:1001-1018]`): when the map asks (`KeyPickupNeedsE`, desktop since C14 `[G:644]`), a `BoxCollider{isTrigger}` **on the root GameObject** (TakeKeyByE matches `chunk.keys[i].go == trigger.gameObject`, `[MW:3782]`), centred on the posed `LocalBounds`, size = max(bounds + 0.02, (0.16, 0.12, 0.08)) m (aim affordance; 交互设计 may tune). The map registers it in `keyByCollider`. The assembly disables it on its first `PoseHeld`.
- **Rest poses:** *Hung* from the host's sidecar `hungPose` v2 (ring yaw 120°, ring lift, key twist −30°, tag twist 150°, tag on the wall side; key tip down, flats to the room; `[40 §2]` key hosts row). *Flat* (key flats down, ring and tag beside it), *over-edge* (tag hanging down the support's front face) per `[SPEC §4.1]`. Hung keys never spin, no emission `[SPEC §4.3]`.
- **Hosts** (case A, `[SPEC §4.3]`): `Kit_KeyHookBoard` (display name "key rack" `[CTX]`), `Kit_KeyCabinet`, `Kit_KeyHook`, from the module `KeySpot.host`; case C placement (procedural rooms) stays the map's choice; A13 puts the one floor key at the station `[SYS A13]`.
- **Held:** `PoseHeld` sets the root to the pose and lays the parts at `[HELD]`'s per-shape `localPosition/localRotation` (`shapes.Rect/Round/Long.parts`). During the 0.25 s travel the parts blend from their rest local poses to the held ones with the shot's ease-out on the assembly's own clock (Δt between `PoseHeld` calls, so a pause freezes it; no pop at contact).
- **HeldItem rule** `[VCT:197]`: inside `PoseHeld`, once `Dot(pose.position − cam.position, cam.forward) < 1.5 × KeyPickup.HeldDistance` (0.525 m), every part renderer goes to layer 30 and stays there until `Release`. The 0.25 s flight starts on Default, so a desk edge still occludes it. `cam` = `FrontRoomsHeldItem.ViewCamera` (the enabled game camera with no target texture and the highest depth, re-validated per frame).
- **First key (EXAMINE C21)** `[INSP K]`, `[DEC #18]`: the game hands `assembly.Root` and `assembly.TagKit` to `FrontRoomsInspect.OpenHeld` (in main, `Inspect.cs:382`); the parts ride the root; HeldItem keeps it unclipped at 0.20 m.
- **HUD hand-off P4** `[KP1]`, `[DEC #2]`: at `HoldUntil` the map hides or destroys the root; `Release` is the hook. Nothing else is owed by the assembly.
- **Unlock key** (M4): `DressShotKey` builds a transient assembly (same identity, Default layer) that the DoorShot poses with `PoseInLock` on §5.2's table and destroys at `KeyOutEnd` or after a cancel's `CancelKeyOut` 0.15 s.

---

## 7. HeldItem layer and pass (V3)

- **Layer 30** (unnamed is fine; 31 is the editor tools' scratch layer, `[MapTestScene.cs:114]`). Naming it "HeldItem" needs a `ProjectSettings/TagManager.asset` line, which is not ours: asked of Red as optional.
- **`FrontRoomsHeldItemFeature : ScriptableRendererFeature`** (`Assets/Scripts/Rendering/HeldItem/`), RenderGraph pass at `AfterRenderingOpaques`:
  1. **Depth punch:** draw layer-30 renderers with `FrontRooms/HeldItemDepthPunch` (`Resources/Rendering/`, ColorMask 0, ZTest Always, ZWrite On, `SV_Depth` = far plane, reversed-Z aware).
  2. **Redraw:** the same renderers with the URP forward tags, ZTest LEqual, opaque sort; the SSAO texture bound white (or `_SCREEN_SPACE_OCCLUSION` off) for these draws, so the background's AO never lands on the key.
  - Result: never clipped by a wall, correct self-occlusion, true depth in the attachment, so the after-transparents depth copy (`m_CopyDepthMode: 1`) carries it and the EXAMINE Bokeh DOF focuses on the key.
- **RenderSetup hunk** (visual-owned `Editor/Rendering`): register the feature like `EnsureSsao` `[RenderSetup.cs:111-124]`; set `m_OpaqueLayerMask`, `m_TransparentLayerMask`, `m_PrepassLayerMask` to `~(1 << 30)`. Nothing is on layer 30 today, so no current pixel changes.
- **WebGL:** `#if UNITY_WEBGL && !UNITY_EDITOR` the feature adds no pass and the assembly never switches layer (the map's spherecast clamp keeps it out of walls, `[DS:527]`). All quality levels share one URP asset (`QualitySettings.asset:51…316`), so the gate is in code `[HR]`. iOS keeps the pass (3–4 draws).

---

## 8. Shot look (V4), shot light (V5), V6 notes

**`FrontRoomsShotLook`** (`Assets/Scripts/Rendering/Shots/FrontRoomsShotLook.cs`): a static driver with one hidden LateUpdate host and runtime volumes (`CreateInstance` profiles, DontSave, base + delta read from the resolved stack at `ShotStarted` with its own weights at 0), as `FrontRoomsInspectLook` does `[InspectLook.cs:1-23]`.

| Volume | Weight | Deltas | Gate |
|---|---|---|---|
| `Post / shot Unlock` (priority 2) | `FrontRoomsDoorShot.Weight` `[DS:54]` | vignette +0.10, postExposure +0.15, DOF **Gaussian** start = lock depth + 0.12 m (lock ≈ 0.58 m), end = start + 2.5 m, max radius 1.0 | DOF only where `FrontRoomsInspectLook.DofSupported` (off on WebGL, iOS, Android); no motion blur anywhere `[PLAN V4]` |
| `Post / shot Glass` (priority 2) | `FrontRoomsGlassShot.Weight` `[GlassShot.cs:40]` | vignette +`GlassBreak.Vignette` 0.08; chromatic aberration +0.16 pulse over 0.25 s on `GlassShot.Beat` 3 (shatter) | CA pulse off with Reduce Flashing `[TIM:135]` |
| LOD0 force | `FrontRoomsCameraRig.ShotStarted/ShotEnded(kind, focus)` | `FrontRoomsDoorRig.Nearest(focus, 0.5).ForceLod0(true/false)` for `ShotKind.Unlock` | all platforms |
| **V5 shot light** | `Unlock.ShotLightIntensity` 0.6 × `DoorShot.Weight` | one point light on the camera, range 1.2 m, shadows None, neutral warm white, only while an Unlock shot lives | compiled out on WebGL (32-light cap, `[PLAN V5]`) |

**V6 notes, adopted here** `[VCT:200]`: pose P yawed 12° toward the hinge `[TIM:67]`; `PinCatchAt` 0.6 of the insert depth `[TIM:54]`; a cancelled unlock leaves the door unlocked and shut `[DS:22-27]`. **Superseded by A12:** "the rattle jolts toward the swing side" now means away from the trier (the map turns `swing` first, `[A12MAP]`), and "the pull-side leaf holds still until 1.55 s" no longer happens from a latched leaf (every DA unlock is a push; the pull branch stays only for a leaf out of the frame).

---

## 9. Materials (RD10) and atlases

Three `SurfaceDefs` lines in `FrontRoomsRenderSetup.cs` (keep the hunk minimal; re-base on `outlets_p4b` and `w2_office_props` with `rebase_apply.sh` `[HR]`):

| Material | Definition | Targets / checks | Source |
|---|---|---|---|
| `Door_Enamel` | `texture = "PaintedMetal"` (existing A/N/S: orange peel, scratches to grey primer), `meshUV = true`, `tile = (1, 1)`, `tint` = calibrated, `macroTone .12`, `macroDirt .12`, metallic 0 | Mean albedo **sRGB (205, 197, 176) ± 5 %** = linear (0.611, 0.558, 0.434), **luminance 0.56** (≥ 0.50); mean smoothness **0.50** (0.45–0.55). The build measures `PaintedMetal_A` and sets `tint = sRGB(target_lin / mean_lin)` per channel. Then T6 (6/12/20 m, `FrontRoomsLockedDoorReadability`): locked ≥ +1 stop over free | `[05 §6.2]`, `[SPEC §2.2]` |
| `Prop_KeyTagNo` | new `Textures/Prop_KeyTagNo_A.png` **4096²**, sRGB, mips, aniso 8 (WebGL import tab: 2048 max); `_S` mask: paper smoothness 0.24, ink 0.18; `meshUV`, `tile (1,1)`, `macroTone 0`, `macroDirt .03` | 10 × 10 cells "00"–"99", row 0 at the top (so the sidecar's negative row offset works; the build confirms against the FBX UV bounds). Paper ground = `Prop_Paper`'s tint (0.863, 0.847, 0.800). **Courier Prime Bold `#1E1D1A`**, two digits centred in each cell's insert crop. Digit height **0.24 of a cell**, which meets G12 on every shape: rect 0.78 × 0.308 = 0.240, long 0.82 × 0.29 = 0.238, round 0.43 × 0.56 = 0.241, plate (crop 0.30) 0.80 of its insert. Font `Assets/Fonts/Period1990/CourierPrime/CourierPrime-Bold.ttf`. The key rack's number strips (cells 01–08) use the same atlas | `[G12]`, `[SPEC §4.1]`, sidecar `numberAtlas.crop` |
| `Prop_SignEngraved` | new `Textures/Prop_SignEngraved_A/N/S.png` **2048²**, 2 × 2 cells: 0 EMPLOYEES ONLY, 1 STAFF ONLY, 2 STORAGE, 3 blank | Face dark brown **sRGB (48, 36, 30)**, ivory core letters (proposal **#E9E1C9**, for 平面视觉), **TeX Gyre Heros Bold** (`TeXGyre/texgyreheros-bold.otf`), all caps, cap height **17 mm** on the 0.254 × 0.076 face (the built sidecar's `capHeightMm`; `05` said 19), tracking +40, centred. `_N`: a 0.3 mm engraved recess from the letter mask; `_S`: face 0.55, letters 0.35 | `[05 §6.2]`, `[SPEC §2.4]`, sidecar `signAtlas` |

The atlases are generated by a script in `W/d16_work/atlas/` (outside main; only the PNGs ship under `Resources/Surfaces/Textures`). Type sign-off goes to 平面视觉 · 信息设计 (G12) and 平面视觉 · 印刷与演示 (the sign) before landing (memory rule "type goes through 平面视觉"). MaterialPropertyBlocks break the SRP Batcher only for tags, plates and signs `[SPEC §4.1]`. The materials land before any code spawns a sign or a tag `[RD10]`.

---

## 10. LODs, budgets, WebGL, mobile

- **LOD0/1/2 everywhere** `[CTX]` call 8: the `_DA` kits use `LODS = (LOD1_RATIO, LOD2_RATIO)` (P-1 in main). The 46 lock, key, tag, host, sign and plate kits are re-exported with `LODS` from their sidecar `lodRatios` (module edits only), keeping main's GUIDs and anchors (diff each anchor to 0.1 mm, `[40 §2]`). If the re-export overruns the slot, it is a separate later package, and the doors land with today's LOD1 parts.
- **Per door at LOD0** (OF-K DA): frame 3.4k + leaf 4.4k + lockset 2 × (3.25k + 2.6k + 1.9k + 5.0k) + bolts and strike 2.0k + 2 signs and 2 plates 2.3k ≈ **37k tris, ≈ 19 renderers** (no closer). Beyond 12 m (×2 at desktop lodBias 2): frame + leaf LOD2 + shadow proxy ≈ 0.5k tris, 3 renderers. **T10** (seed 4242 frames 01/04/23/34/35) reports draws and frame time before and after, desktop and at WebGL's lodBias 1. No budget is set until measured `[SPEC §11]`.
- **WebGL:** same kits, no per-door merge in D1.6 (desktop is the reference; the WebGL track decides any reduced tier from T10's numbers). HeldItem off, DOF off, shot light off, all behind `#if UNITY_WEBGL && !UNITY_EDITOR`; nothing else changes on WebGL `[HR]`.
- **Mobile** (`FrontRoomsHandheld.Active`): keys stay walk-over, so no trigger (`[G:644]`); the presenter still shows the key; HeldItem on; V4 without DOF; V5 on.

---

## 11. Open items for others (none blocks the A12 sheet)

- **Q-EX (交互设计 + Red):** C23 puts a crossbar on the EXIT leaf, and A13 locks the EXIT with the floor key. A keyed knob set at Y 1.000 on both faces and a crossbar at Y 1.000 cannot share the latch stile. Default until answered: EX-K = OF-K hardware, no crossbar `[SYS C23, A13]`.
- **Q-RN (交互设计):** a real DA ward door has push plates and no latch. Red's "lever on free doors" gives RN-F the lever set. Default: lever (P2, no map Run doors yet).
- **Ask Red (optional):** name layer 30 "HeldItem" in TagManager.
- **To the HUD workflow:** use `FrontRoomsKeyIdentity` or keep the identical stub hash.

---

## 12. Map-side contract (关卡设计; a unified diff against main's files with their sha1s, applied only in the clone `[HR]`)

| # | File (base sha1) | Change | Why |
|---|---|---|---|
| **M1** | MapWorld 7ef0ca45, BuildEdge door branch after `doorByCollider[door.leaf] = door` `[MW:1497]` | `door.rig = FrontRoomsInteractableKit.DressDoor(this, door, chunk.root.transform);` (null keeps the cube) | **DressDoor returns the rig**, sets `Door.rig` `[VCT:194]`, `[MW:307-308]` |
| M2 | MapWorld | The A12 swing itself is **not ours**: 关卡设计/交互设计 own it (in progress, `[A12MAP]`). The rig needs exactly: `door.swing` turned away from the opener before every open, rattle and key turn from a latched leaf; `FaceOf` geometric (S = root +X); `Game.BeginUnlock` passing `pull = OnSwingSide(...)` | `[A12]` map contract, `[RULE4]` |
| **M3** | MapWorld `SpawnKey` `[MW:3383-3416]` | unscaled empty root `Key · zone {id}`; host via `DressKeyHost` for case A; `DressKey(root, identity, placement, KeyPickupNeedsE)`; `keyByCollider[assembly.Trigger] = zone`; fallback to today's cube on null; no spin for any key | key hosts `[SPEC §4.3]`, `[RES]` |
| M4 | DoorShot b1b230e8 | the unlock's key: `DressShotKey` at `BeginUnlock` (identity of `map.KeyFor(door, feet)`), posed with `PoseInLock` on §5.2's beats, `Sway` from `KeyTurn`, `Release` + destroy at `KeyOutEnd` or a cancel's 0.15 s | `[SEAM:42]` ("lands with the key models, D1.6") |
| M5 | Game 88c93b09 | the C21 first-key hand-off calls `FrontRoomsInspect.OpenHeld(assembly.Root, assembly.TagKit, …)` | `[INSP K]` |
| M6 | MapWorld | `KeyIdentity(GridCoord keyZone, out shape, out tint, out number)` and `DoorKeyIdentity(Door, DoorFace, out …)`: the A13 floor-key rule; the facade's stub until then | `[SYS A13]`, `[DEC #20]` |
| M7 | MapWorld | public `IsUnlocked(Door)` (alias of `IsUnlockedForTools` `[MW:3695]`) | §3.1, `[IXe]` |
| M8 | MapWorld | **locked-door share hook** = `LockedByRule(door)` `[MW:2644]` (C15 + A13), read at dress time; and `DoorMemberFor(door)` when Run/Exit doors exist | `[SYS C15]`, `[CTX]` |
| M9 | Editor Level Designer | the prop palette skips kits tagged `interactable` | `[SPEC §6.5 item 10]` |
| M-A2 | — | **withdrawn by A12** (no pivot offset). The dormant recipe stays documented here only: hinge += `closed·(S_sign·0.0295, 0, 0.0098)`, collider (−S_sign·0.0295, 1.04, 0.4902). Not in the diff | `[A12]`, `[SPEC §1.3]` |
| M-swing | — | **"single-acting swing side" withdrawn by A12**; `FixedSwing` stays only as a never-opened door's rest side | `[A12]`, `[RULE1]` |
| (opt) T | MapWorld | leave out the jamb and head trims under a dressed DA frame, as Red's window T. Not in the first diff; A12 keeps the sleeve | `[SPEC §6.5 item 9]` |

Nothing in the contract adds a collider, a light or a name the hazard rules forbid `[SPEC §6.7]`. The shot timings stay map-owned and unchanged.

---

## 13. Build, capture, verify (order and gates)

1. **Clone:** `W/tools/mkclone.sh proj_d16` (one status line), read `W/TOOLS.md` first, force the fresh-clone script reimport and wait for the READY line, then compile `-batchmode -nographics -quit -buildTarget OSXUniversal` → **0 `error CS`**. One Unity per clone; ASCII editor scripts, no C# tuples; new `.meta` GUIDs checked unused in main `[HR]`. `W/proj_a12` belongs to the map-side A12 work: do not touch it.
2. **A12 FIRST** `[A12]`, `[IXd]`: the 9 `_DA` kits + 6 DA lock parts (Blender, `--out-root proj_d16`), T1-DA/T2-DA, then in the clone `FrontRoomsDoorRig` + `DressDoor` + the clone-only M1 hunk. Then **the A12 record sheet** (a clone-only harness built on `FrontRoomsDoorGapCapture` / `FrontRoomsKitLookdev`):
   - L0-F Lobby veneer and OF-K Office steel;
   - shut, from both faces at **2.5 m** (eye 1.62 m, FOV 76, on the face normal);
   - open **45° each way** (hinge posed by the harness);
   - game lights and post;
   - the gap frames: dark near room and lit far room, both faces, fins on and off.
   - Output: single frames `d16/images/a12_{lobby,office}_{S,P}_shut.jpg`, `a12_{lobby,office}_open45_{toS,toP}.jpg`, `a12_gap_{lobby,office}_{S,P}_{brush,nobrush}.jpg`, and the composed `a12_sheet.jpg`.
   - Write `d16/05_for_interaction.md`: the facts, the T1-DA numbers, and in-engine lit-pixel counts per gap (hinge, latch, head, floor), both faces, with and without the brush. Add the rig's map contract (§12 M2: "agreed unless the rig needs otherwise"), the period options and costs `[RULE3]`, and the kit's missing pieces `[RULE2]`. It is a record for Red, not a decision aid `[A12]`.
3. Materials (§9), KeyAssembly + hosts + M3, HeldItem (§7), ShotLook + light (§8), M4–M8, the LOD re-export (§10).
4. **Gates:** compile 0 errors; Roslyn macOS + `--ios` 0 errors; GUID collisions 0; `FrontRoomsCxSuites.RunBatch` + 20 seeds unchanged except the expected renderer hashes; `FrontRoomsDoorShotTests` with the real rig as well as the fakes; new EditMode tests `Editor/Rendering/FrontRoomsDoorRigTests.cs` (keyhole ±0.0315 at Y 1.000 on both faces; plug, knob and lever signs; bolt travel; `Jolt` direction vs `door.swing`; collider follow; the cube renderer disabled with the collider intact) and `FrontRoomsKeyAssemblyTests.cs` (held parts within 0.1 mm / 0.1° of `[HELD]`; the layer switch at 0.525 m; the trigger on the root); a 120 s `FrontRoomsCxPlayRun` with 0 new exceptions; T3 (collider bounds ±0.1 mm), T5 (head dip both faces), T6 (Lobby/Office readability), T7 (LOD), T8 (key fit), T10 (cost). Timings at a 1-minute load > 32 are marked **UNDER LOAD** `[HR]`.
5. **Hand-offs:** the 9 `_DA` three-views to 平面视觉 · 印刷与演示 through `FrontRoomsThreeView` plus the `threeview/index.json` `assets` block (DA lock parts as variant rows on the existing latch and strike sheets) `[A12]`; atlas proofs to 平面视觉 (§9).
6. **VL specs** to `W/vl_pending/d16.md`, **VL317–VL324 only** `[HR]`:
   - VL317 A12 record sheet;
   - VL318 DA gaps with and without the brush;
   - VL319 DA swing both ways (T2-DA, caps, saddle);
   - VL320 the family in game at 6/12/20 m (T6);
   - VL321 lock beats: head dip with the key in the lock, rattle, lever open;
   - VL322 the key assembly: hung, flat, held with HeldItem, first-key close-up;
   - VL323 the three materials at 0.3 m;
   - VL324 LOD + cost + V4/V5 on/off.
7. **Package:** v2 apply script with bases, re-based on `outlets_p4b` / `w2_office_props` / the landed A12 map work with `rebase_apply.sh`, every base sha1 re-checked; the map contract diff (§12) with sha1s; lands after the Alpha `[A12]`.
