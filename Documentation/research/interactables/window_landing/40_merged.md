# Window landing — merge record (40)

Date: 2026-10-07 23:24. Machine load 45.9 (no timings taken; none needed).

## Verdict: APPLIED (already in main)

The facade r6 is in Red's project. Nothing was written by this pass.

- Merged 2026-10-07 19:4x by the main visual session (Red approved in chat "视觉材料未同步到主场景"). Red's auto-commit `ee5c9bb` (20:22) holds it.
- `Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs`: main's Codex r3 copy `e1472c6d` → r6 `07dc067e` (sha1).
- Its `.meta` is unchanged: sha1 `48039b7a`, GUID `fe28a88ad796546fab9679489bceb49c` (main's).
- Backup of main's r3 copy: `W/backup/2026-10-07_window_landing`.

## Dry run today (Red's rule a)

`bash /Users/redwang/FrontRoomsVisualWork/apply/window_landing.sh` → exit 0:

| Check | Result |
|---|---|
| 1 base path | already holds the payload `07dc067e` |
| Kept `.meta` | unchanged `48039b7a` |
| MapWorld (contract base) | `9abacc6f` sha1 = sha256 `a56fc9e1…`, the base `03_contract_map.md` is pinned to |
| Payload | 1 / 1 file matches |
| Verdict | `ALREADY APPLIED` — nothing to do |

So no rebuild, no rebase and no re-verify (rules a and c). No new images, so no new VL row.
The script lost its execute bit; run it with `bash`.

## What is NOT in main (by design)

| Item | Owner | State |
|---|---|---|
| R1 (guarded `DressWindow` call) + R2 (RT hint follows visible glass) in `FrontRoomsMapWorld.cs` | map chat | Contract `03_contract_map.md`. `FrontRoomsMapWorld.window-kit.r6.diff` and `.r6-all.diff` pass `git apply --check` on today's MapWorld. `.r6-trims.diff` stacks on top of r6 (checked in a scratch copy) |
| T (no map trim boxes under kit frames) | map chat, optional | In `.r6-all.diff`. The codex audit also flags the second casing step at 0.3 m (`10_review_visual.md`, kits F2) |
| Putty `Prop_Putty` (A10) | Red's look call | VL109 WAIT-RED; `FrontRoomsRenderSetup.putty.r6.diff` |
| Red RT slab in L0 window glass (audit V2) | glass RT track | `codex_audit/vis_F2_rt_print_albedo.diff`; not a facade fault |
| Intact glass does not read (VL107 FAIL) | glass track | open |
| Autopilot re-run at load < 32; one RT frame per window member; G4 rebuild | visual chat / G14 / G4 | owed (see `05_for_red.md`) |

## Codex audit

`codex_audit/20_findings.md` still does not exist. The newest reviews (`10_review_visual.md`, `10_review_runtime.md`) confirm main matches the r6 "kit, no trims removal" state. They list no open finding against the facade itself.
