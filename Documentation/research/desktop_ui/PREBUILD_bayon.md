# Prebuild: the Bayon "·" fallback (FIX_CARD A4)

- **Stage:** 游戏视觉, workflow prebuild-integrations, Bayon build stage. It ran 2026-10-22 16:47–17:0x on account B (session local_a9a1dce2), after account switch #4.
- **What it continues:** run 2 (account A) wrote the harness, a first package script and a v1 render set, then died at 16:17 while it waited for a Unity slot. Its v1 renders had a harness bug (menu ink), so they are kept as history only: `W/bayon_fb/out_v1_menu_ink_bug/`.
- **Spec:** `FIX_CARD_VL217.md` A4. Red's words (总控 chat, 15:02): "开始把所有能够integrate到主build里的所有已经完成的资产都开始integrate，不用等alpha了".
- **Condition:** none. It lands as soon as it is verified. It also unblocks 交互设计's X2.
- **Status: READY, LANDED 17:44** by the 游戏视觉 session (LANDINGS 17:42 CLAIM / 17:44 LANDED, MERGED.md; backup `W/backup/bayon_fallback_2026-10-22_174254`). The verify stage (§6) confirmed it: main's `.meta` is c68a3a3b and the dry run prints ALREADY APPLIED.

## 1. What the game uses (checked before building)

**The text system.** The game's text is legacy uGUI `Text` with `UnityEngine.Font` assets. There is no TMP and no UI Toolkit text. So the fallback goes in the `TrueTypeFontImporter` of `Assets/Resources/Fonts/Bayon-Regular.ttf`, which stores it as `Font.m_FallbackFonts`.

**Where Bayon is used:**
- `FrontRooms3DGame.cs`, through `UiFont()`: every Text whose name contains "Threat", "Key" or "Menu text". That covers the crosshair "Key prompt" (Bayon 20 on desktop, 17 on the phone HUD), the KeyLabel, the threat state, and the menu overlay. It also covers `mobilePauseTitle` and `mobileCaughtTitle`.
- `Input/FrontRoomsTouchControlsView.cs`: the 17 pt labels, legends, settings rows, PAUSED, CAUGHT and the subtitle.

**What Bayon is missing.** The font has 203 code points (cmap parsed from the file). It has no U+00B7 "·", no U+2039/U+203A "‹ ›", no dashes, no curly quote, no ellipsis and no degree sign. IBM Plex Mono Regular has all of them.

**Strings that reach a Bayon Text with a glyph Bayon lacks** (main at aca6240b + tree, Game fc76109d):
- **"·":**
  - the map prompts `E  ·  TAKE KEY`, `OPEN DOOR`, `SHUT DOOR`, `LOCKED  ·  NEEDS THIS ZONE'S KEY` and `HOLD E  ·  BREAK GLASS` (MapWorld `Describe`);
  - `TAP E  ·  BREAK GLASS`;
  - the inspect prompts and hints (`E  ·  READ`, `LOOK`, `READ PLAN`, `READ SIGN`, `EXAMINE KEY`, `BACK`, `SCROLL`/`PINCH  ·  CLOSER`, `MOUSE`/`DRAG  ·  TURN`);
  - the phone forms (`DisplayPrompt`: `HOLD  ·  …`, `TAP  ·  …`);
  - the touch legends and the `HAPTICS  ·  …` rows;
  - the handheld pause copy (`<size=11>` in `PauseText()`).
- **"‹ ›":** the touch settings chevrons (`FrontRoomsTouchControlsView` :996–997). This is a **declared side effect** (§4).
- **Not affected:**
  - the desktop settings panel's `›` and arrows: "Display settings text" is Source Serif;
  - `PauseMeta`: Plex Mono;
  - `mobilePauseRows`: Plex Mono.

**Before the fix**, the editor and the desktop players draw "·" from an OS fallback face: a 3 × 3 px dot at 20 px, centred on the caps. WebGL has no OS fonts, so a dot there depends on what the build can reach. With the fix, every player uses the font's own fallback reference.

## 2. What was built

**The change is 1 path, a `.meta` written by Unity itself.** The clone harness set `TrueTypeFontImporter.fontNames = { "Bayon", "IBM Plex Mono" }` and `fontReferences = { IBMPlexMono-Regular.ttf }`, then called `SaveAndReimport`:

```
   fontNames:
   - Bayon
-  fallbackFontReferences: []
+  - IBM Plex Mono
+  fallbackFontReferences:
+  - {fileID: 12800000, guid: f715d487a32b84693a997275f44b01bf, type: 3}
```

- The `.meta` goes from `e37d004a` to `c68a3a3b`.
- Bayon's GUID `e3aec9a2…` is kept. Every other importer field is unchanged.
- The fallback GUID is main's `IBMPlexMono-Regular.ttf.meta` (`f715d487`, includeFontData 1).

**What it does not do:**
- no code and no platform branch;
- no new bytes in any build: Plex Mono already ships, in Resources;
- the font data, Plex Mono's `.meta`, `FrontRoomsMap/*`, `FrontRooms3DGame*.cs`, `Input/*`, ProjectSettings and Packages are untouched.

Legacy `Text` cannot mix faces inline, so no string is split (FIX_CARD A4).

**The package** (convention v2), in `W/apply/`:
- `bayon_fallback.sh` (sha1 4601be62): the dry run checks bases, kept files, payload sha1, Bayon's GUID and that the fallback GUID equals main's Plex GUID; `--apply` backs up, writes, then checks the manifest;
- `bayon_fallback_bases.txt` (19362945): `e37d004a  Assets/Resources/Fonts/Bayon-Regular.ttf.meta`, recorded 16:55 on main aca6240b + tree (Game fc76109d);
- `bayon_fallback_keep.txt` (ea4a2bab):
  - `Bayon-Regular.ttf` 66fc2fcf;
  - `IBMPlexMono-Regular.ttf` 2b7c2393;
  - `IBMPlexMono-Regular.ttf.meta` db843035;
- `bayon_fallback_expected.txt` (a64fd757): `c68a3a3b  Assets/Resources/Fonts/Bayon-Regular.ttf.meta`;
- `bayon_fallback_payload/Assets/Resources/Fonts/Bayon-Regular.ttf.meta` (c68a3a3b, LF only);
- `bayon_fallback_base/…` (e37d004a = main);
- `bayon_fallback.diff` (76384984).

**Landing.** Apply with `bash /Users/redwang/FrontRoomsVisualWork/apply/bayon_fallback.sh --apply`. If Unity is open on main, it reimports the font once, in seconds. The fresh-session and in-session renders match exactly (§3), so no restart is needed.
- Roslyn has nothing to compile: no `.cs` is touched.

**Where the work is:**
- the clone `W/proj_bayon_fb`: main aca6240b + tree, re-synced 16:51 with mkclone's rsync; the harness `Assets/Editor/BayonFb/` (clone-only) was kept;
- the harness `FrontRoomsBayonFallbackCheck.cs` (62801f84): ASCII, no tuples;
- the analyser `W/bayon_fb/analyze.py` and the plates `W/bayon_fb/make_vl.py`;
- the renders `W/bayon_fb/out/` and the logs `W/bayon_fb/logs/`.

## 3. Checks

**Unity runs:** 6000.3.10f1, URP 17.3, macOS target, legacy uGUI `Text` exactly as the game builds it, URP camera, no AA.

| Run | Time | Duration | 1-minute load |
|---|---|---|---|
| before + apply in session | 16:52 | 50 s | 199–195 |
| after, fresh session | 16:53 | 56 s | 196–225 |
| mini import | 16:55 | 59 s | 209–231 |

Every run had 0 `error CS`. All three are UNDER LOAD; the durations are not timing data.

**The cases:** 108 strings, each rendered twice per pass (determinism).
- **53 game strings with "·"** (corrected by the verify stage: the build stage wrote 60, which also counted 7 probe strings that contain a dot; `analysis.json` `n_dot_cases` 60 counts them too):
  - desktop Bayon 20 at HUD scale 1 (1080) and 1.333 (1440): 19 prompts each, including the decided `LOCKED  ·  NEEDS STAIR KEY`, `E  ·  UNLOCK` and `E  ·  READ SCREEN`, plus the HUD-key state labels `E·1` and `L3·D`;
  - the phone HUD, Bayon 17 on the 874 × 402 canvas at 2.929 (iPhone 15 Pro): 6;
  - the touch layer, Bayon 17 at point scale 3: 7;
  - the handheld pause copy, Bayon 11: 2.
- **46 controls without "·":** every ASCII glyph and « » at 20 (two scales), the menu titles at 88 bold, the stats at 24, the legend at 13, and 10 touch strings at 17 × 3 plus the 48 pt titles.
- **Probes:**
  - the chevrons;
  - the dot alone;
  - `→×§·‹` at 120 px, through Bayon's fallback and through the Plex Mono asset itself.

| # | Check | Result | Verdict |
|---|---|---|---|
| 1 | The import: the importer and `Font.m_FallbackFonts` | `[IBMPlexMono-Regular.ttf (f715d487…)]`; `GetDependencies(Bayon)` = Plex Mono; the `.meta` was written by Unity and is byte-identical to run 2's | PASS |
| 2 | Every "·" string draws its dot | 53 / 53 game strings: the expected number of dots was found before and after the change (the 7 probes are judged in row 4, not here) | PASS |
| 3 | The letters before the dot are untouched (left-aligned harness) | 53 / 53: the first changed column is at the dot | PASS |
| 4 | The dot is Plex Mono's own glyph | Bitmap identical to the Plex Mono asset's dot at 20 px, at 17 × 3 and at 120 px (max \|Δ\| 0). Only the vertical offset differs, because each Text uses its primary font's line metrics | PASS |
| 5 | **No other glyph changed** | 46 / 46 controls: **0 px** differ (max over every control: 0) | PASS |
| 6 | Determinism and reimport | Two renders per pass: 0 px. Fresh session vs in-session after `SaveAndReimport`: 0 px on all 108 | PASS |
| 7 | **Import on every target** (import only, no build) | Fonts-only scratch project `W/bayon_fb/mini`: main's two TTFs and Plex `.meta` (byte copies) plus the payload `.meta`. It started on WebGL, then switched to iOS, StandaloneWindows64, StandaloneOSX and back to WebGL. On every target: the same importer fields, `m_FallbackFonts` = Plex Mono (f715d487), font data 55,132 B (Bayon) and 135,580 B (Plex), the build dependency on Plex Mono, and "·" advance 12 at 20 px (Plex). `TrueTypeFontImporter` has no per-platform settings | PASS |
| 8 | Text boxes still fit | The widest "·" strings: 245 px at desktop 20 in the 720 box; 209.6 units on the phone HUD in the 360 box; 163.7 pt in the 240 pt touch legend; 136 pt in the 337 pt settings column | PASS |

**What changes on screen:**
- **The dot:** 4 × 4 px at Bayon 20 (5 × 4 at 1440, 8 × 8 on the phone HUD), where the OS-fallback dot was 3 × 3.
- **Its height:** its centre sits at 0.39–0.43 of the cap height. That is Plex Mono's own placement on the shared baseline; the OS dot sat at 0.50.
- **Width:** each dot is wider by the Plex advance, so the string grows:
  - +5 px per dot at desktop 20 (+5.25 units at 1440);
  - +4.44 units on the phone HUD;
  - +4.67 pt on the touch layer;
  - +5.46 units at 11 for two dots.
- **Where the words move on screen** (verify stage): the harness renders left-aligned, so there only the text after the dot moves. In the game:
  - the crosshair prompt (`Key prompt`, desktop and phone HUD) is `MiddleCenter`, so a one-dot prompt re-centres: the words before the dot move left and the words after it move right by half the growth each (2.5 px at desktop 20, about 2.2 units on the phone HUD). The phone USE glyph follows, because it is placed from `preferredWidth` (`UpdateMobilePromptGlyph`);
  - the touch legend rows are centred as a whole (`LayoutLegendRow`), so a row shifts by half its growth; each entry is left-aligned inside it;
  - the touch settings rows (`HAPTICS  ·  …`) are left-aligned: only the text after the dot moves;
  - the handheld pause copy is centred inside `Menu text`.

**Images (Shot format):**
- VL388 `research/desktop_ui/images/vl388_bayon_dot_prompts.jpg` (d0f87998): every "·" string, before | after, plus the dot zoomed;
- VL389 `research/desktop_ui/images/vl389_bayon_controls_import.jpg` (c815e1aa): the controls before | after | changed pixels, the 120 px identity probe, the chevrons, and the import table per target.

The slide specs are in `W/vl_pending/prebuild_bayon.md`.

## 4. Notes for other lines (in this report only; no message was sent)

**平面视觉 (FIX_CARD D):**
- The fallback dot sits at Plex Mono's own height, 0.39–0.43 of the caps, not their centre. It is Plex Mono Regular, as specified; the Shot format sets Plex Mono Medium. Check it in the desktop capture after landing (A4).
- Digits stay out of Bayon (A4/A7). The fallback does not change 1 or 0: they are in Bayon's cmap.

**交互设计 (X2 is unblocked once this lands):**
- Side effect 1: the touch settings chevrons ‹ › now draw in Plex Mono. Their advance goes from 5.67 to 10.33 pt, inside their 12 pt box.
- Side effect 2: the legends use `preferredWidth`, so each legend entry with a "·" is 4.67 pt wider.

**关卡设计:** nothing to change. The map's prompt strings keep the two spaces round "·".

**WebGL track:** nothing WebGL-specific. The fallback is a reference inside the Font asset. Plex Mono is already in Resources, so the build gains no bytes.

## 5. Open items

- **Land it:** done 17:44 (the 游戏视觉 session). The dry run now prints ALREADY APPLIED.
- **Place VL388–VL389:** they are not in the log or Figma yet.
- **Not tested here:**
  - the dot in a real WebGL or iOS player: the brief allows an import check only, and no build was made;
  - whether a Mac or Windows player prefers an OS-installed "IBM Plex Mono" (matched through `fontNames`) over the embedded reference. Both are Plex Mono, so the glyph is the same. The editor evidence points to the embedded reference: this Mac has Plex Mono installed as a CFF `.otf` of another version (1,047 code points; the project's `.ttf` has `glyf` outlines and 930), yet the 120 px fallback bitmap is identical to the project asset's (§6).

## 6. Verify stage (2026-10-22 17:02–17:5x, account B, session local_a9a1dce2)

The stage tried to refute the build. Nothing blocking was found.

**Checked again by the verify stage:**
- **Package:** the dry run was OK at 17:02 on main aca6240b + tree. Every sha1 in the build report matches: script 4601be62, bases 19362945, keep ea4a2bab, expected a64fd757, diff 76384984 (equal to a fresh `diff -u` of base and payload), base copy e37d004a, payload c68a3a3b (LF, ends in a newline).
  - `--apply` was tested on a scratch copy (`W/bayon_fb/verify_apply_1707/`): MANIFEST OK, the backup is e37d004a, then the dry run printed ALREADY APPLIED.
  - Run 2's Unity-written `.meta` and this run's are the same (c68a3a3b): two separate sessions gave the same bytes.
- **Text system:** legacy uGUI `Text` + `UnityEngine.Font` only. The one `UIDocument` is the vector logo, with no text, and there is no TMP. Bayon is loaded only through `Resources.Load<Font>("Fonts/Bayon-Regular")`, by the game and by the touch view. The project has one Plex Mono file.
- **Coverage:** every runtime "·" literal that reaches a Bayon `Text` is in the 53 game strings. That is the map's `Describe`, `TAP E`, the inspect catalog, `DisplayPrompt`, the touch legends and haptics rows, and the handheld pause copy. `PauseMeta`, `mobilePauseRows`, the captions and the desktop settings panel are not Bayon. The settings panel's `› ↑ ↓ ← →` are in Source Serif and are unaffected.
- **Cmap:** parsed again by an independent script. Bayon has 203 code points and lacks U+00B7, U+2039 and U+203A (and → × § ’ —). Plex Mono `.ttf` has all of them.
- **Renders:** an independent pixel diff of the 108 × 5 build-stage renders:
  - the 46 controls change 0 px;
  - both determinism pairs, and fresh vs in-session, match on all 108;
  - the probes are bitmap-identical to the Plex asset at 120, 20 and 17 × 3;
  - the Bayon "before" glyphs differ from Plex Mono, because they come from the OS fallback.
- **Fit:** the boxes hold, and the phone USE glyph follows the text through `preferredWidth`. No editor test or playtest driver checks prompt or legend widths: the only prompt checks compare strings.

**Corrected by the verify stage** (small, docs only):
1. **"60 / 60" is 53 / 53.** The build stage counted 60 "·" strings, but its own breakdown (19 + 19 + 6 + 7 + 2) is 53 game strings. The other 7 are probes, and 4 of those have no "dot found" by the finder. All 53 game strings pass rows 2 and 3. Fixed in §3 and in `W/vl_pending/prebuild_bayon.md` VL388. Still to fix: the LANDINGS 17:42 CLAIM and MERGED.md row quote "60/60 dots". Those entries belong to the 游戏视觉 session, so the verify stage left them as they are.
2. **On-screen shift.** "The text after each dot moves right" is true only of the left-aligned harness. The crosshair prompt is centred, so it re-centres (§3, "Where the words move on screen"). VL388's header sentence describes the harness renders and was not re-framed.

**Not re-run:** the verify stage's own Unity pass (restore the base `.meta` in the clone, then before + in-session apply, out `W/bayon_fb/out_verify`) waited 17:08–17:52 for a visual-line slot. Two or three jobs held the slots the whole time (load 55–386), so it was stopped before it started. The clone is untouched (`.meta` c68a3a3b), and the wait is logged in `W/bayon_fb/logs/verify_before_apply.wait`. The verdict rests on the build stage's frames and logs, which the verify stage re-analysed on its own.

## 7. Verify stage re-run (2026-10-22 20:03–20:1x, account B, session local_a9a1dce2)

This re-run did the Unity pass that §6 could not start. It found nothing blocking. The package is landed and correct.

**Main, as landed:**
- `Bayon-Regular.ttf.meta` is c68a3a3b, byte-identical to the payload. The backup `W/backup/bayon_fallback_2026-10-22_174254` holds the base e37d004a.
- The dry run at 20:07 prints ALREADY APPLIED: the 3 kept files are unchanged, Bayon's GUID is kept, and the fallback GUID is main's Plex Mono.
- Every package sha1 in §2 still matches.
- Main has moved since the clone (Game fc76109d → a2e212ef, Mobile, MapWorld). The only "·" string change is the shorter handheld pause copy (`SOCKET · SPRINT` removed), and that copy stays hidden on handhelds anyway. The new desktop menu lines (`Menu CTA`, `Menu line 2/3`: `ESC  RESUME`, `R  RESTART`, `R AGAIN TO RESTART`, `O  SETTINGS`, `R  TRY AGAIN`) are Bayon 20 with no glyph Bayon lacks.
- An independent cmap scan of the Bayon-feeding scripts (Game*, the touch view, MapWorld, the inspect catalog, the exit planner and the stairwell) finds these glyphs Bayon lacks: "·" and the touch chevrons ‹ › (as declared). The others (° – ± × § → and the settings panel's › ↑ ↓ ← →) appear only in log strings or in the Source Serif settings panel.

**Unity pass** in `W/proj_bayon_fb`, out `W/bayon_fb/out_verify`, runner `W/bayon_fb/run_unity_v.sh`:
- the base `.meta` was restored, then before ×2, `SetFallback` and the in-session render ran (20:05:22, 14 s, load 152→131);
- then a fresh-session after ×2 ran (20:05:36, 16 s, load 131→112).
- Both runs had 0 `error CS` and were UNDER LOAD.
- Unity wrote c68a3a3b again. That is the third separate session to write the same bytes.

**Analysis** by an independent script (`W/bayon_fb/vdiff.py`, not `analyze.py`; the cmap scan is `W/bayon_fb/cmap.py`):

| Check | Result |
|---|---|
| Import dumps (before, after apply, fresh after) | identical to the build stage's, artifact hash included |
| Renders vs the build stage | before, after and in-session: 0 of 108 differ |
| Determinism | a/b, and in-session vs fresh: 0 of 108 differ |
| Controls (46) | 0 px changed |
| Game "·" strings (53) | the dot count before and after equals the text's; the first changed column comes after the last glyph before the dot. The detector's 2 flags (`D20q_p03`, `P17_p02`) are the apostrophe of ZONE'S (a small blob at cap top, y 38–45), not a dot |
| Dot identity | the Bayon fallback dot is bitmap-identical to the Plex asset's at 20, at 17 × 3, at 120 and inside `E  ·  TAKE KEY` |
| Growth (last ink column) | +5 px desktop 20, +7 px at 1440 (5.25 units), +13 px phone HUD (4.44 units), +14 px touch (4.67 pt), +16 px for two dots at 11 (5.46 units), as §3 says |

**Closed since §5:** 平面视觉 accepted the dot height on VL388 (`FIX_CARD_VL217.md`, the A4 line under D). The FIX_CARD D check is done.

**Still open:**
- the 游戏视觉 session places VL388–VL389 (not in the log or Figma at 20:0x);
- `MERGED.md` (17:44) and the LANDINGS 17:42 CLAIM still say "60/60 dots" for 53 / 53. Those are the session's own records, so this stage left them as they are;
- no real WebGL or iOS player was tested (import only, as the brief says).
