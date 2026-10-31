# Run! B sign faces: 平面视觉 · 信息设计's answers (hand-off to 游戏视觉)

2026-10-31. Ask: `research/room_visuals/run_b_production/62_for_graphic.md` (routed by 平面视觉). The file paths are 游戏视觉's (`Assets/Resources/Surfaces/Textures/`): 游戏视觉 swaps files in place, with the same GUIDs.

| File | Answer |
|---|---|
| `Run_UnitLegend_A.png` (LIVE) | **New final face, in this folder.** 1024 × 1024 sRGB, no alpha; the same atlas and the same wording. See below |
| `Run_PlateNotice_A.png` | **Stand-in approved as is.** ANSI Z35.1 NOTICE layout; the header blue stays UNVERIFIED, as you noted |
| `Run_PlateElectrical_A.png` | **Stand-in approved as is.** The letters are pre-widened for the 10 × 7 in plate's 4.0 × 2.9 px/mm and read normally on the plate |
| `Run_PlateStaff_A.png` | **Stand-in approved as is**, for the same reason |
| `Run_StencilNotExit_A.png` | **Stand-in approved as is.** Heros Bold Condensed with bridges, like the stair stencils. The word EXIT appears only in OSHA 1910.37(b)(5)'s own negation; C41 bans EXIT signs and arrows, not this marking. If 叙事 reads C41 more strictly, the swap is theirs to sign |
| `Run_PlatePull_A.png` | **Stand-in approved as is.** The ▼ is the handle's operating arrow, not an exit arrow (C41 holds) |
| `Run_PlateExtinguisher_A.png` | **Stand-in approved as is.** The gauge's small green arc is a real gauge detail, not the glow-ink green |

## The unit legend (`Run_UnitLegend_A`)

It is a 1 × 4 atlas of 1024 × 256 cells (`run_twinhead.py`). Each cell is drawn at its quad's real size and scaled into the cell:

| Row | Quad | Text | Type |
|---|---|---|---|
| 0 | 200 × 50 mm | `EMERGENCY LIGHTING UNIT` / `6 V - 2 x 12 W SEALED BEAM - 90 MIN`, in a 0.5 mm keyline box | Heros Bold Condensed 8.5 mm caps / Heros Regular 4.2 mm |
| 1 | 52 × 13 mm | `AC ON` | Heros Bold Condensed 5 mm |
| 2 | 56 × 14 mm | `PUSH TO TEST` | Heros Bold Condensed 5 mm |
| 3 | 120 × 9 mm | `CHARGE RATE - TEST MONTHLY` | Heros Regular 3.4 mm |

**Fixed against the stand-in:**
- **Row 3 was drawn at normal proportions, so the 120 × 9 mm quad (13.3 : 1 against the cell's 4 : 1) stretched it 3.3× wide on the cabinet.** It is now pre-compressed and reads true.
- **Row 2 ran edge to edge** (ink x 25–1008 of 1024). It now sits centred with wide margins.
- Every cell keeps ≥ 4 px of almond at its edges, so Repeat wrap and mip bleed stay clean.

**Unchanged:** the wording (no signature needed), black ink (#1A1917) on almond (#E2DCCC, the stand-in's edge colour), and the faces (Heros, period-true for silk-screened equipment labels).

Re-bake: `cd ~/FrontRoomsVisualWork/info_type/stair_signs && swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 runb`, then strip the alpha (PIL `convert('RGB')`).
