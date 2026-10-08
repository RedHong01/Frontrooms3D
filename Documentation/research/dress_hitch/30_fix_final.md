# Office dress hitch: fix-stage final report (2026-10-04)

Provenance: this is the final section the fix stage (workflow wf_11cd7561-ccc, 2026-10-04 13:19 UTC) returned; subagents could not write report files then. It is copied here unchanged from `W/recovery/office-dress-hitch.json`, so the record survives. Paths under `scratchpad/` were wiped by the 2026-10-05 reboot; the code was recovered from the stage transcripts (see `40_merged.md` §2), and every check that decides the merge was re-run on the recovered files (40_merged.md §3).

---


STATUS
The fix stage is finished and checked against today's main. Main is at d610d3a. Since the 22:02 sync to 75cfdff, only FrontRoomsMapInteractionTests.cs, FrontRoomsRelayNavTest.cs and FrontRoomsMapHunter.cs changed in code. I copied those three into proj_rs. A checksum rsync of Assets/ProjectSettings/Packages then showed the clone matches main apart from our own files.
- Main's MapWorld (sha 3a3281ad) and its three dresser files are unchanged since the sync: OfficeKit c6e76255, FurniturePile 029c766b, KitLibrary b55b9f57.
- The audit's 00_main_state.md:262 lists "D1 dress hitch … not touched". Codex's only edit in this area is `float[] lodDistances` in KitLibrary.Info, and final/ keeps it at FrontRoomsKitLibrary.cs:59.
- codex_audit/20_findings.md still does not exist. I checked at 06:20, 07:00 and 07:45, so there are no confirmed findings to resolve.
- dress_hitch/final/ now equals final2/ (the main-merged version). The old pre-Codex final/ is kept as final_precodex/.
- No renders were made, so nothing goes to the Figma VERIFICATION LOG.

VERIFIER ISSUES: WHAT WAS DONE
1. [medium] Map integration. Accepted. The ticket is map_ticket3/FrontRoomsMapWorld.dress_step.patch. It applies cleanly to main's current MapWorld, and the result is byte-identical to the tested map_ticket3/FrontRoomsMapWorld.cs. What it does:
   - BeginDress/BeginBuild are bound in ResolveDressers (:1820, :1827).
   - One open job is held in `currentJob` (:419).
   - DressNext(all) (:1854) steps that job before it dequeues the next room, so the next room's PlaceProps never runs while a job is open.
   - StepRoom (:1890) cancels the job if its chunk was dropped or rebuilt, calls Complete when `all` is set, and cancels plus logs a warning on an exception.
   - RebuildChunk (:497) and Begin(buildAllNow) (:566) call DressNext(all: true).
   - ReadyAround and Settled wait for the open job.
   - Budget: 3 ms, or 2 ms behind `#if UNITY_WEBGL && !UNITY_EDITOR` (:429-433).
   - One change from the verifier's suggestion: the Begin* result is cast to FrontRoomsDressJob and Step is called directly (no reflection, no boxing). So the four dresser files must be promoted before the patch.
2. [medium] Stale map. Accepted.
   - Current suites pass with original dressers, with the new dressers, and with the new dressers plus the ticket: MapInteractionTests 143/143, LevelDesignerTests 138/138, FixtureTickTests 28/28, RelayNav PASS 60/60. Results are in runs4/{B,A,C}_*.
   - The corpus was re-captured with today's MapWorld (1,111 cases). Original vs new differ in 0 input hashes, 0 rounded and 0 exact output hashes.
   - 5,555 stepped runs at 0, 0.5, 1, 3 and 100 ms all matched (runs3/A3). The full same-process A/B is 1111/1111 exact (runs4/A_ab_full).
   - DressMapStepTest PASS: stepped 3 ms, 0 ms and Complete all equal the synchronous build and the original-dresser map baseline; RebuildChunk mid-job equals a rebuild after a synchronous build; Settled waits for the open job. It was run against two fresh original baselines (runs4/mapbase_orig, runs4/mapbase_low); both baselines match runs3's dumps byte for byte.
3. [low] First room stalls in a fresh process. Accepted.
   - FrontRoomsDressJob.WarmUp() (FrontRoomsDressJob.cs:180-208) runs FrontRoomsKitLibrary.Prewarm() (:140). It then dresses offices and piles of every tableau, synchronously and stepped, under an inactive HideAndDontSave object, and destroys it. The ticket's Prewarm (:2025-2028) calls it.
   - The first office is now 1.8–2.0 ms in one step. The first pile is 5.3–8.1 ms over 2–3 steps, worst step 3.02 ms (runs3/C4).
   - Before: cold original 33.8–36.7 ms office and 76.8–120.5 ms pile (A6). With the map's old Prewarm: 24.9–34.0 / 60.0–64.9 ms (B3).
   - WarmUp costs 81–311 ms once, before the title. Determinism with WarmUp first: 1,111 cases, 0 mismatches (A2w).
4. [low] Corpus coverage. Accepted. The Extra() generators are in FrontRoomsDressPerfTest.
   - The corpus is 1,111 cases, including 108 cut rooms ("Obstacles cut" logged 582 times in B1).
   - The step report is now unfiltered.
5. [low] Step overrun. Accepted, but done differently. Step (FrontRoomsDressJob.cs:89-118) keeps a running guess for the next unit of each kind: the longest recent unit, decaying by a quarter per unit, capped at half the budget. It stops before a unit that would probably cross the limit. This changes only how the work is split.
   - Step(3f) is now almost a hard ceiling: 0 of 4,037 steps took over 3.5 ms, and the longest was 3.32 ms (full corpus × 3 runs).
   - Step(2.5f) is not needed.
6. [low] Editor harness. Accepted: it is in final/Editor/. The promote list below is explicit.
7. [low] Child order. Accepted. The rule is written at FrontRoomsFurniturePile.cs:198-210 and FrontRoomsDressJob.cs:23-28, and the ticket enforces one job at a time.
8. [low] report.md. This text is it.
9. [info] Glass RT line. Not part of this change; it needs its own task. Its description is now out of date: Codex replaced the ChatGPT RT prototype with G14 and removed Ensure() (00_main_state.md §6, B3). That task should start from main's current G14 code, desktop only.

BEFORE / AFTER (Unity 6000.3.10f1, batch mode, Release JIT)
Same process, alternating calls, full corpus of 1,111 cases × 5 runs (runs4/A_ab_full.json, load 10–20). Synchronous call, median of runs:

| Group | Before mean / p95 / max (ms) | After mean / p95 / max (ms) |
|---|---|---|
| All cases | 4.71 / 24.95 / 43.30 | 1.57 / 5.26 / 8.64 |
| Map rooms (52) | 3.45 / 22.25 / 43.30 | 1.14 / 5.23 / 8.64 |
| Office (707) | 1.83 / 6.82 / 15.26 | 1.13 / 4.48 / 7.50 |
| Pile (404) | 9.74 / 28.56 / 43.30 | 2.33 / 5.59 / 8.64 |

- Total over the corpus: 5,233 → 1,741 ms (3.0×).
- Worst case is the hitch room map20388-c16_10-r1-pile: 43.30 ms (42.6–45.3) → 8.64 ms (7.8–8.8) synchronous. On a quieter run (core corpus, load ~5): 22.91 → 3.99 ms.
- Stepped at 3 ms, the hitch room takes 2 frames, worst step 3.00–3.32 ms.

Stepped, full corpus × 3 runs (runs4/A_steps_full):

| Budget | Steps | Over budget + 0.5 ms | Worst step per room, mean / p95 / max | Frames per room |
|---|---|---|---|---|
| 3 ms | 4,037 | 0 | 1.27 / 3.01 / 3.32 ms | mean 1.21, max 4 |
| 2.5 ms | 4,346 | 2 (one 22.9 ms scheduler outlier) | — | — |

All stepped runs build exactly what the synchronous call builds.

Map level, 7 maps (73 frames that furnished), quiet machine (load 4–8; runs4/L_mapbase, L_mapstep):

| Setup | Mean / p95 / p99 / max (ms) |
|---|---|
| Original dressers, one room per frame | 1.47 / 1.75 / 19.40 / 25.39 (the 20388 room) |
| New dressers, map unchanged | 0.46 / 1.03 / 4.34 / 4.49 |
| New dressers + ticket at 3 ms (77 frames) | 0.44 / 1.98 / 3.04 / 3.08 |

The game on autopilot, seed 20388, same quiet machine:
- Original (L_D1_orig): worst handoff frame 42.9 ms = chunk (16,10) 6.7 ms + room 1 of (16,10) 34.8 ms.
- New dressers + ticket (L_D3_ticket): that room appears as a 1.8 ms step inside a 21.5 ms frame. The worst handoff frame is frame 0 (35.3 ms, no map work), and the worst frame with map work is the map's own chunk (17,7) build, 20.2 ms.
- Without the ticket the room is still built in one frame: 14.8 ms quiet (runs3/D2), 78 ms under load (runs4/D2).
- Remaining 444–449 ms spikes at 9.8 s and 18.1 s have no map work in the frame, and the original dressers show them too. They are not dressing.
- The autopilot verdicts "ended by time" or "ended by caught", and the missing FMOD bank, are the same with the original dressers; this change does not cause them.

STEP API FOR THE MAP CHAT (Assets/Scripts/Office)
- `FrontRoomsDressJob FrontRoomsOfficeKit.BeginDress(Transform parent, Rect floorXZ, float ceilingHeight, int seed, Rect[] keepClear, Rect[] obstacles)` (OfficeKit.cs:450). The map path; the "office dressing" root is created right away.
- `FrontRoomsDressJob FrontRoomsOfficeKit.BeginDress(Transform, Rect, float, int, Rect[] keepClear)` (:456). Look-dev and RoomStream path; places its own columns.
- `FrontRoomsDressJob FrontRoomsFurniturePile.BeginBuild(Transform parent, Vector3 localCenter, float radius, float ceilingHeight, int seed)` (Pile.cs:211). The pile root is created only once the layout is solved.
- `FrontRoomsFurniturePile.PileJob BeginBuildPile(…, Tableau? force)` (:217). `.Root` holds the pile root once done.
- `bool FrontRoomsDressJob.Step(float budgetMs)` (DressJob.cs:89): true when done. `void Complete()` (:121) and `void Cancel()` (:130).
- Properties: Done, Cancelled, Created, ElapsedMs, Steps, MaxSolveUnitMs, MaxCreateUnitMs.
- `static void FrontRoomsDressJob.WarmUp()` (:180): call once from Prewarm.
- Dress/Build/BuildPile keep their signatures; they now run the same job to completion.

Rules:
- One open job per chunk. Do not begin the next room, or run its PlaceProps, until the current job is done.
- Use Complete for builds nobody watches (buildAllNow, RebuildChunk).
- Use Cancel when the chunk is dropped or rebuilt. A destroyed parent also cancels the job on the next Step.
- Budget is 3 ms on desktop and Editor. A smaller WebGL value goes only under `#if UNITY_WEBGL && !UNITY_EDITOR` (Red's rule).

PROMOTE (copy-relative; source scratchpad/dress_hitch/, target Frontrooms3D/)
Precondition: main's three dresser files must still have the shas above; otherwise merge as a diff.
Runtime files:
1. final/FrontRoomsDressJob.cs → Assets/Scripts/Office/FrontRoomsDressJob.cs
2. final/FrontRoomsDressJob.cs.meta → Assets/Scripts/Office/FrontRoomsDressJob.cs.meta (new GUID a58cc79c163934fbebbd902e9a67eeba, not used anywhere in main)
3. final/FrontRoomsOfficeKit.cs → Assets/Scripts/Office/FrontRoomsOfficeKit.cs (keep main's .meta)
4. final/FrontRoomsFurniturePile.cs → Assets/Scripts/Office/FrontRoomsFurniturePile.cs (keep main's .meta)
5. final/FrontRoomsKitLibrary.cs → Assets/Scripts/Office/FrontRoomsKitLibrary.cs (keep main's .meta)

Then the map chat applies its own ticket (we never edit MapWorld):
- map_ticket3/FrontRoomsMapWorld.dress_step.patch
- map_ticket3/FrontRoomsDressMapStepTest.cs + .meta (GUID a9b77e1a91c474764b97b11d1d90c64d), suggested target Assets/Editor/FrontRoomsMap/

Optional, editor only: final/Editor/FrontRoomsDressPerfTest.cs + .meta (GUID 1723927f2e84a4a43b84b4bdcd135eaf) → Assets/Editor/Tests/. Never promote FrontRoomsDressVerify.cs or DressPerfOriginal/: they need the *Orig copies.

EVIDENCE
- scratchpad/dress_hitch/runs3/ (analysis.txt, A2/A2w verify, A3 after, A4 ab, A5 steps, A6/B3/C4 cold, C2 mapstep, D*)
- scratchpad/dress_hitch/runs4/ (analysis.txt, all.out, low.out, A_ab_full.json, A_steps_full.json, {B,A,C}_*-tests json, B_/L_mapbase.txt, C_/L_mapstep.txt, D*/L_D* report.json)
- scratchpad/dress_hitch/sync4/ (main d610d3a)
- The clone proj_rs is left with final/ dressers and main's map.