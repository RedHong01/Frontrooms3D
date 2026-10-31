# zone_lamps · 00 · Inventory: every zone, its ceiling, its lamp today, the logic it must keep

游戏视觉, zone-lamps workflow, inventory stage, 2026-10-31. Main was read only (no Unity, no git); no file under
`Assets/Scripts` or `Documentation` was dataless (rule 20 check). Nothing was downloaded or rendered.

Red, 2026-10-31 (游戏视觉 chat): "目前不同区域内的灯的样式应该是符合对应区域的，我需要你对年代进行调研，然后思考比如说泳池房间（在这里泳池房间指的是后室概念里的特定泳池level，以及那个年代的泳池会使用的设施）包括run level以及office level都是，都需要对应的调研以及灯具的revise".

Base files (sha1 prefix): `FrontRoomsMapWorld.cs` 42d12c6d, `FrontRoomsMap.cs` e8ef1b1f, `FrontRoomsRoomModuleData.cs` 1200a918,
`FrontRoomsLevelProfile.cs` 2e7010d2, `FrontRoomsWarnLamps.cs` 076756c3, `FrontRoomsStairwell.cs` ceafb945, `FrontRoomsStationSet.cs` b4d25030,
`FrontRoomsRoomStream.cs` 675d5589, `FrontRoomsRunRig.cs` 2f7938ac, `FrontRoomsStairwellDresser.cs` 2abeed3f,
`FrontRoomsTransitionLightLead.cs` 8f41b31e, `FrontRoomsSurfaces.cs` ab279371, `FrontRoomsLevel0.asset` 6fb270d3
(also in `~/FrontRoomsVisualWork/zone_lamps/00_inventory_base.txt`). Line numbers are for these versions.

Reused, not redone: the lamp study `~/FrontRoomsVisualWork/lamp_options/docs/` (10a period products, 10b real looks,
10c current kit, 20 options L00–L10, 30_for_red; L03 picked by Red 10-31 12:3x, lens 1.8), `research/bathroom_rooms/`
(pool research, D1–D12 unanswered), `research/room_visuals/run_b_production/`, `research/stairwell/`, `research/station/`,
`research/level_transitions/`.

## 0. Short answer

1. **The real zone enums are small.** `ZoneHeight { Low 2.4, Standard 2.9, Tall 5.4 }` × `ZoneTheme { Level0, Office }`
   (`FrontRoomsMap.cs` l.13–16, 62–69); Office is Standard only (l.336). Shares 35 / 55 / 10 %, Office 30 % of Standard
   (`FrontRoomsLevel0.asset`). `MapSkin { Zone, Station }` and `ModuleFinish { Zone, Run }` add the station and Run! skins.
   `RoomRule { Lobby, Shift, Office, Run, Exit }` is the title stream only, and only Lobby is in play.
2. **Every map zone has the same lamp.** `FrontRoomsMapWorld.BuildFixture` (l.3569) puts one 0.6 × 1.2 m lens box, no
   housing, at X [1.2, 1.8] Z [1.2, 2.4] of every 3 m cell, Low, Standard, Tall, Office and station alike. Only colour,
   intensity and lens material differ by theme (`BuildMaterials` l.6542), plus ×1.6 intensity and range 12 when the
   ceiling is over 4 m. So a 5.4 m hall, an 8 ft storage room and an Office bullpen all hang the same lay-in troffer.
3. **Office's "parabolic louver" does not exist.** `FrontRoomsSurfaces.OfficeLouver` returns `Troffer_Lens`
   (l.45–48); with the V5 light lead on (default) the map Office uses `Troffer_Lens_Cool`, a cool prismatic lens. The
   stream's comment "parabolic louvers in the Office" (l.1748) is not what renders.
4. **Zones with their own fixtures already:** Run! B (wraparounds + twin-heads, built, switched off), stairwell X
   (wraparound + twin-head on desktop), the station (map lamps with fixed modes + the TV spot). These three are decided
   looks; the revise is about Level 0 Low/Tall, Office, the stream Lobby seam, and the planned pool club.
5. **No pool zone exists.** `grep pool` in Scripts finds object pooling only. The pool club is L8 (NOT_DESIGNED),
   research landed 10-31 00:26, Red's D1–D4 and D8 block the build.

## 1. Zones the player can be in today

| # | Zone / room | Ceiling (type, height) | Fixture today, code path, owner | Count / spacing | Lamp logic it must keep | What a c.1990 building had there (first answer; sources in the docs named) |
|---|---|---|---|---|---|---|
| 1 | **Title corridor + start rooms** (stream `RoomRule.Lobby`; Shift/Office/Exit not in play, `IsPlayable` false, only editor tests call `BeginPlayableSequence`) | 2'×4' lay-in grid print on a 0.2 m solid box, 2.9 m | 2×4 pan 0.588 × 1.188 (`Painted_Metal`) + prismatic `Troffer_Lens` + beam + Spot, (1, .96, .88) at 5.2, range 10. `FrontRoomsRoomStream.BuildRoom` l.1144, `AlignFixtures` l.1231. **游戏视觉** | 12 per 11.5 × 12 m room: columns 0, ±3.6 m; rows z 1.7 / 4.55 / 7.4 / 10.25 (2.85 m); 60 across the 5 pooled rooms | Per-room ballast strike (Waiting → Striking → Rising → Steady / Unstable / Dead, l.888), door-cued reveal, Lobby odds dead 2 % / unstable 10 %, rebase-safe grid, start-room freeze, StartLamps groups on the map side; the start-door seam with the map lamp (Q4) | Lensed 2×4 prismatic troffer = the installed default (lamp_options 10a family 1, S02/S03/S06). Must match Level 0's L03 or the start door shows two lamps |
| 2 | **Level 0 · Standard** (`ZoneTheme.Level0`, the bulk of the map; modules `L0_WaitingRoom_4x3`) | 2'×4' lay-in print (`Ceiling_Fissured`, 23.8 mm T-bar) on 0.16 m slabs, 2.9 m | Lens box only, Level 0 lens = map copy of `Troffer_Lens` × 1.5 (`Level0LensEmissionScale`, being dropped for L03's 1.8), (1, .96, .88) at 5.0, Spot 162°/96°. `BuildFixture` l.3569. Placement/modes **关卡设计**; lens, colour, intensity **游戏视觉** (LEVEL_MODULE_SPEC l.73–76) | 1 per 3 m cell (9 m² a lamp); none in reserved cells or module `Off` cells | All map rules in §3 | **L03 decided** (air-handling 2×4, dark return channel each side; 10a family 8). Open: the odd-`cell.y` half-tile fault on the 1.2 m print (10c §2) |
| 3 | **Level 0 · Low** (35 % of zones; never columns; module `Low_Storage_2x3`) | Same print, 2.4 m (8 ft) | Same lens box and light as #2 | 1 per cell | §3 | An 8 ft lay-in ceiling is back-of-house: service corridors, storage, tenant support. 2×4 / 2×2 / 1×4 lensed, or surface wraparound and strip in storage (10a families 6, 11, 12; S06 shipment shares). [To verify: a dated 8 ft back-of-house photo] |
| 4 | **Level 0 · Tall hall** (10 %; one 5–7-cell hall per zone; 0.9 m columns 70 %; module `Tall_PillarHall_6x5`; reflection cube `Refl_Tall`) | Same 2'×4' print at 5.4 m (about 18 ft) | Same lens box; intensity ×1.6, range 12 (l.3605) | 1 per cell, so a lay-in troffer every 3 m at 18 ft | §3 | A lay-in grid at 18 ft is unusual. Period candidates: recessed or pendant HID downlights, incandescent cans in lobbies (10a family 9, S09), HID 6.2 % of 1986 floorspace (S01). [To research: dated atrium / tall-lobby photos and catalog pages] |
| 5 | **Office** (`ZoneTheme.Office`, Standard only; cubicles, CRTs, bulkheads; module `Office_Bullpen_4x4`) | 2'×2' lay-in print (`Office_Ceiling`), 2.9 m; 0.9 m columns with bulkheads | Same lens box; `Troffer_Lens_Cool`, (.90, .96, 1.0) at 6.0 (V5 light lead on by default), else `Troffer_Lens` at 5.5. **关卡设计** placement / **游戏视觉** look | 1 per cell; lens ≥ 0.15 m from any bulkhead | §3, plus the light-lead border rule (opt-in `-lightleadBorders`) | A CRT floor of 1990 = parabolic louver troffer: 2×4 18-cell three-lamp (patent filed 1988, US4907143) or 2×2 9-cell U-tube (1988), IES RP-24 VDT practice 1989 (10a families 3, 7; S21, S25, S32). Cool white F40. Study options L06 / L07 exist |
| 6 | **Station** (Floor 1 first room `Station_FrontDesk_4x4`; floor stations `Station_Waiting_3x2` Level 0, `Station_Desk_3x3` Office; `MapSkin.Station`) | Map ceiling, `Station_Ceiling` (L0 copy, URP step) + `Station_CeilingBand`, 2.9 m | Map lamps with Level 0's lens and lamp (`StationMaterials` l.1574); every module lamp set, no Auto; plus `FrontRoomsStationTvLight` spot. Light layers by `FrontRoomsStationSet.TagLamps` l.309. Module **关卡设计**, dress and TV light **游戏视觉** | 4 × 4: 12 station lamps (Steady/Dim) + 2 entrance lamps + 2 Off (key spot, TV corner) | ≥ 2 Steady along E1 (教程设计), key-spot cell Off, TV corner Dim/Off, entrance pair on both light layers, TV light × `StartLampsNorth`, placard lamp Steady | Lobby security desk: the floor's troffers; possible task light at the counter [U]. Keep L03 for consistency with Level 0 (station research 01) |
| 7 | **Stairwell X** (C22 EXIT stairwell, safe zone; 1 cell) | Inner shell, painted slab, 2.40 m | Map part: one point lamp, range 1.5 m, 1.3 m high, I 2.5 (`FrontRoomsStairwell` l.73–80, **关卡设计**). Desktop dress: `Kit_WrapFixture_4ft` on the slab + 2 soft spots (2.2, range 4.2) + `Kit_EmergencyTwinHead` (mains on: heads dark, red AC pilot) (`FrontRoomsStairwellDresser` l.616, **游戏视觉**); phones/WebGL keep the map lamp | 1 wrap per 3 × 3 m cell | Steady always; follows only `LampLevel` (C22 blackout swap); never W1; `_FR_OWN_LIGHTS` keeps map lamps out | **Decided** (stairwell A, Red 10-22): wraparound + battery twin-head = stair egress lighting (research/stairwell SOURCES) |
| 8 | **Level transitions** (Office ↔ Level 0 crossings: Frame, ~30 % Renovation, V5 light lead) | Each side's own; renovation cuts tile holes (ceiling cut 2.67 m) | Map lamps; light lead sets colour and Office lens per theme (`FrontRoomsTransitionLightLead` l.21–38, **游戏视觉**); kits in main since 10-30 21:37, inert until 关卡设计's contract (A47) | 1 per cell | Border rule (opt-in): L0 cell Open/Arch into Office = Dead, door/window only = Dim, Office border cell = Steady; renovation holes keep off the lens rectangle (Renovation kit l.1464) | The lamp change is the cue: lensed L0 → louvred Office reads as a refit (10a §2). A new fixture must fit the renovation keep-out |

## 2. Built, not reachable in play

| # | Zone | Ceiling | Fixture | Count / spacing | Logic | Period |
|---|---|---|---|---|---|---|
| 9 | **Run! B alarm corridor** (`Run_AlarmCorridor_1x9`, `FrontRoomsRunRooms`; profile `runRooms` false; playable earliest 11-04) | Painted concrete soffit 2.9 m (`Run_SoffitPaint`) with exposed sprinkler main, tray, EMT; no grid | Map cells lamp Off; `FrontRoomsRunRig` (l.214, **游戏视觉**): 9 `Kit_WrapFixture_4ft` on the axis, (.90, .95, 1.0) at 5.5; 4 twin-head units, warm (1, .84, .64), goal head ×5; sealed-beam lens; C42 goal-window light. Planner **关卡设计** | Wraps every 3 m (z 1.8 + 3k); twin-heads at z 3 / 9 / 15 / 21, alternating walls | Armed (cell 5 stutters at 0.78) → trip (mains 1 → 0 in 0.06 s, 0.35 s black, heads rise) → reset sweep; W1 warn on its own wraps; Reduce Flashing path; WebGL tier | **Decided** B (Emergency Power, 10-21; run_b_production). Back-of-house corridor = wraparound + battery units |
| 10 | **Stream Shift / Office / Run (Ward) / Exit profiles** | Stream grid, 2.9 m | Stream lamps with profile colour/intensity (l.1463–1501); Run stays Ward (C40) | 12 a room | Profile odds (Run dead 70 %) | Not in play; out of scope unless the stream opens up |

## 3. Planned zones (docs)

| # | Zone | Source | Ceiling | Fixture today | Logic it must take | Period (first answer) |
|---|---|---|---|---|---|---|
| 11 | **Pool hall** (tenant fitness club, rec. B2, EXIT across the water) | L8; `bathroom_rooms/10_research.md` §2 rows 15–17, D1–D12 | No dropped ceiling over water; painted structure or acoustic deck, 5.4 m (hall 14.84 × 23.84) | none | "Lamps dip with bursts like any lamp; underwater lights take part" (§3.5); Reduce Motion for thrown light; reflection probe `Refl_Pool` + dead-lamp variant | Vapour-tight sealed fluorescent or metal-halide high bays (S45, S48); wet-niche incandescent underwater lamps, 300–500 W PAR56, grounded and GFCI under NEC Art. 680 (S40, S41, S49). [To research: dated natatorium photos, 1987/1990 NEC 680 text, IES sports/recreation chapter] |
| 12 | **Club front desk, lockers, showers, washroom, pump room** | same, §2 rows 18–24 | Front desk/lockers likely lay-in; showers and pump room hard ceilings [U] | none | as map lamps | Enclosed vapour-tight units in wet rooms [U]; pump room strip or caged jelly-jar [U]; desk as station |
| 13 | **Office washroom (+ mirrors)** | bathroom D7 | Lay-in 2.9 m [U] | none | as map lamps | Lensed troffer + light over the mirror [U] |
| 14 | **Sanctuaries** (dead-circuit rooms the Relay won't enter) | L3 DESIGN (v2.1) | as host zone | none | A dead circuit means mains off: only battery units or exit signs lit [design question] | Emergency battery units (as Run!/stairwell kit) |
| 15 | **Trigger rooms** (L2) beyond Run! | L2 DESIGN | as host | none | trip state like Run! | per room type |
| 16 | **Walkable stair** (1 × 2 cells, 3.5 m drop) | stairwell 60 / 1b | two landings | none | ≤ 6 shadowed spots within 10 m (desktop) | as #7 per landing |
| 17 | **B-floor stations** (A35) | systems N6 | as #6 | none | as #6 | as #6 |

## 4. Lamp logic every map fixture must keep (any new mesh in zones 2–6, 8)

- **Modes** `ModuleLamp { Auto, Steady, Stutter, Failing, Dead, Dim, Off }`; tier odds T1 62/20/10/5/3 → T5 40/25/18/12/5, stutter ≤ 20 %, failing ≤ 12 % (`FrontRoomsLevelProfile` l.173–213); run-time `SetLampMode`, look holds `SetLampHold` (M1), ambient holds (M2).
- **Overrides** `LampFx { Dip, Sag, Warn }` (l.5916); W1 warning bursts (`FrontRoomsWarnLamps`); W5 chase wave (design).
- **Photosafety**: flash limiter ≤ 3/s (P4), Reduce flashing.
- **Lens follows the light**: one renderer per lamp takes `_EmissionColor` × level and `_FR_LampDip` (l.355, 3717); a new housing must stay dark/unlit, never carry the glow (10c §0.5–6).
- **Groups**: start groups (`StartLampsNorth/South`), C22 `ExitLamps` blackout, exit hold 5 × 5 Steady round N (l.1128–1150).
- **Budget**: on within `lightRadius` 16 m (3 m fade), 1 in 3 shadowed within 9 m; WebGL `TickFixturesNearOnly` (l.3712).
- **Readers**: `LampModeOf` → DeadLamp reflection cube; placard glow and EXAMINE read `LampLevel`; G14 glass RT reads lens emission; Foley hum attaches to children named "Light".
- **Geometry keep-outs**: lens rectangle hard-coded in the renovation kit, `FrontRoomsModulePlanView`, Office bulkhead 0.15 m.

## 5. Open points for the research and options stages

1. Tall halls (#4) and Low zones (#3) have no period basis today: research first.
2. Office (#5): parabolic louver vs today's cool prismatic; the parabolic goes dark at low angles (10a §5), which changes how a long Office reads.
3. Pool (#11–12): fixtures depend on Red's D1/D2/D8; research can go ahead, build waits.
4. Start-door seam (#1 ↔ #2): the stream lamp must follow L03.
5. Media wanted go to `media_candidates.md` in this folder; no downloads without Red's OK.
