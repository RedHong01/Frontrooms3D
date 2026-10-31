# 60 · Evacuation plan on every stairwell landing (BS6, 游戏视觉, 2026-10-31)

Status: **built, compile-gated, not yet run** (Red 10-31 00:47, rule 25: no per-package tests; this lands, then the combined round tests it). Package `W/apply/bs6_landing_placards.sh` (v2, landed 13:1x); token patches (§5, Red 10-31 "(c)") `W/apply/bs6_token_patches.sh` (v2). Row: `research/tutorial/20_requests.md` BS6 (教程设计, "V6 now").

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
| `Assets/Scripts/Rendering/FrontRoomsLandingPlacardSheets.cs` (new, generated) | The 20 sheets: Resources paths (since §5: the variant's base and the token's patches + rects), EXAMINE transcripts (from each examine.json, with the region heads of `inspect_transcript.py`), regions (B10+ has a 3.2 mm title-block cap) |
| ~~`Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A_B1 … B9, B10plus` (+ `…Phase0`) (new, 20 + .meta)~~ | The prints, packed by `pack_evac_plan.py` unchanged (4096 × 2048, sRGB, CompressedHQ). **Moved out by §5** (to the package's backup under W): replaced by 2 bases + 20 token patches |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_Locked_E.png` (new + .meta) | The full sheets' one glow mask (md5 9d3a9480 in every `sheetA_B*`), linear BC1 (its name ends in `_E`, so the surface importer keeps it linear) |
| `Tools/lookdev/pack_landing_placards.py` (new) | Packs and generates all of the above; `--check` rebuilds in a scratch folder and compares (CHECK OK, 22 outputs, 21 metas, 2026-10-31) |

Not written: the Phase 0 sheets' glow mask (md5 ed18539e in every `sheetA_phase0_B*`) is **byte-identical** when packed to main's `Prop_EvacPlan_E.png` (sha1 81a86c5e, packed from `sheetA_phase0`), so those sheets use it and the tool checks it. No existing file changes: not `FrontRoomsPlacard.cs`, not the title or station mount, not the catalog (transcripts go in at run time through `FrontRoomsInspectCatalog.SetTranscript`), no map file, nothing of sound.

Art: 平面视觉's BS8 folders (2026-10-30 23:40), used as delivered; every print, mask and examine.json md5 matches their README tables.

## 3. Cost

- **Memory:** one print resident at a time (≈ 10.7 MiB with mips, BC7 / ASTC 4×4) plus the full sheets' glow (≈ 5.3 MiB, BC1) or the shared Phase 0 glow (already resident). The last floor's textures are unloaded one frame after its copy goes.
- **Build size:** 20 prints × ≈ 10.7 MiB + 5.3 MiB ≈ **219 MiB more on desktop and on iOS** (the importer's default tab, 4096, as Floor 1's sheets); WebGL ≈ 7 MiB. PNGs in the repo ≈ 16 MB.
- **Since §5 (Red 10-31 "(c)"):** ≈ 28.5 MiB on desktop and on iOS (2 bases 21.3 + 20 patches 1.8 + the Locked glow 5.3), WebGL ≈ 1.9 MiB; PNGs ≈ 1.8 MB. Memory: still one print's worth per floor (the composite is the base's format on desktop and phones).
- **Cheaper, if Red wants it (as offered before (c) was picked):** (b) iOS tab 2048 for these 20 prints: iOS ≈ −160 MiB, the copy slightly soft at the closest EXAMINE zoom, desktop unchanged; (c) the sheets differ only in two token strips (title band x 1717–2466 / y 132–223 px, title block x 1813–2461 / y 1459–1552 px at 6 px/mm): two base prints plus 40 small patches composited once per floor would cut both to ≈ 25 MiB, at the price of a run-time composite.

## 4. What the combined round must check

1. Unity import (mac, iOS, WebGL): the 23 textures (2 bases, 20 patches, the Locked glow) import with their .meta GUIDs unchanged; bases and patches sRGB CompressedHQ (BC7 on desktop, ASTC 4×4 on iOS, every patch a power of two so it compresses), `Prop_EvacPlan_Locked_E` linear Compressed; WebGL tab 1024 (the patches stay full size there). No `Prop_EvacPlan_A_B*` left in Resources.
2. Play 2554 `-autopilotExit` through floors 2–4 (and to floor 11 once, for B10+): per floor one log line `[Placard] landing LEVEL B<n> (floor <f>): P1 west, sheet sheetA[_phase0]_B<n> …` or `… no sheet: StairDoorPlacardWest false …`; no exception; the last floor's copy gone; one print resident (Profiler, Memory).
3. Station floors (bs_on) hang `sheetA_B<n>`; a no-fit floor hangs `sheetA_phase0_B<n>`.
4. Frames (Shot format, `W/vl_pending/bs6_landing_placards.md`): the copy from the stairwell doorway as the far door opens; flush on the wall, no z-fight, its keep-clear free of furniture; dark (cell lamp out): the glow at k 0.158, dark ratio ≥ 1.30 as Q16; desktop | iPhone | WebGL (no lens).
5. EXAMINE (InspectPlaytest or by hand): the transcript is the sheet's examine.json (LEVEL B<n> in the title band and the title block; B10+ on floor ≥ 11), glow lines only when dark, the four regions (title block cap 3.2 mm on B10+).
6. No floating plan: walk away until the stairwell is removed; come back: no copy. The chunk that builds the copy's wall (the cell south of W) or W's dropped and rebuilt: the copy shows only with its wall.
7. G2 (平面视觉): one HUD frame each on B1 and B10, with the HUD's LEVEL tag next to the copy's token.
8. Token patches (§5): see §5.4.
9. `Tools/localization/language_tables.py check`: the new lines (`EVACUATION PLAN · LEVEL B<n>`, `LEVEL B<n> · SHEET A-2 OF 4 · PRINTED 11/89`, n = 1…9 and B10+) have no rows yet; they read in English until 设置菜单与画质预设 adds them.

## 5. Token patches (Red 2026-10-31: "(c)，继续，做完一个合一个")

The 20 per-floor prints (≈ 213 MiB on desktop and on iOS) become **2 base prints + 20 small token patches** (≈ 23 MiB), composited once per floor, same sharpness.

### 5.1 What differs (measured on the 20 packed prints, texels of the 4096 × 2048 print, from the top-left)

- Within each variant the 10 sheets differ only in two bands: the title band (y 160–276) and the title block (y 1782–1900). Outside them all 10 are identical, texel for texel.
- B2 … B9 differ from B1 only at the digit: title band x 3797–3904, title block x 3067–3098 (y 1825–1858). B10+ re-centres both lines: title band x 2710–3896, title block x 2863–3893.
- A token's texels in those bands are the same in its full and Phase 0 sheets (checked over every patch rect), so one set of patches serves both variants. The glow masks do not differ per floor (one per variant already): nothing to patch.

### 5.2 Bases and patches (`Tools/lookdev/pack_landing_placards.py`, CHECK OK 24 outputs, 23 metas)

| Texture | Rect (x, y, w, h) |
|---|---|
| `Prop_EvacPlan_A_Landing` / `…LandingPhase0` | the full / Phase 0 base = `sheetA_B1` / `sheetA_phase0_B1`'s packed print, byte for byte (B1 needs no patch) |
| `Prop_EvacPlan_A_Landing_B<n>_Title0`, n = 2 … 9 | 3712, 128, 256, 256 |
| `Prop_EvacPlan_A_Landing_B<n>_Block0`, n = 2 … 9 | 2944, 1792, 256, 128 |
| `Prop_EvacPlan_A_Landing_B10plus_Title0` / `_Title1` | 2688, 128, 1024, 256 / 3712, 128, 256, 256 |
| `Prop_EvacPlan_A_Landing_B10plus_Block0` / `_Block1` | 2816, 1664, 1024, 256 / 3840, 1664, 128, 256 |

Each rect is the token's differing texels (both variants) grown out to a 128-texel grid and cut into power-of-two pieces (Unity 6 keeps a non-power-of-two texture with mips uncompressed). On that grid a rect stays on the 4 × 4 block grid down to mip 5, and the patch's own box mips equal the print's there. The tool proves, pixel for pixel, that each base with its token's patches is that token's packed print, in both variants (20 of 20); it stops if a token ever differs elsewhere, or between its variants.

### 5.3 Run time (`FrontRoomsLandingPlacard.LoadPrint`, once per floor, nothing per frame)

- B1: the base is the print.
- Desktop, iPhone, iPad (`blocks`): a GPU-only texture in the base's own format (BC7 / ASTC 4×4: the memory of one print), the base copied in (every level), then each patch's compressed blocks copied over its rect, levels 0–5 (`Graphics.CopyTexture`). Below level 5 the print is 64 texels wide and keeps the base's texels (no token is legible there). Identical blocks to the old per-floor print at levels 0–5: the encoder compresses each 4 × 4 block on its own.
- WebGL (`blit`; WebGL 2 cannot copy between compressed textures), and any device without block copies: an sRGB RenderTexture at the base's size (1024 on WebGL, 2.7 MiB), the base blitted in, each patch drawn over its rect (UI/Default, always included; white; the full-size patch is read at its mip 2 = the base's scale), mips regenerated.
- A missing patch, or no path working: the base (LEVEL B1's token, a warning in the log), never a blank. The base and the patches unload one frame after the composite is made; the composite is destroyed (its RenderTexture released) when the copy goes. `PrintMode` says which path ran; the hang's log line carries it (`print blocks`).

### 5.4 What the combined round must check (adds to §4)

1. Each floor's composite equals the old full print: read back the composite (blocks: the levels 0–5 bytes; blit: the RT) on B1, B5, B10 (floor 11) and B11 (floor 12, B10+) against the backed-up `Prop_EvacPlan_A_B<n>[Phase0].png` imported the same way: identical blocks at levels 0–5 on desktop and iPhone; within compression elsewhere; both variants.
2. The log line says `print base` on B1 and `print blocks` on B2+ (desktop, iPhone, iPad), `print blit` on WebGL; no fallback warning, no `Graphics.CopyTexture` error.
3. Phone (iPhone, iPad): same as desktop (ASTC 4×4 4096); the token reads at the closest EXAMINE zoom.
4. WebGL: the composite RT is 1024 × 512 and upright (LEVEL B<n> reads, not mirrored), the token as sharp as the rest of the sheet.
5. Memory (Profiler): one composite per floor (10.7 MiB desktop / iPhone, 2.7 MiB WebGL), the base and patches gone one frame after the hang, the last floor's composite gone at the floor change; one-frame peak at the hang about +21 MiB on desktop and iPhone (base + the new texture's CPU copy until it is uploaded).
6. EXAMINE transcripts and regions unchanged (the generated table's lines and regions are byte-identical to the landed BS6 table).
7. A renamed patch (rehearsal only, in a clone): the copy hangs the base with the warning, never a blank.
