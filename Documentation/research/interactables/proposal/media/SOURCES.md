# Proposal media: sources

Media for the Figma section "FRONTROOMS · DOORS + WINDOWS · PROPOSAL". Nothing in this folder was downloaded for it.

- **Film and tape stills** are single frames cut on 2026-10-03 from videos that were downloaded on 2026-10-02 with Red's approval, for the IP research section. They are listed in `Research/week02/ip-research/SOURCES.md`. The raw files sit in the 平面视觉 session scratchpad (`658709b9…/scratchpad/src1080/`). Frames were cut with `ffmpeg -ss <t> -frames:v 1` at the native size.
- **In-engine frames** are our own captures. They were made by the door-gap harness (`../../harness/FrontRoomsDoorGapCapture.cs.txt`) in the private clone `proj_audit` on 2026-10-02, and copied here at full size (1920 × 1080) from `scratchpad/proj_audit/Verification/door_gap*/`.

## Film and tape stills

| File | Source | Time | Size | License |
|---|---|---|---|---|
| `a24_trailer_0127_wood_door_knob.jpg` | A24, *Backrooms* Official Trailer HD, https://www.youtube.com/watch?v=0HjdiohVOik | 87.0 s (1:27) | 1920 × 1080 | © A24. Used as research reference only, as in the IP research section |
| `a24_trailer_0139_dark_door_level0.jpg` | same | 99.0 s (1:39) | 1920 × 1080 | same |
| `kane_emg_0053_oak_door_lever.jpg` | Kane Pixels, "Backrooms - Everything Must Go", https://www.youtube.com/watch?v=ewZx0bnBb30 | 53.0 s (0:53) | 1310 × 1080 | © Kane Parsons. Research reference only |
| `kane_emg_0125_door_six_bolts.jpg` | same | 85.0 s (1:25) | 1310 × 1080 | same |
| `kane_emg_0308_framed_window_1990.jpg` | same; the VHS stamp reads 06/19/1990 | 188.0 s (3:08) | 1310 × 1080 | same |

Also used from existing ledgers (not copied here):
- `Research/week02/kit-references/a24/a24_trailer_0115_blue_painters_tape.jpg`: A24 trailer at 115 s (1:55), listed in `Research/week02/kit-references/SOURCES.md`.

## In-engine frames (ours)

The seed is 516574485, and the door is (276, 202) → (277, 202): side A is Level 0 Low, side B is Level 0 Standard. FOV is 76° and the eye is at 1.62 m. "Near dark" means the near room's lamps are at 8 % and a shadowed spot lights the far room.

| File | Original | What it shows |
|---|---|---|
| `ingame_door_gap_16_sideB_hinge_inline_darknear.png` | `door_gap/16_sideB_hinge_inline_darknear.png` | Today. The eye is on the hinge jamb line, 1.2 m out. A lit slit runs the full height, with the far troffer visible through it |
| `ingame_door_gap_18_open_fromA_t0.13s.png` | `door_gap/18_open_fromA_t0.13s.png` | Today. Pushed from side A, 0.13 s into the 0.55 s swing |
| `ingame_door_gap_21_open_fromB_t0.13s.png` | `door_gap/21_open_fromB_t0.13s.png` | Today. Pushed from side B: the same leaf swings the other way |
| `ingame_door_gap2_12_today_sideB_hinge_inline.png` | `door_gap2/12_0before_sideB_hinge_inline_darknear.png` | Today. Same view as frame 16, in the run that also captured Option A |
| `ingame_door_gap2_74_optionA_sideB_hinge_inline.png` | `door_gap2/74_A_sideB_hinge_inline_darknear.png` | The Option A prototype (boxes) in the same view: no slit |
| `ingame_door_gap3_00_floor_leak.png` | `door_gap3/00_S_sideA_floor_darknear_B.png` | A door-shaped patch of light on the floor: the far lamp's shadow leaks through the thin leaf |
| `ingame_door_gap3_01_floor_shadowproxy.png` | `door_gap3/01_S_sideA_floor_darknear_B_shadowproxy.png` | The same view with a 0.14 m shadows-only box inside the leaf: the patch is gone |

The shadow pair was captured on the retired Option B prototype, which has a push plate on its leaf. The leak and the fix are the same on Option A (`../04_door_re8_gap.md` §1.5, `../10_spec.md` §1.7).

## Added in the fix pass (2026-10-03, about 17:00)

Nothing here was downloaded. Every file is our own capture or render.

**In-engine mock-ups from the locked-door test** (`../05_locked_door_type.md` §5; harness `../../harness/05_FrontRoomsLockedDoorReadability.cs.txt`; private clone `proj_int`, 2026-10-02). They stay in `../../images/` and were cropped for the deck only.

| Used on | Source file | Crop (x0, y0, x1, y1) | What it shows |
|---|---|---|---|
| DW03, `media:dw03_locked_mockup` | `images/05_r3_close_L0lit_recommended.jpg` | (180, 120, 1150, 842) | Level 0 game light at 2.4 m: the wood door with its lever, and the almond locked door with dark frame, kick plate, sign, number plate and knob. Primitive mock-ups, not the kit |
| DW12, `media:dw12_peek_lite` | `images/05_r3_close_Office_recommended_lite.jpg` | (600, 80, 1300, 601) | Office grade: the locked mock-up with the 4 × 25 in dark lite (the "peek window" option) |

The blue-tape still (A24 1:55) is no longer on a slide. Its point (blue marks openings, so blue is not our lock colour) stays in `../01_content_plan.md` DW03 notes.

**Spec-albedo previews (ours; copies in `wip/`).** Rendered in Blender 4.x Cycles (96 samples, AgX) from the kit FBX files in the private clone `proj_int`, read only. The colours are the measured Unity means from `../10_spec.md` §7.3 (sRGB): `Prop_SteelBrown` 58/45/36, `Prop_WoodWalnut` 73/47/31, `Door_Veneer` 157/109/59, `Prop_WoodOak` 154/106/57, almond enamel 205/197/176. Frame and leaf only; no lockset, sign or plate yet. Script: `wip/spec_doors.py.txt` with `wip/scene_lib.py.txt` (a copy of the G1 scene helpers, output redirected).

| Used on | File | Crop |
|---|---|---|
| DW05 `img:DoorSet_L0-F_persp` | `wip/spec_l0f.png` (wood casing, veneer leaf at 95°) | (194, 0, 1407, 900) |
| DW05 `img:DoorSet_L0-K_persp` | `wip/spec_l0k.png` (bronze steel frame, almond steel leaf, kick plate) | (204, 0, 1417, 900) |
| DW05 `img:DoorSet_OF-F_persp` | `wip/spec_off.png` (bronze steel frame, oak leaf, closer) | (274, 0, 1487, 900) |
| DW05 `img:DoorSet_OF-K_persp` | `wip/spec_ofk.png` (bronze steel frame, almond leaf, closer) | (274, 0, 1487, 900) |
| DW08 `img:Kit_WindowFrame_Wood_persp` | `wip/Kit_WindowFrame_Wood_faceA_1p5m.png` (in a wall, with the stand-in slab) | (0, 4, 1200, 897) |
| DW08 `img:Kit_WindowFrame_Steel_persp` | `wip/Kit_WindowFrame_Steel_faceA_1p5m.png` | (0, 4, 1200, 897) |
| DW09 `img:Kit_WindowFrame_Wood_close` | `wip/Kit_WindowFrame_Wood_stoolA_0p3m.png` | (0, 150, 1200, 810) |

The window renders come from a copy of the G4 check script (`scratchpad/g4/g4_check.py`, which already uses the same measured means), run into `scratchpad/dwfix/render/out_g4/`.

**Other work-in-progress previews (the interactables workflow's, in `scratchpad/interact_previews/`; temporary).**

| Used on | File | Crop |
|---|---|---|
| DW06 `img:LockSet_Mortise_persp` | `G2/selfcheck/e_poseP_key090.png` (the proposed lock from pose P, key in and turned) | (660, 400, 1060, 696); the frame, which renders in a stand-in colour, is cropped out |
| DW07 `img:KeySet_Desk_persp` | `G3/G3_flat_desk_close.png` (brass key, split ring, red tag "14") | (204, 0, 1304, 816) |
| DW08 `img:Kit_WindowFrame_Alu_persp` | `G4/Kit_WindowFrame_Alu_faceA_1p5m.png` (rendered after the G4 script switched to measured albedos) | (0, 4, 1200, 897) |
| DW12 `img:Kit_ExitSign_persp` | `G3/G3_exit_sign_close.png` (red letters) | (140, 0, 1484, 1000) |

**Re-cropped evidence.** DW02 `media:dw02_leak` / `media:dw02_proxy` now use a 2× crop on the threshold, (700, 560, 1220, 845), of `ingame_door_gap3_00_floor_leak.png` and `ingame_door_gap3_01_floor_shadowproxy.png`. DW08 `media:dw08_kane_window` now uses (0, 120, 1290, 1080) of `kane_emg_0308_framed_window_1990.jpg`, which drops the burnt-in VHS stamp entirely; the chip carries the year.
- DW09 `img:Kit_WindowFrame_Steel_close`: `wip/Kit_WindowFrame_Steel_screws_0p3m.png` (spec albedo, same G4 script copy), crop (0, 95, 1200, 835) with the flat grey edge columns stretched 74 px per side to reach the slot's 1.82 : 1. The render itself is unchanged.
