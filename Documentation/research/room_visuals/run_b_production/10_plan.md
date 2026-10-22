# Run! B · Emergency Power: production plan

Date: 2026-10-22, 08:0x. This is stage 1 (plan) of the 游戏视觉 `run_b` production workflow.
- **Nothing was built in this stage.** No clone was made, Unity was not run, and nothing in main was changed. The only writes are this file and the empty `images/` folder next to it.
- **Machine load** while this was written: 1-min load 426. Any timing taken at this load counts as UNDER LOAD (rule: load > 32).

**Decided.**
- Call 1 = **B · Emergency Power**: Red, 2026-10-21, "全部按推荐" (`../40_for_red.md`, top line).
- On 2026-10-22 Red asked 游戏视觉 to start every planned step now.
- The Run! floor is glossy waxed 12-inch VCT: Red, 2026-10-03.

**Not decided.** Calls 2–8 of `40_for_red.md` stay open (§8). This plan builds B so that none of them has to be answered first.

**Sources read.**
- `../10_run_directions.md` §1, §3, §7, §8.
- `../11_run_dir_emergency_power.md`: every value used in r8, and D1–D14.
- `../20_run_emergency_power.md`: build log, costs, needs, open issues, and the probe findings §1.2.
- `../21_run_fix_1014.md`: box probes reach the floor (VL236); the Relay at 2.05 m reads 7.68 : 1 (VL237).
- `../40_for_red.md`.
- `../03_run_research.md` §0–§7.
- Figma ROOM VISUALS `2862:6157`, read only:
  - RV27 `2865:6310`, with image slots s1b, s1a, s2, s3, c4k;
  - RV15 `2864:6157` (title Run).
- The design clone `W/proj_run_emergency_power`, read only. Unity was not run on it.
  - It holds 98 non-`.meta` files that are not in main (the brief counts 92).
  - The B design set among them: 4 Blender modules, 4 kit FBX with 4 sidecars, 29 textures and cookies, 4 slot materials, the post profile, 2 shaders, the rig, the harness, 6 probe EXRs and `gen_run_b.py`.
  - The rest are look-dev and audit tools and stale Office models.
- The look-dev feature tool `W/tools/feature/run_lookdev/emergency_power`, and `W/TOOLS.md` §2, §5 and §8.
- Code in main:
  - `FrontRoomsRoomStream.cs` (sha1 4e6e8e7f);
  - `FrontRoomsExitSign.cs`, with fix1a/fix1b and `LockLit`;
  - `FrontRoomsHunter.cs` (`TryAlarm`, `Alarmed`);
  - `FrontRoomsSurface.shader`;
  - `FrontRoomsRenderSetup.cs`;
  - `FrontRooms_URP.asset`;
  - `kitlib.py` (`LODS`);
  - the `Kit_ExitSign*` and `Kit_Conduit*` sidecars.
- The packages in `W/apply`, and `W/vl_pending`.

`W` = `/Users/redwang/FrontRoomsVisualWork`. `M` = Red's `Frontrooms3D`.

---

## 0. Short answer

1. **Two layers.** B is built as two reusable layers that know nothing about the map:
   - **`FrontRoomsRunRig`** carries the lights and the states: armed wraps, the mains drop, the twin-heads, the EXIT signs, the corridor ambient, the per-state box probes and `FrontRoomsPost_Run_B`.
   - **`FrontRoomsRunDress`** carries the finishes and props: two-tone block, glossy VCT, soffit, sprinkler main, tray, EMT, fire gear, plates and obstacles.
   - A `RunRigLayout` describes the room. It has two presets: the 27 m corridor (`Corridor1x9`, the r8 geometry) and today's stream Run room (`StreamRoom`, 11.5 × 12 × 2.9 m).
2. **It plugs into today's RoomStream Run room behind one switch,** `RunLook { Ward, EmergencyPower }`.
   - The default is **Ward**, today's Level ! look. Its frames must stay identical.
   - Flipping the default is call 6, which stays with Red.
   - The map module (CR-1 to CR-6) can later call the same rig with the corridor preset.
3. **New assets:**
   - `Kit_EmergencyTwinHead` (battery cabinet) plus `_Yoke` ×2 and `_Lamp` ×2 per unit. These are the r8 models, rebuilt on main's current kitlib with LOD0/1/2.
   - A hero-quality `Kit_WrapFixture_4ft`. The r8 version was a 200-triangle blockout.
   - Two sealed-beam cookies.
   - Thirteen secondary B kits (fire gear, overhead services, rail, plates, three obstacles).
   - A layout-independent waxed VCT set.
   - The per-layout box-probe cubes.
4. **What the package changes in main's existing files:**
   - `FrontRoomsSurface.shader`: two probe keywords;
   - `FrontRoomsRoomStream.cs`: the switch and the host-lamp hook;
   - `FrontRoomsRenderSetup.cs`: one separate two-line hunk.
   - It changes no EXIT-sign file.
5. **One packaging question goes to the orchestrator** (not a design call): the URP asset's two probe flags are in `Assets/Settings`, outside the ship list (§7.3).
6. **Gates:**
   - Relay at 2.05 m ≥ 4 : 1, protan included (r8: 7.68);
   - photosafety ≤ 3 changes/s, with Reduce Flashing honoured, for the trip, the reset, the warning, the stutter and a sprint pass;
   - frame cost against Ward at the same pose;
   - the r8 proposal frames matched within tolerance;
   - Ward and every non-Run frame unchanged.
7. **Verification rows:** VL338–VL341 (§11).

---

## 1. Production list

### 1.1 Hero kit: the twin-head unit and the wraparound (Blender, kit pipeline, built in the clone)

| Kit | Source | What it must show at 0.3 m (hero LOD0) | Budget LOD0 / LOD1 / LOD2 (tris) | lodDistances d01, d12, dcull (m, lodBias 1, vFOV 76) |
|---|---|---|---|---|
| `Kit_EmergencyTwinHead` (battery box) | design clone `run_twinhead.py` (3,706 tris, LOD0 only) | almond steel cabinet 338 × 191 × 114 mm; lanced side louvres; bottom slots; red AC ON neon pilot; PUSH TO TEST button; legend plate; conduit feed knockout; two yoke bosses (anchors `mount_l`, `mount_r`, `feed`) | 3,800 / 1,500 / 450 | 3, 10, never (it is the readable object and the light source; S2 showed 285 px of twin-heads) |
| `Kit_EmergencyTwinHead_Yoke` ×2 | `run_twinhead_yoke.py` (916) | U-yoke, swivel boss, tilt knob (anchor `trunnion`) | 950 / 380 / 60 | 2.5, 8, 30 |
| `Kit_EmergencyTwinHead_Lamp` ×2 (sealed-beam head) | `run_twinhead_lamp.py` (2,480) | PAR-36-style head shell, chrome bezel, fluted sealed-beam lens (`Run_SealedBeamLens`, emission for the lit filament; anchor `lens` = light origin) | 2,500 / 1,000 / 150 | 2.5, 8, never |
| **One unit** (1 box + 2 yokes + 2 heads) | r8: 10,498 | the rig aims each head like an installer: the yoke swivels on its boss, the head tilts in the yoke | **≤ 10,700 / 4,260 / 870** | — |
| `Kit_WrapFixture_4ft` | `run_wrap_4ft.py` (200 tris, a blockout) → **rebuilt** | 1.22 × 0.25 × 0.08 m; steel channel; end caps on the wrap profile; prismatic acrylic wrap (curved bottom and sides; prisms in the normal map); two lamp bands seen through the lens (`Run_WrapLens` E); screws; anchor `light` | 2,400 / 960 / 160 | 4, 12, never |

**What hero means here (checked in VL338).**
- **Curves:** chord error ≤ 0.2 mm on every round part. Example: the 110 mm lens rim needs ≥ 48 segments.
- **Edges:** every box edge bevelled ≥ 0.8 mm, 2 segments. No visible normal seams.
- **Decals:** ≥ 2.5 px/mm. At 0.3 m and vFOV 76, one screen pixel covers 0.43 mm, so the legend plate needs an atlas of at least 512².
- **Build:** each module uses main's kitlib `LODS = (0.4, 0.12)`. Parts that vanish at a switch are marked with `fr_lod1_drop` / `fr_lod2_drop`.
- **No colliders** (`no_collider`). The units sit 2.45 m up.

**Sealed-beam cookies.**
- `Cookie_SealedBeam_S`, 256 px, one channel: the 50° / 20° beam with about 2 % stray light out to 45°, inside a 100° spot (D3). It shows a hot core, one faint ring and lens fluting.
- `Cookie_SealedBeamCore_S`, 256 px: the goal head's narrow beam (26° / 22°).
- Both come from the r8 generator. They are desktop only.

### 1.2 Secondary B props (new Blender modules, the same 0.3 m rule; no B prop is left as a blockout)

| Kit | Spec (from `11` §2.5 and `10` §3.4) | LOD0 / LOD1 / LOD2 | d01, d12, dcull | Collider |
|---|---|---|---|---|
| `Kit_PullStation` | 130 × 180 × 60 mm, red #B01E18, T-handle #C42A20, "FIRE / PULL DOWN" face | 2,400 / 900 / 150 | 2, 6, 25 | no |
| `Kit_FireBell` | 6 in gong #A8231C, black striker, base | 1,800 / 700 / 120 | 2, 6, 25 | no |
| `Kit_ExtinguisherCabinet` | semi-recessed 0.24 × 0.61, 70 mm proud, white #E4E2DA, clear glass door, vertical red legend, extinguisher with gauge and hose | 5,500 / 2,200 / 400 | 2.5, 8, 30 | no |
| `Kit_SprinklerHead_Upright` | red tee boss, brass frame, red bulb, deflector | 700 / 220 / 0 | 1.5, 5, 15 | no |
| `Kit_SprinklerMain_3m` + `Kit_PipeHanger_Band` | Ø 0.10 #A3201A (smoothness .48), grooved coupling; band hanger on threaded rod | 500 / 200 / 40 + 300 / 100 / 0 | 3, 9, never / 2, 6, 20 | no |
| `Kit_CableTray_3m` | galvanized ladder tray 0.30 × 0.08, rungs every 0.23 m, cable bundle | 2,600 / 900 / 120 | 3, 9, 30 | no |
| `Kit_BumperRail_1200` | charcoal vinyl #3A3A38, 0.15 tall, 2 standoffs | 400 / 140 / 24 | 3, 9, never | no |
| `Kit_CornerGuard_1200` | stainless jamb guard | 120 / 40 / 12 | 2, 6, 25 | no |
| `Kit_SafetyPlate_10x7` | 254 × 178 mm plate, 4 screw heads; faces NOTICE, ELECTRICAL ROOM, STAFF ONLY; plus a NOT AN EXIT stencil decal quad | 300 / 100 / 12 | 2, 6, 25 | no |
| `Kit_ChairDolly8` | low dolly with 8 folded grey steel chairs #5F615E | 12,000 / 4,800 / 900 | 4, 12, never | box |
| `Kit_JanitorCart` | structural-foam cart #ABA9A0 (r8, D12), dark vinyl bag, yellow bucket and wringer #D9AE14, mop, spray bottles | 9,000 / 3,600 / 700 | 4, 12, never | box |
| `Kit_HandTruck_Cartons` | black frame, 2 wheels, 3 white corrugated cartons #D6D3C8 with tape, leaning 16° | 4,500 / 1,800 / 350 | 4, 12, never | box |

**Reused from main, no new model:**
- `Kit_ExitSign_End`: the flag-mounted double-face sign (5,758 / 1,822). This is B's flag sign.
- `Kit_ExitSign`: the wall sign over the goal.
- `Kit_ConduitEMT` and `Kit_ConduitStrap`: B's three 3/4 in conduits.
- `Kit_OutletHandyBox`: the junction box every 3 m.
- `Kit_DoorFrame_Steel` + `Kit_DoorLeaf_Steel` (almond `Prop_SteelAlmond`) + `Kit_DoorCloser_Body`: the locked RN-K side doors, as non-opening props.
- The `Prop_*` slots.

**Not in B production** (owned elsewhere, or a call):
- the goal door RN-X / `Kit_DoorLeaf_Ward_Lite`: interactables, plus calls 2 and 7;
- `Glass_Wired`: the glass track;
- the Relay model: the Relay rig track; Hunter B Night Shift is VL342–347.

### 1.3 RunB textures and materials

| Set | Mapping | Size and compression | Notes |
|---|---|---|---|
| `Run_Block` A/N/S | world-projected, 0.4 × 0.4 m (divides 192 m and the 1.2 m paint line) | 1024², BC7 / BC5 / BC7 | the r8 set. Tones are material tints: `RunB_BlockLower` #8E958B (0–1.2 m), `RunB_BlockUpper` #DAD7CB, semi-gloss |
| **`RunB_VCT` A/N/S** (new) | **world-projected 1.2 × 1.2 m = 4 × 4 tiles of 12 in (0.30 m metric, as main's `Run_VCT` and `FourFoot`)** | 2048² (0.59 mm/px), BC7 / BC5 / BC7 | Field #B9B4A6, per-tile tone, through-chip flecks laid in quarter turns, seams. **S is Red's gloss: field ~.80, seams ~.38.** It replaces r8's `Run_ServiceVCT`, which is corridor-local (mesh UVs across exactly 3.0 m) and cannot fit the 11.5 m stream room |
| Floor layout bands | the same `RunB_VCT` textures on adjacent floor slabs (no overlay, no z-fight, no shader change) | — | `RunB_Floor` (field); `RunB_FloorAccent` (tint #7E7A70, two 0.30 m rows); `RunB_FloorLane` (burnished, smoothness ×1.06 → ~.85, 1.2 m wide); `RunB_FloorWax` (0.15 m perimeter band, yellowed, ~.90). Positions come from the layout, so the corridor and the stream room share one texture set |
| `Run_SoffitPaint` A/N/S | world-projected 2.4 m (form seams 1.2 × 2.4, tie holes on 0.6 m) | 1024² | r8, #45463F |
| `Run_SealedBeamLens` A/N/S/E, `Run_WrapLens` A/N/E, `Run_UnitLegend` A, `Run_NeonPilot` | kit slots, mesh UVs | 512² (wrap 1024 × 512), BC7 | the wrap atlas is rebuilt for the hero wrap's UVs. The legend is ≥ 2.5 px/mm |
| Plates: `Run_Plate{Notice,Electrical,Staff,Pull,Extinguisher}_A`, `Run_StencilNotExit_A` | kit slots | 640 × 448 at 2.5 px/mm, **BC7 with mips (r8 used RGBA32)** | Faces in TeX Gyre Heros Bold / Bold Condensed (`Assets/Fonts/Period1990/TeXGyre`). Faces and the unit legend **go through 平面视觉** before shipping |
| Cookies | light cookies | 256², single channel | §1.1 |
| Slot materials | — | — | `RunB_SprinklerRed` (#A3201A, .48), `RunB_FireRed` (#B01E18), `RunB_BumperVinyl` (#3A3A38), `RunB_Cove` (#34332F), `RunB_CartPlastic` (#ABA9A0), `RunB_BucketYellow` (#D9AE14), `RunB_Carton` (#D6D3C8), `RunB_GalvSteel`. They ship as `.mat` assets in `Resources/Surfaces`, not as RenderSetup SurfaceDefs, so the RenderSetup hunk stays two lines |

- **Dropped from r8:**
  - `Run_ExitSignB` A/E: the merged EXIT family's faces are used.
  - `Run_WireGrid_A`: the goal lite belongs to call 7 and the glass track.
  - `Run_ServiceVCT*`: replaced by `RunB_VCT`.
  - The matte S mask: Red's "keep the matte versions" rule is about frames. Matte frames come from a clone-only harness switch.
- **Budget:** all new B textures ≤ **40 MB** at editor runtime size (r8: 81 MB), BC-compressed with mips. The WebGL import tab caps them at 1024. Mac/Win tabs are untouched.
- **Generator:** `gen_run_b.py` → `W/tools/feature/run_b/gen_run_b_prod.py` (`Tools/lookdev` is not in this ship list). A `.txt` copy goes to `run_b_production/tools/` for provenance.

### 1.4 Lights, states, shaders, post: production values

The r8 values (`11` §2.3–2.4) are kept unless a row says otherwise.

| Item | Value |
|---|---|
| Armed wraps | spot 172° / 124° (D2), (0.90, 0.95, 1.00), 5.5, range 10; 1 in 3 soft-shadowed. Stutter tube in cell 5 at 0.78, using the stream's Unstable model (≤ 3 changes/s, steady under Reduce Flashing). `RunSoffitHalo` scallop over each wrap (armed only) |
| Trip t 0 (mains drop) | every wrap 1 → 0 in 0.06 s; lens glow 0.03, τ 0.5 s; the AC ON pilots go out |
| Heads rise | from t = 0.35 + 0.004 z, 0 → 1 over 0.18 s, ease-in (independent transfer relays) |
| Flood heads | 100° / 96° spot + `Cookie_SealedBeam_S`, (1.00, 0.84, 0.64), 10, range 11, soft shadows .95, pitch −30°, aimed across the axis |
| Goal head | 26° / 22° + `Cookie_SealedBeamCore_S`, gain 5, range 13, pitch −13°. Exposed as `goalHeadGain` (call 4) |
| Beam cones | `RunBeamCone` additive frustum, 2.5 m, density .05, gain 16. **Both shaders move to `Resources/Rendering/`**, because `Resources/Lighting` is outside the ship list |
| EXIT | The merged family. The sign driver's own red light is driven by `Level`. On the flag sign, the rig adds one slave face spot on the second face: (1, .13, .07), 0.30, range 2.4, 170° / 120°, set from `sign.Level` (D8) |
| EXIT on the trip | **Steady**: they are on the emergency circuit, so the rig never calls `SetMains` on them. In B they are dealt Lit or Half only, never Loose (rule 3: red stays steady). The goal wall sign keeps the family rule: no light of its own (EXIT spec §8.6), EmissionGain 1 (call 7) |
| Corridor ambient | Spherical-harmonics (SH) lighting set per renderer through MaterialPropertyBlocks (`CustomProvided`), set only when the state changes. It uses the D9 bounce model. **The four look-dev floor-bounce spots are not shipped.** A height-banded SH set gives renderers above 2.4 m (soffit, main, tray, EMT) the "lit from the floor" up term instead. Tripped lights in the corridor go 15 → 10 |
| Reset (never rendered so far) | heads out in one change; wraps re-strike in a sweep from the entrance, 0.15 s per cell, ≤ 2 flicks each; pilots relight. With Reduce Flashing: 0.5 s rises, no flicks |
| Reduce Flashing trip | no black gap: wraps fade 1 → 0 over 0.5 s from t 0, heads rise 0 → 1 over 0.5 s from t 0.25 |
| Stage-1 warning (armed) | `ApplyWarn(multiplier)` on the rig's own lamps (CR-4). The map drives it; the stream has no warning |
| Post `FrontRoomsPost_Run_B` | lift (1, 1, 1, 0) (D1), contrast +12, saturation −10, temperature −6, post-exposure −0.1, bloom 1.0 / 0.6, vignette .32. Added with `EnsureZoneVolume(room, "Run_B", bounds, 1.0)` and raised to priority 2. Shipped in `Resources/Rendering` with a new GUID |
| Culling | the rig culls its own lights beyond 16 m on desktop, 10 m in the WebGL tier |
| Variant prewarm | at room build the rig warms the `_LIGHT_COOKIES` and `RunBeamCone` variants (a ShaderVariantCollection). Otherwise the first tripped frame compiles shaders exactly when the player must run |

### 1.5 Per-room box probes (VL195 / VL236 rules)

- **One probe set per state.**
  - The rig enables the set that matches the state. It never swaps a probe's texture in place.
  - Cubes are Custom, 256 px HDR. They use the same format and folder as the zone cubes: `Resources/Rendering/Reflections/RunB_<layout>_<armed|tripped>_<i>`.
- **Boxes:**
  - each box overhangs the shell by **0.16 m** on x and y, and at the ends;
  - blend 0.15, box projection on;
  - importance above the zone default reflection.
  - The overhang matters because a box placed exactly on the shell weighs 0 on its own faces.
- **Corridor:** 3 per state, boxes 3.16 × 3.22 × 9.6 m (r8).
- **Stream room:** 1 per state, box 11.82 × 3.22 × 12.32 m, or 2 halves of 6.3 m.
  - For a box-shaped room a single box is geometrically exact.
  - The choice is made by the VL340 coverage and pixel test.
- **Baking:** offline in the clone, one Unity run per state (D11). A batch run freezes the Forward+ probe atlas after the first render. Armed bakes at t −1, tripped at t +3.
- **Shader:** `FrontRoomsSurface.shader` gains `#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING` and `#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION`.
- **URP asset:** needs `m_ReflectionProbeBoxProjection` 1, and `m_ReflectionProbeBlending` 1 unless test P0 shows that Forward+ blends without it (§7.3).
- **Cost:** about 1 MB per cube. Stream: 2–4 MB; corridor: 6 MB.

### 1.6 Code

| File | Status | What |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` | new | Replaces the clone-only `FrontRoomsRunLookdevRig`. Holds `RunRigLayout` (plain data class with presets `Corridor1x9()` and `StreamRoom()`) and the host modes `HostLamps` (the stream drives the armed lamps; the rig supplies `MainsLevel`) and `OwnLamps` (the rig builds the wraps; for the map, CR-4). API: `Build`, `Arm`, `Trip(time)`, `Reset`, `ResetImmediate`, deterministic `Evaluate(t)` (used by look-dev and the tests), `Tick(dt)` (no allocation), `ApplyWarn`. C# events for the sound chat (CR-S3), with no audio calls (AUDIO_CONTRACT): `StateChanged(state, pos)`, `MainsDropped(pos)`, `TransferRelay(unit, pos)`, `ResetStarted(pos)`. The WebGL tier sits inside `#if (UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW`, the same gate as `FrontRoomsPlacardMount` |
| `Assets/Scripts/Rendering/FrontRoomsRunDress.cs` | new | Finishes and props from the layout: wall tone slabs, floor bands, soffit, main, heads and hangers, tray, EMT and boxes, bumper rail, cove, fire gear, plates, RN-K props, obstacles |
| `Assets/Resources/Rendering/RunBeamCone.shader`, `RunSoffitHalo.shader`, `FrontRoomsPost_Run_B.asset` | new (moved) | §1.4 |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | +2 lines | probe keywords (§1.5) |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | hunk | §2 |
| `Assets/Scripts/Rendering/FrontRoomsSurfaces.cs` | only if needed | B material lookups |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | **separate 2-line hunk** | `Set(so, "m_ReflectionProbeBoxProjection", 1)` (and Blending) in `EnsurePipeline` |
| `Assets/Editor/Rendering/FrontRoomsRunRigTests.cs` | new test | T2 (§9) |
| `Tools/Blender/frontrooms_kit/assets/run_*.py` | new | 17 modules: 4 hero (§1.1) + 13 secondary (§1.2) |
| `Assets/Resources/Props/Models/Kit_*`, `Resources/Surfaces/*.mat` + `Textures/*`, `Resources/Rendering/Reflections/RunB_*` | new | assets, each `.meta` with a GUID checked unused in main |
| Clone-only, in `W/tools/feature/run_b` | tools | The harness drives the **production** rig (`-runRig prod -runLayout corridor\|stream`, plus the r8 switches). Also the texture generator, the measurement scripts (the `measure_b_r8.py` successor) and the probe-bake runner. `.txt` copies go to `run_b_production/tools/` |

Every editor script in the clone is ASCII only with no C# tuples. The new runtime files follow the same rule by choice.

---

## 2. How B goes onto today's RoomStream Run rooms

### 2.1 What the Run room is today (main 0ea38bb7)

- **Place in the cycle:** profile 2 of the furnished cycle (Shift, Office, **Run**, Exit, Lobby).
- **Shape:** an 11.5 × 12 × 2.9 m box. The far wall has a double door: 2.4 m opening, leaves 2 × 1.12 m.
- **Lamps:** 12 troffers on the 0.6 × 1.2 grid (columns x 0 and ±3.6; rows z 1.7 / 4.55 / 7.4 / 10.25, snapped by `AlignFixtures`). The centre column of 4 is soft-shadowed.
  - Run tubes: (.88, .92, .94), 0.6, range 6.5.
  - 70 % are dead and 45 % of the rest fail.
- **EXIT signs:** two `Kit_ExitSign_Hanging` at z 3.4 and 9.2.
  - Each has a red spot 162° / 140°, 3.6, range 9.5, soft (fix1), EmissionGain 1.
  - Odds: Half .15, Loose .15.
- **Props:** handrails, teal chairs, a gurney, an IV pole.
- **It is never reached in play** (`lobbyOnlyTitle`). Only two things show it:
  - the stream editor test, where the old stream brain alarms on entry once the door behind has shut (`FrontRoomsHunterBrain.TryAlarm` → `Alarmed`);
  - the edit-mode profile preview (`BuildEditorPreview`, `FrontRoomsLookdevCapture`).

### 2.2 The switch

- **Declaration:** `public enum RunLook : byte { Ward, EmergencyPower }`, with the serialized field `runLook = RunLook.Ward` and a static override for look-dev and tests.
- **Ward** is today's code path. Its frames must stay identical (gate T3).
- **EmergencyPower** builds a B variant inside `Profile props / Run`.
  - It is built at pool init in every slot, like the other profile variants. Nothing pops or re-dresses in view.
  - It is toggled by `SetActive` with the rule.
- **Flipping the default is call 6.**

### 2.3 Lamp plan in the stream room (`StreamRoom` preset; values marked SP are starting points, tuned against the gates)

| | Armed | Trip / tripped |
|---|---|---|
| Ceiling lamps (host mode) | **The stream keeps its 12 lamps and its LampState machine** (door-cue reveal, AlignFixtures, rebase). For Run+B: the troffer pan, diffuser and beam hide; a `Kit_WrapFixture_4ft` parented to each fixture shows (toggled in `RefreshRoomMaterials` like the lens swap). Light 172° / 124°, (0.90, 0.95, 1.00), 5.5, range 10. Odds: dead 0, unstable 0, plus fixture 5 as the one stutter tube. Shadows on the centre column (4) | The stream multiplies its Run room's lamp output and lens emission by `rig.MainsLevel` (1 → 0 at t 0 over 0.06 s). This is the only new hook in the stream's lamp code |
| Twin-heads | 2 units: **U1 on the right wall at z 3.0 and U2 on the left wall at z 9.0, box centre 2.45** (the corridor's first two units). Pilots lit | Heads at 0.362 / 0.386 s. U1's −Z head lights the threshold lane (z ≈ 0.5). U1's +Z head and U2's −Z head meet on the lane at z ≈ 6. U2's +Z head is the goal head, aimed at the door pair. Aim points on the lane at x ±0.6 (the stream's clear lane is x ±1.4). Pitch about −22 to −30° for the 5.6 m throw (SP). **If any reachable 0.5 m floor cell falls under pool max / 40** (rule 6: no pockets), add U3 (left wall, z 3) and U4 (right wall, z 9) aimed across the room |
| EXIT | Flag `Kit_ExitSign_End` on the right wall at z 6.0 (bottom 2.32), faces ±Z. Wall `Kit_ExitSign` over the far door (`BuildDoorExitSign`, no light). No hanging signs in B | unchanged |
| Lights (stream, desktop) | 12 wraps + 2 EXIT = **14** (4 shadowed) | 4 heads + 2 EXIT = **6** (4 shadowed); 10 / 8 in the 4-unit variant |

### 2.4 Ceiling, finishes, dressing (B variant)

- **Ceiling:** the Run ceiling material becomes `Run_SoffitPaint` (no tiles).
- **Walls:**
  - the wall material becomes `RunB_BlockUpper`;
  - a lower-tone slab, 0–1.2 m and 2 mm proud, goes on every wall face that looks into the Run room, door-wall faces included;
  - bumper rail at 0.9 m on both long walls, with returns at the doors; cove base.
- **Floor:** `RunB` bands from the layout:
  - field;
  - accent rows on the lane edges, x ±1.5 (SP);
  - lane x ±0.6;
  - wax perimeter.
- **Overhead:**
  - sprinkler main along the axis at x +0.55, 2.62 m, wall to wall; heads every 3 m; hangers;
  - tray at x −0.60, 2.70 m;
  - EMT on the left wall at 2.71–2.81 m, straps every 1.5 m, handy box every 3 m.
- **Fire gear:**
  - pull station at z 1.0 right, 1.2 m;
  - bell above it at 2.3 m;
  - extinguisher cabinet on the left wall at z 6.0, 1.1 m.
- **Side doors:** two non-opening RN-K props: z 4.5 left "STAFF ONLY", z 10.0 right "NOT AN EXIT" stencil.
- **NOTICE plate:** on the Run side of the entry wall, beside the opening at 1.5 m.
- **Obstacles, all in pools (SP):**
  - chair dolly lane-right at z 6.0;
  - janitor cart lane-left at z 2.5, moved 0.5 m off the wall into the pool centre. This is the fix for r8's 1.16 : 1 cart.
  - Each must leave a lane ≥ 1.4 m and read ≥ 2 : 1.
  - The hand truck goes only into the corridor preset, because 12 m holds two obstacles.
- **Outlets:** the stream's outlet planner stays as it is (`StreamClass(Run)`).
- **Post:** `Run_B` volume as a child of the variant (11.5 × 2.9 × 12, blend 1.0, priority 2).

### 2.5 Trip, reset, recycle, rebase

- **Trip:** the rig trips once per room instance. Two conditions: the stream camera is ≥ 1.5 m past the Run room's entry plane, and the door behind is shut. This mirrors `TryAlarm`, so the trip can never be seen from the previous room.
  - The stream raises `RunTripped(sequence)`.
  - The title never trips: it has no Run rooms.
- **Recycle:** a slot is recycled only while sealed, so `ResetImmediate()` runs out of view (armed state, armed probe set).
- **Rebase:** a rebase moves the root. Lights, probes and the post volume are children, so they move with it. World-projected tiles divide 192 m.
- **Edit-mode preview:** shows B armed only when the switch is on.

### 2.6 Reuse for the map (nothing in map files is touched or decided)

- **The map module uses** `RunRigLayout.Corridor1x9()` with `OwnLamps` (module cells `ModuleLamp.Off`, CR-4), plus `FrontRoomsRunDress`.
- **The map would call:**
  - `Build` on `ChunkBuilt`;
  - `Trip` on `RoomTriggered(Alarm)`;
  - `Reset` on `RoomReset`;
  - `ApplyWarn` from its warning lamps;
  - `Dispose` on `ChunkDropped`.
- **The goal comes from the layout as a point and a normal.** The rig builds no door, so call 2 stays open.
- **CR-1 to CR-6 and CR-S1 to CR-S4** are already written in `10` §8. This plan sends nothing.

---

## 3. Budgets

| | Corridor 1×9, desktop | Stream room, desktop | WebGL tier |
|---|---|---|---|
| Lights armed / tripped / blackout | 11 / 10 / 2 (r8: 12 / 15 / 3 with look-dev extras) | 14 / 6 (10 with U3–U4) / 2 | corridor 10 / 5; stream 13 / 3–5. Cap 31 with the anteroom |
| Shadowed armed / tripped | 3 / 8 | 4 / 4 (8) | 1 / 1 (nearest head) |
| Rendered tris at the S1b pose (LODs active) | ≤ 140k | ≤ today's Ward room + 60k | LOD as desktop |
| Draws / SetPass at S1b (SP) | ≤ 160 / ≤ 60 | ≤ Ward + 60 / + 25 | ≤ Ward + 30 draws |
| GPU frame at 1920 × 1080 (SP) | reported against Ward | ≤ Ward + 1.5 ms at load ≤ 32 | timed in real Chrome by the WebGL track |
| New textures | ≤ 40 MB (shared) | shared | max 1024 |
| Probe cubes | 6 MB | 2–4 MB | none |
| First second after the trip | max frame ≤ 1.5 × median | same | same |

Per-LOD triangle budgets are in §1.1 and §1.2.

---

## 4. WebGL tier

The WebGL tier is gated only by `#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW` and by the WebGL import tab. Mac/Win rendering and logic do not change.

- **Lights:**
  - tripped: one wide spot per unit (80°, no cookie, intensity set so that pool centres stay within ±20 % of desktop);
  - no beam cones, no soffit halos;
  - only the nearest head casts shadows;
  - armed wraps are unshadowed except one;
  - culling at 10 m, cap 31.
- **Reflections:** no box probes. One flat custom cube per state (the r1–r2 matte path); the floor keeps its gloss mask.
- **Ambient:** SH property blocks only on the ≤ 8 shell renderers, so the props stay SRP-batched.
- **Textures:** max 1024 on the WebGL tab.
- **Shader keywords:** the two probe keywords are stripped from WebGL builds with an `IPreprocessShaders` stripper, so WebGL frames do not change. That file lives in `Editor/Rendering` but is not a test, so its path needs the orchestrator's OK (§7.3). Without it, WebGL carries the extra variants, and URP turns the box-projection keyword on globally.
- **Checked in the clone** with the preview define: look, Relay ≥ 4 : 1, photosafety, no probe objects.
- **Timing** belongs to the WebGL track, in real Chrome, never the Claude pane.

---

## 5. Order of work (stages after this one)

1. **Clone.**
   - Make it with `W/tools/mkclone.sh proj_runb --feature run_lookdev/emergency_power`.
   - Compile with `-batchmode -nographics -quit -buildTarget OSXUniversal`; expect 0 `error CS`.
   - Force a reimport of `Assets/Scripts` (TOOLS.md known issue); the first Play run must print the READY line.
   - Record main's HEAD and base sha1s (§7.2). Copy the B design files from `W/proj_run_emergency_power` (read only).
   - Batch look-dev runs with `-frGlassRT off` if the RT layout self-test stalls. The design clone's "RT feature off" renderer change is not shipped.
   - One Unity process at a time on `proj_runb`.
2. **Kit.**
   - Write the 17 Blender modules: `Blender -b --factory-startup --python build_asset.py`, with main's kitlib and `LODS`.
   - Import with `FrontRoomsKitImporter`.
   - Check with `FrontRoomsThreeView`, `FrontRoomsCxVisLodDump` and 0.3 m close-ups.
3. **Textures and materials.** Run `gen_run_b_prod.py` (W venv python). Plate faces and the unit legend go to 平面视觉 for review.
4. **Code.** The rig, the dress builder, the shaders and post, the Surface shader hunk, the RoomStream hunk, the tests, and the separate RenderSetup hunk (clone only). Then the Roslyn check.
5. **Probes.** Bake per layout and per state, one Unity run per state.
6. **Renders and measurements** (§9), images to `run_b_production/images/`. Every frame is read by eye before the next change.
7. **Critic and fix round.**
8. **Packages** (§7), then the VL specs (§11).

---

## 6. Rules this production keeps

- **Main is read-only**, except `Documentation/research/room_visuals/run_b_production/`.
  - Never run Unity on main.
  - Never run `FrontRoomsRenderSetup.RunBatch` outside `proj_runb`.
  - No git writes. Never delete: anything to remove is moved to `W/_partial`.
- **One clone, `W/proj_runb`, with one Unity process at a time.** `W/proj_run_emergency_power` is a read-only reference; Unity is never run on it.
- **No downloads.** Python is `W/venv/bin/python` without `-I`.
- **Images** go to `run_b_production/images/` (JPG q85, 1920 px wide).
- **Clone editor scripts** are ASCII only, with no C# tuples. Every new `.meta` GUID is checked unused in main.
- **Highest spec:** the desktop is the reference. WebGL changes sit only behind the WebGL gate and the WebGL import tab.
- **Fair darkness:**
  - darkness is never cover: dark bands are full width, and every obstacle sits in a pool;
  - red stays steady;
  - every light change ≤ 3 per second, and Reduce Flashing is honoured.

## 7. Packaging

### 7.1 Ship list and packages (apply convention v2)

- **`W/apply/run_b.sh`:** every new file in §1, plus the Surface shader hunk, the RoomStream hunk, the `FrontRoomsSurfaces.cs` hunk if any, and the tests.
  - Paths: `Assets/Scripts/FrontRoomsRoomStream.cs`, `Assets/Scripts/Rendering/**`, `Assets/Resources/{Surfaces,Rendering,Props}/**`, `Tools/Blender/frontrooms_kit/**`, `Assets/Editor/Rendering/**` (tests).
- **`W/apply/run_b_rendersetup.sh`:** the two-line RenderSetup hunk, alone.
  - Main's RenderSetup (256cf3a4) is also the base of the unapplied `outlets_p4b_d`; `outlets_p6` is stacked and held; `d16-doors-locks-keys` is relaunched.
  - So at package time it is rebased with `W/tools/rebase_apply.sh run_b_rendersetup` onto whatever has landed, and every base sha1 is checked again.
- **No EXIT-sign file is changed:** `FrontRoomsExitSign.cs` 4848e486, the `Kit_ExitSign*` sidecars and Blender modules.
  - B only calls `Configure`, reads `Level`, and leaves `LockLit` at its default.
  - So the in-flight EXIT work (chevrons VL333–336, LIT lock) cannot conflict.
- **No git writes, no deletions** (anything moved goes to `W/_partial`). Main stays read-only except `run_b_production/`.

### 7.2 Base sha1 (main 0ea38bb7 + working tree, 2026-10-22 07:56; checked again at package time)

| File | sha1 | Touched? |
|---|---|---|
| `Assets/Scripts/FrontRoomsRoomStream.cs` | 4e6e8e7f9a19ea98bc9afb74cdc61373c61fe353 | hunk |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | a82fe1872b133c2a92e96201cb44828323885cff | +2 lines |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | 256cf3a449266de795ba4675ec660e9f28af35a6 | separate hunk |
| `Assets/Scripts/Rendering/FrontRoomsSurfaces.cs` | ab27937134799fdddbd5567db672e7c637be442b | maybe |
| `Assets/Settings/FrontRooms_URP.asset` | 18584f2bb983c440f38ba4af08f098caf1cc1678 | see §7.3 |
| `Assets/Scripts/Rendering/FrontRoomsExitSign.cs` | 4848e4866d4919945506f4c416ce9158f50d752d | no (read) |
| `Assets/Scripts/Rendering/FrontRoomsPostStack.cs` | 6882466191d50443248c9504ee0767e3e88f3a9f | no (called) |
| `Tools/Blender/frontrooms_kit/kitlib.py` | d6de07ab869aee960537aaf5ef2b239bb70cca6d | no (used) |
| `Assets/Scripts/FrontRoomsHunter.cs` | 57d9fad57af1bcf2539870627f8d623edea8c13d | no (not visual-owned) |

### 7.3 Open packaging points (for the orchestrator, not Red's design calls)

1. **URP asset.** `m_ReflectionProbeBoxProjection` and `m_ReflectionProbeBlending` (0 → 1) are in `Assets/Settings/FrontRooms_URP.asset`, outside the ship list.
   - The RenderSetup hunk writes them, but only when RenderSetup runs, and it must never run outside the clone.
   - Two ways forward: ship the two-line asset diff with approval, or the probes stay inert in main until RenderSetup's owner runs it there.
   - **Test P0** first checks whether Forward+ needs the blending flag at all. If not, only box projection is needed.
2. **WebGL keyword stripper.** Its path is in `Editor/Rendering` but it is not a test (§4).

---

## 8. What stays open for Red (calls 2–8) and how B leaves each open

| Call | Open question | How B is built so it does not decide it |
|---|---|---|
| 2 | May a trigger room author its own goal door (CR-6a), or must Run! end on a zone border? | The rig takes the goal as a layout point and normal and builds no door. In the stream, the goal is the stream's own far door pair |
| 3 | On the trip: a local sound, or silence (EGRESS)? | The rig only raises C# events with positions (`MainsDropped`, `TransferRelay` ×n, `ResetStarted`). No audio. The sound chat decides |
| 4 | Is "the goal door is the brightest object" enough when a near lamp's spill is brighter? | r8 values are shipped (goal head gain 5). `goalHeadGain` is one parameter. VL340 reports the goal region against the near spill again |
| 5 | Run! in Office zones too, or Level 0 only? | The armed look, ambient and post are B's own, whatever the host zone. Placement is the map's |
| 6 | Rebuild the title-stream Run room to match B, or leave it? | `RunLook` defaults to **Ward** (today). B is built and verified behind the switch. Answering is a one-line flip |
| 7 | Shared goal treatment (EXIT kit with the fix, plus a lit door lite): yes or no? | The goal wall sign uses the family default (EmissionGain 1, no light) and there is no lit lite. VL340 measures the goal both with and without a gain-3 sign and lit lite (clone only), so Red has the numbers |
| 8 | The media batch? | Nothing is downloaded. Modelling uses `03` §4 and the r8 models; the A5 twin-head photo stays pending |

**B's own carried items** (from `40_for_red.md`; not numbered calls):
- **The cart:** fixed by placement (§2.4). Gate ≥ 2 : 1 at S1b and at 8 m.
- **p95/p5 ≥ 8 and saturated red 1–3 % do not fit the pool target:** the r8 recommendation is kept ("I kept the pools"). Both are reported.
- **The five checks never made in any direction** (sprint pass, reset, stage-1 warning, a Relay pushing through a door, WebGL): all are in §9.

---

## 9. Test plan

**Load rule.** Record the 1-min load (`uptime`) before and after every timed run.
- A timing taken at load > 32 is marked **UNDER LOAD**. It is reported but is not a pass/fail gate.
- Counts and pixel gates do not depend on load.

| Gate | What | How | Bar |
|---|---|---|---|
| P0 | Does Forward+ need `m_ReflectionProbeBlending`? | Stream S1b: blending flag on vs off, keywords and box projection on | Decides §7.3.1 |
| T1 | Compile | Unity (OSXUniversal) + `roslyn_check.py` on main + package (macOS and iOS defines) | 0 errors |
| T2 | `FrontRoomsRunRigTests` | EditMode batch | All PASS: `Evaluate` deterministic; per lamp ≤ 3 changes in any 1 s (trip, reset, warning, stutter); Reduce Flashing envelopes ≥ 0.5 s with no black gap; one-shot trip; idempotent reset; 0 bytes GC in `Tick`; recycle and rebase keep lights, probes and post attached; the `FRONTROOMS_WEBGL_PREVIEW` gate compiles |
| T3 | Ward and the rest of the game unchanged | `FrontRoomsRoomVisualsCapture` (65 frames) + `FrontRoomsLookdevCapture` (10), base vs package, with `runLook = Ward` and the probe flags on | Every frame ≤ 1/255. Otherwise ESCALATE (it would mean the flags change Mac/Win outside Run) |
| T4 | Hero and LODs | Three-views, 0.3 m close-ups of every new LOD0, LOD1/LOD2 at their switch distances, `CxVisLodDump` | No facets, seams or blurry decals at 0.3 m; within the §1 budgets; LOD pop ≤ 2 % mean Y in a 64 px window |
| T5 | **The proposal frames matched** | Harness `-runRig prod -runLayout corridor` at the r8 cameras: S1a, S1b, S2, S3, C1, C2, C3, C4, C4k, x_goal, r_cart, r_truck | Per frame, p50 and p95 within ±15 % of `harness/emergency_power/metrics_final.json`; pool centres at z 6 / 12 / 18 / 25.5 within ±20 % of r8 (.17 / .33 / .49 / .27 × armed); S2 shows the twin-heads + main + door ≥ 40 px; a side-by-side sheet against RV27's slots (s1b, s1a, s2, s3, c4k) and the VL194–195 / VL236–237 images, read by eye |
| T6 | **Relay at 2.05 m** | Contract stand-in (proxy × 1.114, top 2.05 m), ID mask, 12 px ring. Positions: C4k in the corridor; in the stream room, looking back at the Relay in a gap and at the broken rear door; the WebGL preview | **≥ 4 : 1, protan included** (r8: 7.68 / 7.74). Glare reported. The Relay rig model is re-run when it fits 2.9 m (report only) |
| T7 | Fairness | S1b and the stream analogue in grey | Goal region reported against the brightest fixture spill (call 4); obstacles ≥ 2 : 1 mean vs ring, outline reported, also at 8 m; no pockets: corridor axis every 0.5 m min ≥ max/40, stream every reachable 0.5 m floor cell; lane ≥ 1.4 m |
| T8 | Box probes | Magenta / green debug; then Play mode: trip, and read the floor region over the next frames | Floor ROI ≥ 95 % magenta, 0 % green. In Play mode, the tripped set shows within 2 frames, and the floor ROI mean is within 3/255 of the per-state batch reference (the atlas follows) |
| T9 | **Photosafety** | 30 fps timelines (frame-mean Y + saturated-red area, WCAG 2.3.1 general and red flashes) of: the trip, the reset, a stage-1 warning burst, the stutter, the sprint pass. Reduce Flashing off and on | ≤ 3 flashes in any 1 s; no red-field flash; Reduce Flashing: no black gap, 0 flashes. If the reset sweep fails, cut it to 1 flick per lamp, out of sync, so the frame mean stays monotone |
| T10 | Sprint pass | Camera at 5.5 m/s from the trip line to the goal, vFOV 76, head bob; corridor and stream room | Every obstacle ≥ 2 : 1 in the frame 8 m before it; pool rhythm ≤ 0.92 Hz (corridor), ≤ 1 Hz (stream) |
| T11 | **Frame cost** | Play mode, fixed poses (S1a, S1b, C4 in both layouts), 300 frames each, `FrameTimingManager` + editor stats; Ward at the same pose as the base | Counts within §3. GPU ≤ Ward + 1.5 ms (SP) at load ≤ 32. First second after the trip: max frame ≤ 1.5 × median. No "too many shadowed lights" warning |
| T12 | WebGL tier | Clone with `FRONTROOMS_WEBGL_PREVIEW` | §4 light counts; T6 and T9 pass; no probe objects; with the define off, the T5 frames are identical |

**The proposal frames to match**, all in `../images/`:
- **Glossy, r8:** `run_dir_emergency_power_{s1a,s1b,s2,s3,c1,c2,c3,c4,c4k}.jpg`.
- **Extras:** `…_{x_goal,x_unit_armed,x_unit_tripped,r_cart,r_truck}`.
- **Grey and protan:** `…_{s1b,c4}_grey`, `…_c4_protan`.
- **Floor crops:** `run_floor_b_{s1a,s1b}`.
- **Probe checks:** `…_v_reflections`, `…_v_probefix`.
- **Relay crops:** `run_fix_v_relay_contract`.
- **In Figma:** RV27 `2865:6310` and RV29–RV31; VL194 `2855:6151`, VL195 `2855:6177`, VL236, VL237.

---

## 10. Risks

- **The Play-mode probe atlas may not follow state toggles** (r8 proved it only in batch). Fallback: keep one probe set and blend two custom cubes in the rig (more memory), or keep the r8 matte-reflection path for the tripped state.
- **Two units may leave pockets in the 11.5 m room.** The pre-set answer is U3 and U4 (§2.3); cost +4 shadowed spots.
- **The cookie and shadow atlas is under pressure** with the previous room visible through the door. Watch for T11 warnings.
- **SH property blocks break SRP batching** for the corridor renderers. Shell slabs are few, but the props count; T11 decides. The fallback is a box-masked ambient term in the Surface shader (more shader work).
- **Main moves daily.** The RoomStream hunk (sha1 4e6e8e7f) and the RenderSetup hunk are rebased at package time, never against stale copies.

## 11. Verification rows (specs go to `W/vl_pending/run_b.md`, VL338–VL341 only; reserved in VERIFICATION_LOG 2026-10-22 08:1x)

| VL | Check |
|---|---|
| VL338 | B kit at hero spec: twin-head unit (box, yokes, heads) and the hero wrap at 0.3 m, LOD0/1/2 at their switches, the cookies, the 13 secondary kits (T4) |
| VL339 | B in the RoomStream Run room: switch off is identical to today (T3); switch on, armed / blackout / tripped, against the r8 corridor reproduction and the RV27 proposal frames (T5) |
| VL340 | Fairness: Relay at 2.05 m ≥ 4 : 1 with protan (T6); goal, obstacles, no pockets (T7); box probes in Play mode (T8); the call-4 and call-7 numbers |
| VL341 | Photosafety timelines with and without Reduce Flashing (T9); sprint pass (T10); frame cost with the load marked (T11); WebGL tier preview (T12) |
