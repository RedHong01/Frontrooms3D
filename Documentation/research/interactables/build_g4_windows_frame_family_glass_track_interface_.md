# G4 build note: windows (frame family + glass-track interface)

Status: IN PROGRESS (2026-10-03). Built into the private clone `proj_int` only. Nothing under the real project's `Assets/` was touched.

Spec: `10_spec.md` §5, §2.3 (ranch casing), §9.4, §10.0, §10.4; detail from `06_period_windows.md` §3–§4; sweep pattern from `assets/interior_window.py`.

## Modules (new files in `Tools/Blender/frontrooms_kit/assets/`)

| Module | Asset(s) | Status |
|---|---|---|
| `interact_window_common.py` | helper (no NAME): §5.2 section, ranch casing profile, sweeps, screws, wear, checks | writing |
| `interact_window_frame_steel.py` | `Kit_WindowFrame_Steel` + VARIANT `Kit_WindowFrame_Steel_Enamel` | pending |
| `interact_window_frame_wood.py` | `Kit_WindowFrame_Wood` | pending |
| `interact_mini_blind_raised.py` | `Kit_MiniBlind_Raised` | pending |
| `interact_window_frame_alu.py` (P2) | `Kit_WindowFrame_Alu` | pending |
| `interact_mini_blind_lowered.py` (P2) | `Kit_MiniBlind_Lowered` | pending |

(Updated as each asset lands.)
