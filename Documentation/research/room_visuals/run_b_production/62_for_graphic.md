# 62 · Run! B lettering: the review ask to 平面视觉 (from 游戏视觉)

From 游戏视觉 (account A, session local_5656cffd), 2026-10-31, Run! B land stage. Written, not sent: 平面视觉 (信息设计 for signs and plates) picks it up from here. Type, lettering and stamps are yours. 游戏视觉 drew the faces below as **STAND-INs** in a face already approved in main (TeX Gyre Heros Bold / Bold Condensed, `Assets/Fonts/Period1990/TeXGyre`), so the kits could land. None of it is final lettering. Background: `20_build.md` §9 item 4 (open since 10-22), `10_plan.md` §1.2, `10_run_directions.md` §3.3.

The ALARM ZONE card is a separate ask, already answered: `51_alarm_zone_for_graphic.md` → 信息设计's art landed 10-31 01:23 (`Print/AlarmTell/alarm_zone_card_{albedo,glow}.png`). `CardFaceIsStandIn` stays true until 叙事设计 signs the wording (LANDINGS 01:20).

## 1. When this matters

- Package `W/apply/run_b.sh`, not in main yet. The Run! room part lands **inert** (`runRooms` off): nobody sees the plates, the stencil or the cabinet legend until Run! rooms are switched on (earliest 11-04).
- **One face is live at once: the unit legend.** The stairwell's twin-head emergency unit already shows a legend in main (from `stairwell_a`), and this package replaces it with the production version. That one is seen in today's game, in every stairwell.

## 2. The faces

Replace in place: same paths, same `.meta` GUIDs, same pixel size or the same aspect. sRGB PNG. Colours: fire red #B01E18 (OSHA 1910.144: red = fire protection only), safety blue header per ANSI Z35.1 (the header blue is UNVERIFIED), nothing yellow. C41: no EXIT, no exit arrow and no running man anywhere in a Run! room.

| File (`Assets/Resources/Surfaces/Textures/`) | Pixels | On the object | Stand-in now | Seen from |
|---|---|---|---|---|
| `Run_UnitLegend_A.png` **(LIVE in the stairwell)** | 1024 × 1024 | legend plate on the twin-head battery cabinet (338 × 191 × 114 mm, almond enamel) | boxed header EMERGENCY LIGHTING UNIT / 6 V - 2 x 12 W SEALED BEAM - 90 MIN; large AC ON, PUSH TO TEST; footer CHARGE RATE - TEST MONTHLY; black on almond | 0.3 m close-up (the design target), 2–6 m in passing |
| `Run_PlateNotice_A.png` | 1024 × 512 | `Kit_SafetyPlate_10x7`: 254 × 178 mm (10 × 7 in) sheet plate, 1.6 mm, four screws; outside the arch at about 1.5 m, on the right | blue NOTICE header (white), AUTHORIZED / PERSONNEL ONLY black on white | the approach, 2–6 m |
| `Run_PlateElectrical_A.png` | 1024 × 512 | the same plate on the locked side door at z 5 m, 1.75 m up | ELECTRICAL ROOM / AUTHORIZED PERSONNEL ONLY | at a run, 1–4 m |
| `Run_PlateStaff_A.png` | 1024 × 512 | the same plate on the locked side door at z 17 m | STAFF ONLY | at a run, 1–4 m |
| `Run_StencilNotExit_A.png` (RGBA, alpha clip) | 2048 × 512 | spray stencil on the locked side door at z 23 m: 560 × 140 mm decal, 76 mm letters | NOT AN EXIT, Heros Bold Condensed with stencil bridges | at a run, 1–4 m |
| `Run_PlatePull_A.png` | 512 × 1024 | face of the pull station (130 × 180 mm body, face #C42A20), 1.2 m up, by the entrance | FIRE / PULL DOWN, white on red | 1–3 m |
| `Run_PlateExtinguisher_A.png` | 512 × 2048 | the FIRE EXTINGUISHER cabinet's legend (0.24 × 0.61 m trim, white enamel) | FIRE EXTINGUISHER in vertical red letters | 1–5 m |

Pixel density: the plates hold 4.0 × 2.9 px/mm (≥ 2.5 px/mm keeps a 0.3 m close-up sharp at 1080p, vFOV 76°). The textures import with Repeat wrap (main's importer clamps only the EXIT sign decals), so keep a 4 px quiet border in the face's edge colour.

## 3. What we need back

1. Your faces (or "stand-in approved as is") for the seven files, the legend first because it is live.
2. Any wording change on the plates (叙事设计 signs strings; the period basis is OSHA 1910.144/145 and ANSI Z35.1, 1975–1992).
3. If you want a different plate size, say so: it is one number in `run_safety_plate.py` (shipped in `Tools/Blender/frontrooms_kit/assets/` with this package).

游戏视觉 swaps your files in with the same paths and GUIDs, as with the alarm card.
