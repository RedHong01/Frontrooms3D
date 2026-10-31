# 60 · Evacuation plan on every stairwell landing (BS6, 游戏视觉, 2026-10-31)

Status: **built, compile-gated, not yet run** (Red 10-31 00:47, rule 25: no per-package tests; this lands, then the combined round tests it). Package `W/apply/bs6_landing_placards.sh` (v2). Row: `research/tutorial/20_requests.md` BS6 (教程设计, "V6 now").

## 1. What the player gets

On every floor below LEVEL 0 the player steps out of the stairwell through its far (north) door and finds the plan on the wall beside it, on the map side, west of the door, at the height and offsets of Floor 1's title placard (P1 west: centre 1.560 m, inner frame edge on the cell line, 0.5 mm off the wall face, facing into the floor). It is the same Q16 kit (frame, lens, phosphor print), the same glow driver (k 0.158, FrontRoomsPhosphor.PeakEmission, unchanged) and the same EXAMINE (E · READ PLAN). The sheet names the floor (LEVEL B1 … B9, then LEVEL B10+ for every floor below B9).

- **Where it hangs:** only where 关卡设计's G1 rule for floors 2+ holds, `FrontRoomsExitPlanner.StairDoorPlacardWest` (the door cell's west edge open, a wall under the west cell to hang it on). That is the spot the sheet's YOU ARE HERE is drawn for (平面视觉 README "The stairwell copy"). Where it does not hold, the floor hangs **no sheet**: a P1 east or P2 copy would put YOU ARE HERE on the wrong side of the door. SeedFor prefers seeds where it holds; the validator counts the rest (`placardWestMissing`).
- **Which sheet (叙事 40 §3, A35):** the floor's own state at the change. A floor with its own station and a locked EXIT (`map.FloorHasStation && map.ExitLocked`, set by 关卡设计's BS2 before `FloorChanged`) hangs the full sheet `sheetA_B<n>` (the security desk, the padlocked stair door, line 4 `STAIR DOORS ARE LOCKED. KEY AT SECURITY DESK.`). Every other floor (no station fits: A35) hangs `sheetA_phase0_B<n>`. The sheet is chosen once per floor, like Floor 1's.
- **When:** `FrontRooms3DGame.FloorChanged` (the map reseeded, the EXIT and station planned, Begin called, nothing built yet). The copy shows while the chunks of its wall (the north edge of the cell south of W, which that cell's chunk builds) and of W are built (as the stairwell does, D8), and is taken down when the start area is given back (the stairwell removed: its door, which the sheet's NO REENTRY names, is gone) or if a revisit shift ever rebuilds that chunk without the wall.
- **Platforms:** desktop, iPhone and iPad hang the same copy, lens included; WebGL keeps the title mount's rule (no lens; its textures are 1024 by the importer's WebGL tab).

## 2. What was built

| Path | What |
|---|---|
| `Assets/Scripts/Rendering/FrontRoomsLandingPlacard.cs` (new) | The per-floor mount: one component on the map, FloorChanged hook, P1-west resolve through `FrontRoomsPlacard.Resolve`, keep-clear (`map.KeepClearAtStart`), kit + lens spawn (shadows off), `FrontRoomsPlacardGlow`, the sheet's textures, transcript and regions; visibility (the wall's chunk and W's) and retirement; one floor's textures at a time |
| `Assets/Scripts/Rendering/FrontRoomsLandingPlacardSheets.cs` (new, generated) | The 20 sheets: Resources paths, EXAMINE transcripts (from each examine.json, with the region heads of `inspect_transcript.py`), regions (B10+ has a 3.2 mm title-block cap) |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_B1 … B9, B10plus` (+ `…Phase0`) (new, 20 + .meta) | The prints, packed by `pack_evac_plan.py` unchanged (4096 × 2048, sRGB, CompressedHQ) |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_Locked_E.png` (new + .meta) | The full sheets' one glow mask (md5 9d3a9480 in every `sheetA_B*`), linear BC1 (its name ends in `_E`, so the surface importer keeps it linear) |
| `Tools/lookdev/pack_landing_placards.py` (new) | Packs and generates all of the above; `--check` rebuilds in a scratch folder and compares (CHECK OK, 22 outputs, 21 metas, 2026-10-31) |

Not written: the Phase 0 sheets' glow mask (md5 ed18539e in every `sheetA_phase0_B*`) is **byte-identical** when packed to main's `Prop_EvacPlan_E.png` (sha1 81a86c5e, packed from `sheetA_phase0`), so those sheets use it and the tool checks it. No existing file changes: not `FrontRoomsPlacard.cs`, not the title or station mount, not the catalog (transcripts go in at run time through `FrontRoomsInspectCatalog.SetTranscript`), no map file, nothing of sound.

Art: 平面视觉's BS8 folders (2026-10-30 23:40), used as delivered; every print, mask and examine.json md5 matches their README tables.

## 3. Cost

- **Memory:** one print resident at a time (≈ 10.7 MiB with mips, BC7 / ASTC 4×4) plus the full sheets' glow (≈ 5.3 MiB, BC1) or the shared Phase 0 glow (already resident). The last floor's textures are unloaded one frame after its copy goes.
- **Build size:** 20 prints × ≈ 10.7 MiB + 5.3 MiB ≈ **219 MiB more on desktop and on iOS** (the importer's default tab, 4096, as Floor 1's sheets); WebGL ≈ 7 MiB. PNGs in the repo ≈ 16 MB.
- **Cheaper, if Red wants it:** (b) iOS tab 2048 for these 20 prints: iOS ≈ −160 MiB, the copy slightly soft at the closest EXAMINE zoom, desktop unchanged; (c) the sheets differ only in two token strips (title band x 1717–2466 / y 132–223 px, title block x 1813–2461 / y 1459–1552 px at 6 px/mm): two base prints plus 40 small patches composited once per floor would cut both to ≈ 25 MiB, at the price of a run-time composite.

## 4. What the combined round must check

1. Unity import (mac, iOS, WebGL): the 21 textures import with their .meta GUIDs unchanged; prints sRGB CompressedHQ 4096, `Prop_EvacPlan_Locked_E` linear Compressed; WebGL tab 1024.
2. Play 2554 `-autopilotExit` through floors 2–4 (and to floor 11 once, for B10+): per floor one log line `[Placard] landing LEVEL B<n> (floor <f>): P1 west, sheet sheetA[_phase0]_B<n> …` or `… no sheet: StairDoorPlacardWest false …`; no exception; the last floor's copy gone; one print resident (Profiler, Memory).
3. Station floors (bs_on) hang `sheetA_B<n>`; a no-fit floor hangs `sheetA_phase0_B<n>`.
4. Frames (Shot format, `W/vl_pending/bs6_landing_placards.md`): the copy from the stairwell doorway as the far door opens; flush on the wall, no z-fight, its keep-clear free of furniture; dark (cell lamp out): the glow at k 0.158, dark ratio ≥ 1.30 as Q16; desktop | iPhone | WebGL (no lens).
5. EXAMINE (InspectPlaytest or by hand): the transcript is the sheet's examine.json (LEVEL B<n> in the title band and the title block; B10+ on floor ≥ 11), glow lines only when dark, the four regions (title block cap 3.2 mm on B10+).
6. No floating plan: walk away until the stairwell is removed; come back: no copy. The chunk that builds the copy's wall (the cell south of W) or W's dropped and rebuilt: the copy shows only with its wall.
7. G2 (平面视觉): one HUD frame each on B1 and B10, with the HUD's LEVEL tag next to the copy's token.
8. `Tools/localization/language_tables.py check`: the new lines (`EVACUATION PLAN · LEVEL B<n>`, `LEVEL B<n> · SHEET A-2 OF 4 · PRINTED 11/89`, n = 1…9 and B10+) have no rows yet; they read in English until 设置菜单与画质预设 adds them.
