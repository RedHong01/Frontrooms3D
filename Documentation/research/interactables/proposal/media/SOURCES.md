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
