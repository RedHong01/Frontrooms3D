# Codex audit · map contracts (for 关卡设计)

2026-10-14. These are exact patches for files the map chat owns (`FrontRoomsMapWorld.cs`, `FrontRooms3DGame.cs`). The visual chat does not edit those files: the map chat applies them, or not, after reading each `.md`. Every patch was built against **today's** files (main `0a9ee7f` + Red's working tree) and checked in a clone.

## 1. The contracts

| # | Finding | File | What it does | Who decides | Order | Verified |
|---|---|---|---|---|---|---|
| **C1** | R2 (major, pre-existing since `f5e99e10`) | `FrontRoomsMapWorld.cs:3757` | The zone key owns a private copy of its material, so a restart no longer turns keys magenta | map chat | any | Play with a forced R: key keeps `Map test / key`, 0 pink renderers |
| **C2** | MAP-3 (minor, Codex) | `FrontRoomsMapWorld.cs:1782` + new `EdgeNoGenerate` | The lamp border rule reads edges without generating chunks; `LampModeOf` stays pure under `-lightleadBorders` | map chat | before C3 | 800 → 0 chunks generated; interaction tests FAIL 151/1 → PASS 152/0 with the border rule; 15,970 lamp modes identical |
| **C3** | MAP-2 (major, Codex B0.1) | `FrontRoomsMapWorld.cs:1309–1338` + new `ChunkLineReach` | Posts on chunk lines stop depending on the neighbour's interior: no notch after a revisit shift | map chat (+ level-transitions for the single B0 rule) | **after C2** | 115 / 240 → 0 / 240 chunks inconsistent after shifts; 90 notches → 0; VL272 |
| **C4** | COV-7 (decision, Codex `c1f2d31`) | `FrontRooms3DGame.cs:2006` | Desktop stamina segments back to 24 × 6 px (phones keep 5) | **Red first** (option a), then map chat | any | compile (macOS + iOS defines) |

Files: `C1_R2_key_material.md` + `.diff`, `C2_MAP3_lamp_border_pure.md` + `.diff`, `C3_MAP2_chunk_line_posts.md` + `.diff`, `C4_COV7_stamina_height.md` + `.diff`. The `.md` holds the why, the evidence and how to test; the `.diff` is the patch (`git apply <file>` from the project root).

Base file sha1s (today): `FrontRoomsMapWorld.cs` `11586bbc4a6c9e3c7a4da98f7a9e3b833b166652`, `FrontRooms3DGame.cs` `70bedd38112e23ceb93373bed44500b702edeb01`. If the map chat changed these files since, apply with `git apply --3way` or by hand from the `.md`; each change is small and anchored on unique lines.

**Apply all four in order:**

```
git apply Documentation/research/codex_audit/contracts/C1_R2_key_material.diff
git apply Documentation/research/codex_audit/contracts/C2_MAP3_lamp_border_pure.diff
git apply Documentation/research/codex_audit/contracts/C3_MAP2_chunk_line_posts.diff
git apply Documentation/research/codex_audit/contracts/C4_COV7_stamina_height.diff   # only after Red picks (a)
```

`git apply --check` passes for C1, C2 and C4 each alone, and for C1 → C2 → C3 → C4 in sequence (on a copy of today's files). C3 also applies without C2 as text, but then does not compile.

### Not in this folder

| Item | State |
|---|---|
| V1 (desktop key panel anchor, `vis_F1_key_panel_anchor.contract.diff`) | **Done** by the map side in `22bb75f` (2026-10-07) |
| Window contract R1/R2 (+ T) | window-landing's: `research/interactables/window_landing/FrontRoomsMapWorld.window-kit.r6.diff` (+ `…r6-trims.diff` for T, Red decides). Still applies to today's file, and together with C1–C3 (checked 10-14) |
| `research/level_transitions/contracts/lightlead_lampmodeof_pure.diff` | **Superseded by C2. Do not apply it**: both touch the same hunk |
| The "+1 reach on every chunk-line post" patch in `../10_review_map-edits.md` §4 | **Rejected** by both MAP-2 verifiers (brings back the 13:09 seam on 1,315 of 5,600 chunk-line posts). C3 replaces it |
| MAP-1 (V5 on by default) | not a contract: Red keeps V5 ON |
| MAP-4 / MAP-5 / MAP-6 | dropped findings; see `../20_findings.md` §4 |

## 2. Verified today (2026-10-14)

Clone `W/proj_cxreport` (`W/tools/mkclone.sh`: main `0a9ee7f` + working tree; `W` = `/Users/redwang/FrontRoomsVisualWork`). Unity 6000.3.10f1 batch, `-buildTarget OSXUniversal`, macOS 27.0.1. Load average 50–75 (other workflows' Unity jobs). Harness: `harness/FrontRoomsCxMapContracts.cs.txt` (clone only, never promoted); contract builder: `harness/make_contracts.py.txt`.

| Check | Main as is | Main + C1–C4 |
|---|---|---|
| Roslyn compile, macOS and iOS defines | — | 0 errors, 0 warnings |
| T1 · still-built chunks ≠ their own rebuild after every other chunk is shifted (20 seeds × 12) | **115 / 240** (B0 off: 0) | **0 / 240** |
| T1 · chunk data generated beyond the 500 built | 429 | 30 (= B0 off) |
| T1b · chunk-line posts (5,600; 1,628 mixed): notch / z-fight / high hole / paper strip, after the shifts | 90 / 9 / 0 / 44 | 0 / 24 / 0 / 196 |
| T1b · same, first build | 0 / 0 / 0 / 48 | 0 / 24 / 0 / 202 |
| T1b · chunk corners (320): z-fight | 0 | 0 |
| T2 · `LampModeOf` on 64 far cells per seed (10 seeds), border rule on: chunks generated | 800 | **0** |
| T2 · built lamps whose prediction ≠ built mode (15,970) | 0 | 0 |
| T2 · every built lamp mode, main vs contracts | — | identical (15,970 / 15,970), border rule on and off |
| T3 · `FrontRoomsMapInteractionTests`, border rule on | FAIL 151 / 1 | **PASS 152 / 0** |
| Project suites, default flags | — | MapVerification 100 / 0, RelayNav 60 / 60, MapInteraction 152 / 0, LevelDesigner 139 / 0, FixtureTick 28 / 0, CameraRig 33 / 0, Captions 9 / 0, GlassShot 20 / 0, Stream 5 / 0 |
| Map signatures, 20 seeds | — | 0 second-world and 0 rebuild determinism failures; 422 of 500 chunks change wall/collider boxes (C3 at mixed chunk-line posts); renderer, collider and light counts unchanged; lights unchanged |
| Play, RT High, 30 s of map, forced R at 12 s (seed 516574485) | key NULL after a restart (apply stage run on main + payload, 2026-10-14 17:0x) | **key `Map test / key` (URP Lit) after R**; 0 renderers that would draw pink; 0 G14 command-buffer errors; only the clone-only FMOD and QuickSearch exceptions |

Logs: `W/logs/cxreport/` (`a_main` … `j_shot_c3`). Outputs: `W/proj_cxreport/Verification/cx_contracts/{main,main2,patched,patched2,sim3,final,shot_main,shot_c3}/`, `W/proj_cxreport/Verification/cx_audit/` (suites, signatures, `play_cr_keys`).

The clone holds main + C1–C4 at the end of this stage. Nothing was written into Red's project except these documents and four images.

## 3. Frames (VL272)

Figma `3150:7042` in FRONTROOMS · VISUAL VERIFICATION LOG, verdict **FAIL main / PASS C3**. Seed 20261002, chunk-line post (0, 4), an Office corner, seen from 0.9 m (FOV 50):

- `../images/map2_notch_main_after_shift.jpg`: main, right after the neighbour's revisit shift: a full-height slot in the corner (the post quarter nobody builds);
- `../images/map2_notch_main_stale_rebuilt.jpg`: main, after the stale chunk is rebuilt: the corner is whole again (158,417 px differ from the first frame);
- `../images/map2_notch_c3_after_shift.jpg`: main + C2 + C3, the same shift: the corner is whole (0 px differ from main's rebuilt view);
- `../images/map2_notch_main_after_shift_wide.jpg`: context (not on the slide).

## 4. Who sends what

- The visual chat sends this folder to 关卡设计 with one line: "Codex audit contracts C1–C4, apply C1–C3 now (C3 after C2), C4 waits for Red; read each .md; C2 replaces level-transitions' lightlead_lampmodeof_pure.diff."
- The map chat replies with what it applied; the visual chat records it in `../00_main_state.md` §0.1 and in `VISUAL_CHAT_TASKS.md`.
