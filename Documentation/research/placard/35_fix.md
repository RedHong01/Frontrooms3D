# 35 — Evacuation placard (Q16): fix stage

Status: **FIXED in a fresh private clone, 2026-10-08 00:1x–01:4x.** Art **v2**. The critic's 13 issues (2026-10-08) are each verified, then fixed, handed to their owner, or accepted with a reason.
- Clone: `/Users/redwang/FrontRoomsVisualWork/proj_placard_fix`, made with `W/tools/mkclone.sh` from main **22bb75f** (the old `proj_placard` was behind main: pre-Q1b wallpaper). 0 compile errors in Unity (OSXUniversal) and in Roslyn.
- Merge package: **`W/apply/placard_q16.sh`** (convention v2). Dry run against main: **DRY RUN OK**. Details in `41_promotion.md`.
- Map contract: `40_contract_map.md`. Request to 平面视觉: `42_request_graphic.md`.
- Red's project: only `Documentation/` was written (this folder, `VERIFICATION_LOG.md` rows VL203–209). Unity was never opened on Frontrooms3D.
- Where this file and `10_spec.md` / `20_build.md` / `30_render.md` differ, this file wins.

## 0. Summary

| # | Critic issue | Verdict | What changed | VL |
|---|---|---|---|---|
| 1 | Legend swatches in the wrong colourway | **CONFIRMED, handed to 平面视觉** | Candidate art built in W (clone only), shown in game beside the real wall. The approved v2 art ships unchanged | VL204 |
| 2 | Frame reads as dark, blotchy pewter | **FIXED** | New placard-only slot `Prop_AluminiumAnodised` | VL203 |
| 3 | Reduce flashing hides the legend in Failing cells | **FIXED** | Calm Failing read rebuilt: G 0.087 → **0.36**, steady | VL205 |
| 4 | k rests on mixed references | **FIXED** (k stays Red's call) | `PeakEmission` moved to `FrontRoomsPhosphor`; A4 measured at P1 and P2 with 2 lit neighbours: **1.44 / 1.38** | VL206 |
| 5 | Q16-1 not applied; reflection can be stripped | **CONTRACT sent** | Exact diff with `[Preserve]`, tested on main's current file; the mount logs once if the method is missing | — |
| 6 | WebGL path unbuilt | **FIXED, bigger than reported** | Unity kept the 2592 × 1676 textures **uncompressed on every platform (40.5 MB)**. Now 4096 × 2048: 16.8 MB desktop, 0.35 MB per map on WebGL | — |
| 7 | Merge package not ready | **FIXED** | One atomic v2 package, 31 paths, ordered | — |
| 8 | Frame covers the top of the cue band | **FIXED** | Centre 1.524 → **1.560 m** (every mount); 12 mm clear | VL208 |
| 9 | Lens on URP Lit; dark angled case uncaptured | **PASS** (captured) | No change: a standing player can never see a lamp mirrored over the legend | VL207 |
| 10 | Duplicate-placard race | **FIXED** | `Prepare` retires old mounts at once | — |
| 11 | P1 / P3 hidden from the doorway | **ACCEPTED** | None (the door shuts at 4 m; the footer says close it) | — |
| 12 | Period evidence missing; "copied" | **PARTLY FIXED** | Spec says "screen-printed"; photos still wait for Red's download approval | — |
| 13 | Small notes | **FIXED / RECORDED** | `ForceLevel` not serialized; shadows-off deviation and iOS numbers recorded | — |

Also found and fixed or reported (§10): running *Set up* in main would overwrite `FrontRoomsPost_Office.asset`; the kit importer can drop an FBX's material remap; main's map file is being edited right now.

## 1. Swatch colours (critic 1) — owner 平面视觉

**Verified.** v2 draws the four WP03 swatches in the reference palette of `Tools/print/patterns/hard_edge.py`. The game prints the same pattern through `LobbyPrint` (`FrontRoomsRenderSetup.cs`), via the print's density/cream encoding (`hard_edge.encode_table` → `print_tool.shade`):

| Ink | v2 swatch (reference) | What the wall shows in game |
|---|---|---|
| ground | #EEDDCC | #C8B871 |
| cream | #F4F0EC | #D9CA86 |
| grey (and the cue band) | #BBBEC0 | #AB9952 |
| pink | #DBAFA5 | #A4934E |
| slate | #9B9BAA | #83763B |
| deep | #887799 | #766A34 |

So the sheet shows a pink and grey wallpaper the player never sees. The shapes match; the colours do not.

**What this stage did (the art is 平面视觉's, so nothing shipped):**
- Rebuilt the v2 generator in W: `legend_swatches.py` + `placard.swift` reproduce the shipped v2 PNGs **byte for byte**.
- Built a **candidate**: the same generator with the six inks remapped as in the table (`fix_src/candidate_art/legend_swatches.diff`, 20 lines). Only the four swatches change. The glow mask is byte-identical; the print md5 is 5f71409a….
- Packed it like the real art and swapped it onto the placard's material instance in game (clone-only texture under `Assets/Editor/Audit/PlacardCand/`).
- Captured it beside the real Q1b hard-edge wall at P1 (facade) and P2 (map wall): the candidate swatches read as the wall around them; v2's read as another paper (VL204).
- Dark legibility with the candidate (own lamp off, 2 lit neighbours): P1 **1.37**, P2 **1.32** (v2: 1.44, 1.38; bar 1.30). The darker mustard bands under the glow cost about 0.06.

**Request:** `42_request_graphic.md`. When 平面视觉 delivers new art at the same size and paths, re-run `Tools/lookdev/pack_evac_plan.py`; no mesh, UV or code change. The three-view index carries a `pending` note to re-render then.

## 2. Calm Failing read (critic 3)

**Verified in game:** with Reduce flashing, a Failing cell showed G 0.087 (`render_data/a9_P2`), against 0.92 normal. Cause: the build gated the 3 s mean of the *lamp*; the mean lamp (about 0.47) sits near the gate's closed end (0.55).

**Tried first (critic's proposal):** a 3 s box mean of the *target* C·gate(Ls), low-passed 6 s. In a quick model (`fix_src/calm_sim.py.txt`) it gave **0.14**: half the normal path's time-average (0.27), still too faint, because a Failing lamp spends most of its time above the gate.

**Fix (in `FrontRoomsPhosphorCell.Tick`):**
- The normal path's target C·gate(Ls at τ 0.35 s) runs every frame, in every mode.
- A **peak follower** tracks it: instant attack, release τ **3 s**.
- That is low-passed with τ **8 s**. Under Reduce flashing, in a Failing cell, this is the target.
- The existing hold (1.5 s) and slew (0.5 /s) still apply on top.
- So with Reduce flashing the legend shows, steadily, about the strength the normal path reaches in its pulses. Every other mode and the normal path are unchanged.

**Results:**

| Run | Mean G (settled) | Largest 2 s swing | Max change |
|---|---|---|---|
| Simulation, calm (from 24 s, 66 s) | 0.353 | 0.047 (bar 0.05) | 0.50 /s |
| Simulation, normal | 0.312 (pulses to 0.80) | 0.796 | 2.0 /s |
| **In game P1**, calm, 2 lit neighbours | **0.358** (min 0.325) | **0.034** | 0.51 /s |
| **In game P2**, calm | **0.344** | **0.030** | 0.50 /s |
| In game P1, normal | 0.308 (pulses to 0.86) | 0.815 | 2.0 /s |
| Before this fix (render stage, P2) | 0.087 | 0.017 | 0.23 /s |

- The simulation is **14 / 14 PASS** (`fix_data/simulation.txt`). The calm Failing check gained a legibility bar: mean ≥ 0.7 × the normal path's mean. Its no-pulse window is measured once the 8 s low-pass has settled (from 24 s); before that it is a smooth fade-in, at most 0.04 /s.
- In the 1 m strip (VL205) you still see the lamp breathe: that is the map's lamp, not the glow.

## 3. Glow strength k (critic 4)

**Code:** `PeakEmission` now lives in `FrontRoomsPhosphor` as one absolute value for every surface printed in this ink (the placard now, the walls' driver if Q2 resumes). The comment was wrong (it said 12 %); it now gives both measured references. It is a `public static float` (not `const`) only so the clone's look-dev can sweep it. Value unchanged: **k = 0.158**.

**A4 measured with exactly 2 lit neighbours** (the spec's case; north + door side; own lamp Off 15 s; legend at 0.6 m, FOV 40, linear HDR, post off):

| Spot | G | Lit paper | Full glow / lit paper | Glyph / paper | Least k for 1.30 |
|---|---|---|---|---|---|
| P1 facade (seed 2) | 0.386 | 0.283 | 34 % | **1.437** | 0.123 |
| P2 map wall (seed 3) | 0.391 | 0.723 | 13.3 % | **1.375** | 0.137 |
| P2 with the candidate art | 0.391 | 0.723 | 13.3 % | 1.321 | 0.152 |

- The "% of the lit sheet" swings 2.6× between the two spots: P2's side wall faces its lamp, P1's facade takes it at a grazing angle. That is why the cap is now stated as an absolute k, not a percentage.
- **Recommendation: keep 0.158.** It is the smallest round value that clears 1.30 at both mounts with both arts. Red still picks k; the options are in `30_render.md` §9.

## 4. Frame finish (critic 2)

**Verified:** the shared `Prop_Aluminium` has albedo 0.72, macro dirt 0.06 and occlusion 0.8; its maps are flat, so the blotches were the world-space macro dirt.

**Fix:** a placard-only slot through the kit's normal slot path.
- `Tools/Blender/frontrooms_kit/assets/evac_placard_common.py`: `ALU = "Prop_AluminiumAnodised"` and `register_slot` for its preview colour (`kitlib.SLOTS` untouched).
- `FrontRoomsRenderSetup.SurfaceDefs`: `Prop_AluminiumAnodised` = FrontRooms/Surface, no texture, base (0.91, 0.92, 0.92), metallic 1, smoothness 0.62, occlusion 1, macro tone 0, macro dirt 0, mesh UV.
- Kits rebuilt in Blender (1,754 / 10 tris, all asserts pass); the FBX remaps to the new material by GUID. Every other aluminium kit keeps `Prop_Aluminium`.

**Result (VL203):** at 0.3 m in game the rails read clean satin silver at P1 and P2, with a smooth bullnose highlight and no blotches.

## 5. Height vs the cue band (critic 8)

- `FrontRoomsPlacard.CentreHeight` **1.524 → 1.560 m** for every mount (one constant). The facade carries the same Level 0 wallpaper as the map walls, so P1/P3 had the same overlap.
- Frame **1.403–1.717 m**. The turned or flattened cue band spans 1.184–1.391 m (`cue_states/states.py`, print blocks of 1,125 mm from the floor): **12 mm clear** (it hid the top 24 mm before).
- The top stays under NYC's 1.83 m sign cap; the bottom stays over the leaf handles (1.13–1.31 m).
- In game the frame's bottom edge sits just above the guide row's chevron apex at P2 and P1 (VL208). The cue driver needs no occluder rule.
- The guide-row height is still the print's assumption (blocks from the floor); the frames agree with it.

## 6. Textures and WebGL (critic 6) — a bigger finding

**Measured** (a throw-away Unity project in W, the same importer settings, Mac, iOS and WebGL targets; `fix_data/texture_format_probe.txt`):
- A **non-power-of-two texture with mips is never compressed** in Unity 6: 2592 × 1676 imported as **RGBA32 (23.2 MB) and RGB24 (17.4 MB)** on Mac and iOS, RGB24 on WebGL. 2048 × 1324 and 1024 × 664 (multiples of 4) stay uncompressed too. Without mips it compresses (BC7), and every power of two compresses.
- So the build's "BC7 + BC1, 8.7 MB" was really **40.5 MB uncompressed** on desktop, and as much on iOS.

**Fix:**
- `pack_evac_plan.py` now stretches the padded 2592 × 1676 sheet to **4096 × 2048** (Lanczos, in linear light; an upsample, so no detail is lost). The mesh UVs are relative, so nothing else changes. A round-trip check is printed (mean error 0.88 / 0.04 levels).
- Desktop (measured in the clone): `_A` **BC7**, 11.2 MB; `_E` **DXT1 (BC1)**, 5.6 MB; **16.8 MB** with mips.
- **WebGL tab only:** max size 1024 → **1024 × 512 DXT1, 0.35 MB per map with mips** (measured: the final packed pair imported for the WebGL target). The desktop tabs are untouched.
- iOS: no override; the power-of-two textures now compress to ASTC 4×4 / 6×6 (Unity's automatic choice).

**A10 status:** the format is verified by import (WebGL target). The lens is not spawned on WebGL (`#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW`), and the frame FBX has 2 submeshes, so 2 draws. The glow runs the same code; `TickFixturesNear` keeps `LampLevel` fresh on WebGL. **A WebGL build was not run** (cost); that stays open for the WebGL track.

**Note for other tracks (not checked here):** any other Resources texture that is non-power-of-two with mips is stored uncompressed too. Worth a look: the Q1b print slices (1024 × 1536).

## 7. Three-view (hand-off to 平面视觉)

- Rendered in the clone (W/tools `FrontRoomsThreeView` via `FrontRoomsPlacardLookdev -placardThreeView`), 4,000 px/m, alpha: `threeview/png/Kit_EvacPlacard_{top,front,side,persp}.png`, the same for the lens, and `manifest.json`.
- `threeview/index.json` follows the interactables format (`research/interactables/threeview/INDEX_FORMAT.md`): one sheet `evac_placard` (sheet scale 2000 px/m, 10 cm scale bar), two kits, swatches by the interactables rule (frame #e8ebeb, paper #e9dfd8, phosphor glow #c7f294 marked emissive, lens #000000 = clear).
- Review: silver rails, the sheet square in the sight, the 13.5 mm profile unchanged (VL209).

## 8. Smaller issues

- **10 · race:** `FrontRoomsPlacard.Prepare` now walks every child named `Placard mount`, calls the new `FrontRoomsPlacardMount.Retire()` (unsubscribes at once, never places), renames and deactivates it, then destroys it (`DestroyImmediate` in edit mode). `OnRunStarted` ignores a retired mount. Compiled; the double call cannot happen today, so it was not run.
- **11 · P1/P3 hidden from the doorway:** accepted. The door shuts by itself 4 m out, and P1 reads from there.
- **12 · period:** `10_spec.md` §1.2 now says screen-printed (the swatches and the overprint cannot be photocopied), matching `PRINTED 03/90`. Snap-frame photos still wait for Red's approval of `media_candidates.md` M1–M5; nothing was downloaded.
- **13 · shadows:** off on every placard renderer, the 467 mm frame included. SSAO gives the contact shading; the deviation from "small parts only" is recorded here.
- **13 · iOS:** the lens ships (Metal, as on Mac); textures now ASTC (§6). No iOS-only tier; that is the mobile track's call.
- **13 · `ForceLevel`:** now `[System.NonSerialized]`.

## 9. Lens in the dark (critic 9)

- **Geometry:** the legend spans 1.50–1.61 m; the standing eye is 1.62 m. A ceiling lamp can only mirror on a point *above* the eye, so it can never sit over the legend.
- The one lamp that can mirror at all is the north neighbour's, in the sheet's top band (1.68 m). That needs the eye **0.255 m** (P1) or **0.076 m** (P2) from the wall; the player capsule (radius 0.30 m) keeps the eye at least 0.30 m away.
- Rendered anyway (own lamp off, 2 lit neighbours), with and without the lens: the lens changes the frame by **1.6** (P1) and **3.4** (P2) 8-bit levels on average. At 0.8 m and 60° toward the lit side neighbour: **1.1** levels. The glow reads through it (VL207).
- The lens stays on URP Lit. Moving it to `FrontRooms/Glass` (`_PaneF0` 0.039) remains a follow-up once the codex audit clears the glass track.

## 10. Other findings

1. **Do not run *FrontRooms → Rendering → Set up* in main to make the placard materials.** In the clone it also rewrote `FrontRoomsPost_Office.asset` (5 values differ from main, e.g. `m_Value` −14 → −10 and −22 → −18; main's values look hand-tuned) and added the Q1b print defaults to about 70 materials. The package ships the three materials as files instead.
2. **The kit importer can drop a remap.** `FrontRoomsKitImporter` calls `RemoveRemap` when `LoadAssetAtPath` returns null, which happens if the FBX is imported before its material in the same refresh. The package writes the materials first and the apply script tells you to re-import the two FBX if they show grey. A `File.Exists` check in the importer would remove the risk (visual-owned; not changed here).
3. **Main's `FrontRoomsMapWorld.cs` is being edited right now** (uncommitted, 01:23: time-sliced dressing jobs). The Q16-1 diff still applies to it (with an offset) and compiles with the package (§5 of `40_contract_map.md`).
4. `Kit_EvacPlacard.fbx.meta` keeps a stale remap entry for `Prop_Aluminium` (no submesh uses it). Harmless; left as Unity wrote it.
5. Codex audit: `codex_audit/20_findings.md` still does not exist; `00_main_state.md` and the `10_review_*.md` notes have no placard finding (re-checked 01:3x).

## 11. Files

**In this folder:**
- `35_fix.md` (this file), `40_contract_map.md`, `41_promotion.md`, `42_request_graphic.md`.
- `images/q16_fix_*` (VL203–209), `threeview/` (index.json + png/).
- `fix_src/`: the four runtime scripts (`.cs.txt`), `placard_q16.diff.txt` (the whole package as a diff), `contract_q16_1.diff`, `pack_evac_plan.py.txt`, the clone tools (`FrontRoomsPlacardRender`, `FrontRoomsPlacardFixBatch`, `FrontRoomsPlacardLookdev`, `TextureFormatProbe`), `build_package.py.txt`, `calm_sim.py.txt`, `calm_chart.py.txt`, `candidate_art/`.
- `fix_data/`: `fix_P1`, `fix_P2` (series, frames, log, report), `simulation.txt`, `batch_report.txt`, `texture_format_probe.txt`.

**Working files:** `/Users/redwang/FrontRoomsVisualWork/placard_fix/` (raw frames in `render/`, three-view, candidate art, charts), logs in `W/logs/placard_fix/`.

**Figma (FRONTROOMS · VISUAL VERIFICATION LOG `2595:6093`):**

| VL | Node | Check | Verdict |
|---|---|---|---|
| VL203 | 2859:6151 | Anodised frame at 0.3 m | PASS |
| VL204 | 2859:6181 | Swatches vs the real wall | FLAG |
| VL205 | 2859:6208 | Calm Failing legend reads | PASS |
| VL206 | 2859:6230 | Dark legend, 2 lit neighbours | PASS |
| VL207 | 2859:6257 | Lens never veils the glow | PASS |
| VL208 | 2859:6287 | Frame clears the guide row | PASS |
| VL209 | 2859:6314 | Placard kit three-view, v3 | PASS |

The section grew to 53 rows (65,800); the cover now reads 24 tasks, 207 checks, 614 images; Q16 = `VL123–209 · 16 checks`.
