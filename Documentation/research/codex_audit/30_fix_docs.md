# Codex audit · 30 · Fix: docs group

Date: 2026-10-08, 19:30–19:50 PDT. Fix group: docs. Main HEAD `bc0aa79` (2026-10-08 01:47) plus Red's working tree.

**What this stage did:**
- It edited one tracked doc, `Documentation/VISUAL_CHAT_TASKS.md` (working tree, not committed), and added a short "superseded" note to `codex_audit/10_review_docs.md` §4.
- It did not open Unity, run code, commit, push, move or delete anything.
- No images were made, so there is no VL slide.

**How:**
- One script (`scratchpad/docsfix/apply_docs_fix.py`) made 16 edits. Each edit matched an exact text anchor that had to occur exactly once; if any anchor failed, nothing would be written. No edit used line numbers.
- The script re-checked the file's sha1 just before writing.
  - Base sha1: `8d364755…`. It differs from HEAD because of other sessions' uncommitted 10-08 lines in D1.6 and DB1. Another session saved the file twice while this stage ran.
  - After: `04801619…`.
- A preview copy was checked first. Every touched table row keeps its column count.
- The other sessions' 10-08 text is still present, byte for byte: D1.6 door shots incl. the `PoseHeld` line, the DB1 C rework, Q13, Q16, K1, N1 and the merge-rule bullets.

**Current main differs from the 10-03 findings.** Four facts changed the replacement text:
1. G14's `FRGlassRTPrepass` hook landed on 10-04 (Codex `9eddc35`, `bf2e883`, `70644f0`). G14 now traces window glass (VL116), so "traces nothing" is no longer true.
2. Facade r6 replaced Codex's r3 copy on 10-07 (Red approved). Its header is now correct: "logs every failure; the map's trims stay".
3. The 10-03 editor log has rotated away. Today's `Editor.log` and `Editor-prev.log` show 0 DropChunk errors and 0 ZBinningJob errors.
4. Nothing else in these tracks changed after `75cfdff`:
   - no commit to `Rendering/GlassRT`, so there is no fail-closed guard and no F8 patch in main;
   - no production `FrontRoomsGlassBreakable`;
   - no recapture of `Refl_*.exr`;
   - no interactables-kit final merged over Codex's 53 D1 pre-renders.

---

## 1. Finding → change

| Finding | Row(s) changed (anchor) | Change | Handoff |
|---|---|---|---|
| **DOC-D7** | codex-audit bullet "Known so far from Red's Editor.log" (§000) | **10 / 29 → 85 / 253.** The new text cites the session (10-03 19:15–20:01), the log lines 11,173–39,235, and the fact that the log has rotated. It names the cause: the ZBinningJob errors are a knock-on of the DropChunk throw during URP's record (`10_review_rt.md` §2). It records 0 of either in every later session (10-03 after the recompile, 10-05, 10-07, 10-08). It also says any other G14 exception would still freeze frames until F1 lands. | none |
| **DOC-D2** | bullet "Clone merge audit 2026-10-03" | **Rewritten as a nested list.** (1) The metas were written by Codex with python uuid4 (`75e18a03…`, `af825826…`), not by Unity. They are valid, and Red committed Unity's whitespace rewrite in `03c43ed`. (2) The scan covered 8 clones, not 6, matched names only and skipped `win_fix/`. (3) Two owed pieces: G14's G-1/G-2/G-3 shader hunks (**landed 10-04**) and the window map contract R1/R2 (+ T) (**still owed**, owner the map chat). (4) The other unique files stay out of runtime. No byte counts are given. | glass-rt-track (done for the hunks); window-landing → map chat (R1/R2/T) |
| **DOC-D1** + **F7** | G14 row | **Whole row replaced.** It puts back Red's 09:5x request and the original scope (P3 Windows DX12, P4 fracture, P1b = G14b) from `7320ed1`, and the owner cell Codex dropped. New facts: <ul><li>Codex promoted it at 19:10 from an implement stage that was interrupted and never verified.</li><li>It is **live by default since `9eddc35`** (VL086 → VL116, 71,919 px ≥ 8/255).</li><li>The camera opt-in comes from `FrontRoomsPostStack.ConfigureCamera`, which covers the title and map cameras and the edit-mode eyes. Only the Play camera traces.</li><li>The MSL compiles at runtime (D4). The Xcode Metal Toolchain is missing but not needed (do not download it). The dylib sha1 is `56a35164…`, equal to a fresh build. Red's editor loaded it on 10-08.</li><li>Open: P0/P1 never run; proof 154/175 with 1 open fail (VL119); F1 (patch ready); F3 2.8 ms p50 / 20.6 ms max against 0.3 ms (VL117); F8 (patch ready); F4–F6; VL107.</li></ul>Status: "PROMOTED (Codex, WIP); LIVE by default since 10-04; acceptance pending → glass-rt-track". It does **not** say the toolchain claim is false (the toolchain really is missing; it is just not needed), and it does **not** say "the map takes no part" (`FrontRoomsModulePreview` calls `ConfigureCamera`). | glass-rt-track: F1/F8 patches, P0/P1 acceptance, F3, F4–F6. When it merges, it rewrites the row. |
| **DOC-D1** | W1.5 last sentence | **Kept** the pre-Codex plan sentence "Ray tracing (G14) targets the visible slab"; it is the visual chat's own text, not a Codex error. Added an update: from 10-03 19:10 to 10-04 10:05 the slab was registered but traced 0 px (VL086); since `9eddc35` it is traced (VL116), but too faintly for the intact pane to read (VL107 FAIL). | glass-rt-track / glass track (VL107) |
| **DOC-D4** | W1.4 description + start of the status cell | **Description:** added a correction. Codex promoted the r3 fix-stage facade (17:41; compiled 17:54, never run) without Red's review, plus a direct `DressWindow` call that ignores the result and logs every failure. So the map's trims are still built under every kit frame (hidden by the casing). The spec's "trims dropped for kit windows" needs contract T, which Red decides. Window-landing has tested main since; VL107, an autopilot re-run and the G4 rebuild are open. **Status:** "PROMOTED; runtime acceptance pending" → "PROMOTED BY CODEX 2026-10-03 (r3, unreviewed); facade replaced by r6 (below); map trims still built until contract R1/R2 + T". The main session's "MERGED 2026-10-07 19:4x … r6" note after it is unchanged. "UNTESTED" is not used, because r4–r6 were tested on main. | window-landing → map chat (R1/R2; T needs Red) |
| **DOC-D4** | Facade header `FrontRoomsInteractableKit.Window.cs` | **No edit needed.** r6 (merged 10-07, `07dc067e`) already rewrote the header. It now says "main as Codex left it: … logs every failure; the map's trims stay", and it describes R1 (logs once) and T correctly. | — |
| **DOC-D4/D5** | W1.7 status | Appended: the kit frames, interim slab, glass shader and materials have been live since 19:10, before D1.5. Red's 18:0x merge-as-you-go order covers assets with defaults, and Red approved r6 on 10-07. D1.5 still decides T, the blinds and the final G4 frames. | — |
| **DOC-D4/D5** | W1 intro "**Nothing is in the game yet.** …" | Replaced with "Overtaken 2026-10-03 19:10": the frames, stop band and interim slab are in the game (see W1.4). The map's trims are still built under the frames. The 30 mm pane cube stays as the collider, with its renderer hidden. | — |
| **DOC-D5** | D1 intro "Nothing about doors or windows lands … before Red confirms." | **Red's rule kept.** Added a note: overtaken for windows; 53 D1 models are in main as unused files; no door model is live. | — |
| **DOC-D5** | D1.4 | Appended the Codex note. 53 pre-renders were copied at 19:10 (106 FBX/JSON + 106 Unity-made metas), while G2/G3 were unfinished. No script spawns them, but `KitLibrary.AllNames` lists them, so `Prewarm` loads them and the Level Designer picker shows them (`FrontRoomsModuleEditing.cs:35`). The verified finals merge over them and keep main's metas. Status "RUNNING" → "RUNNING (interactables-kit)". | interactables-kit |
| **DOC-D5** (+ D6) | G1 | **Restored from `7320ed1`:**<ul><li>the reflectance numbers (8 / 9.5 / 13.7 / 35.8 % vs real 8.2 / 10.9 / 15.7 / 38.5 %);</li><li>the face-on caveat, word for word, with today's evidence that it still holds (VL107: 2.9 / 2.8 luma, bar 12);</li><li>**Red's 13:3x decision**, word for word, marked "partly overtaken" by the 18:0x order and Codex's 19:10 promotion.</li></ul>**Added:**<ul><li>the D6 owed steps (`30_final.md` §4). Step 1 is a no-op on today's main (`10_review_glass-look.md` §1 #7). The cubes are stale (F2).</li><li>Red's look sign-off (F3).</li></ul>Codex's correct file-list and hook sentences are kept. Status → "PROMOTED (Codex 19:10); look sign-off + runtime acceptance pending". | visual (glass owner): cube recapture |
| **DOC-D5** | G2, G3, G4 | "RUNNING (glass-track)" → "DONE (proj_glass 13:1x); promoted 19:10 by Codex (8ef5b64), see G1". G4 adds "Red's look sign-off pending". These rows were already stale at `7320ed1`, so this is not called a Codex error. | — |
| **DOC-D5** | G6 | "RUNNING … Stub … IN MAIN" → DONE + promoted. The real `SetZoneReflection` is live, and `FrontRoomsZoneReflection.cs`, its driver and the 4 cubes are in main. The cubes are stale (see G1). | visual (glass owner) |
| **DOC-D5** | G10 | "RUNNING (glass-track verify)" → "Run 3 DONE in proj_glass (clone evidence); not re-run on main; next run after the cube recapture, with the map's stop and frame in". It is not called "promoted". | visual (glass owner) |
| **DOC-D5** | GD3 | **Restored** "The glass lands together with W1", with the note that W1 is already live, so GD3 hooks onto W1.4's slab and root. **Restored** the owner cell "visual". Status "BLOCKED FOR PROMOTION; lab-only" → "RUNNING (workflow glass-break-build); no production `FrontRoomsGlassBreakable` yet, so nothing is promoted". Codex's correct "Promotion audit" sentence is kept. Added: the generator package `W/apply/gd3_generator.sh` is READY and not applied. | glass-break-build |

## 2. Left alone on purpose

- **N1 row.** DOC-D3 is not in this group. Red decided on 10-03 at 22:1x to keep V5 ON (`RED_DECISIONS.md`), and other sessions have updated the row since.
- **D1.6.** Another session is editing it right now (map chat door shots, 10-08). Its "after D1.5" gate is still right for doors, and the W1.7 note covers windows.
- **The codex-audit bullet "Output: … 20_findings.md".** That file was never written. Readers already fall back to `00_main_state.md` + `10_review_*.md`, as the recovery note says. Its pointer is not a Codex claim.
- **W1.6 / R10.** Codex's 10-04 `04c3a6a` text there was later replaced by 平面视觉's 10-07 text.

## 3. Decisions for Red (not acted on)

None of this group's findings is owned by red-decision. The edited rows point to these open calls. Each has a recommended default; none is applied:

| Decision | Where | Recommended default |
|---|---|---|
| Keep G14 on by default before its acceptance runs | G14; `10_review_glass-look.md` F5b | Keep it ON. It follows the highest-spec order, and turning it off changes the desktop look. glass-rt-track lands F1 (the fail-closed guard) and F8 first. |
| Map contract T (no map trims under kit frames) | W1.4, W1.7; `window_landing/03_contract_map.md` | R1/R2 now. T together with D1.5. |
| Glass look sign-off (props, windows, title cubes) | G1, G4; `10_review_glass-look.md` F3 | Keep it as promoted (G10 run 3 verified). Revisit the Run room's +2.7 to +5.8 luma when the cubes are recaptured. |

## 4. Files

- `Documentation/VISUAL_CHAT_TASKS.md`: 16 anchor edits, +50 / −19 lines against HEAD together with the other sessions' uncommitted lines (`git diff --stat`).
- `Documentation/research/codex_audit/10_review_docs.md`: a one-paragraph "superseded" note at the top of §4.
- `Documentation/research/codex_audit/30_fix_docs.md`: this file.
- Scratch, outside the project: `apply_docs_fix.py`, `VISUAL_CHAT_TASKS.before.md` and the preview, in the session scratchpad `docsfix/`.
