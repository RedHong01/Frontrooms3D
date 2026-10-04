# Level transitions · 20 · Variation V5 Light lead: summary

Date: 2026-10-03. Full report, numbers, diff and logs: `15_var_lightlead.md`. This page is the short version.
Built and rendered in the private clone `proj_trans_lightlead` only. Red's project only received the files listed at the end.

## Re-run (2026-10-03 night): the copy in Red's game

Codex committed this variation into main at 19:10 (`8ef5b64`). Main runs B0 and the cool Office colour and lens by default; the dead/dim border rule only runs with `-lightleadBorders` or `-lightleadSoft`. Red has not picked. Full check: `15_var_lightlead.md` §15.
- **Tests on a fresh copy of main, as the game runs it:** 136/136 map interaction, 28/28 lamp tick, 100/100 seeds; `LampModeOf` matches all 1597 built lamps; Office 281/281 lamps cool, Level 0 1316/1316 warm.
- **E1, bug:** with the border rule on, `LampModeOf` generates chunks it should only read (135/136). Fix verified in the clone (136/136, 0 mismatches). Patch for the map chat: `contracts/lightlead_lampmodeof_pure.diff`.
- **E2, side effect:** B0 reads the edges of the ring of chunks round every build, so it generates them early (47 chunks of data instead of 28 for a 25-chunk build). Their tier is fixed one chunk early. Map chat to decide.
- **E3, process:** the colour lead is live without Red's pick. One line makes it opt-in, if Red wants that.
- **E4-E6, small:** stale comments (patch `contracts/lightlead_comments_visual.diff`); the Office lens is now 0.67x the Level 0 lens (×1.5 since 17:16); the title stream's Office still has the warm lens.
- **No code in main was edited.** Fixes go through the `codex-audit` workflow and the map chat.
- **New image:** `images/var_lightlead_main_sheet.jpg`, the four fixed shots from main's code (off, as the game runs, border rule). Rendered 2026-10-04 (the first try was killed when the session ended). Details: `15_var_lightlead.md` §15.4.
- **Main matches the prototype.** With `-lightleadBorders`, lit lamps 81/77/79/86 (the same as the clone) and a mean pixel difference of 0.13-0.67 of 255. With `-lightleadSoft`, 85/83/83/89 (same as the clone). Layout frozen: all 16 plan files identical.
- **As the game runs it today, it is colour only.** Office b/r 0.69-1.01 -> 0.80-1.23 and +2-3 % luma; Level 0 unchanged (0 %). The Level 0 cell is still brighter than the Office through the opening (shot1 122 vs 77), so the cut stays in full light. The border rule makes the Office 1.6x-2.7x brighter than the dark cell; soft only evens them (0.9x-1.2x).
- **The two patches still apply** cleanly to Red's current files (`git apply --check`; nothing applied). Main HEAD is `d610d3a`; the render code has not changed since `75cfdff`.

## Images

- `images/v_lightlead_vs_before.jpg`: six rows, BEFORE | LIGHT LEAD | SOFT, for the four fixed shots and two extra shots.
- `images/v_lightlead_shot1.jpg` .. `shot4.jpg`: the four fixed shots (same harness and cameras as `shot<k>_before.jpg`).
- `images/v_lightlead_shot5.jpg`: extra, from a lit Level 0 cell through the dead border cell to the Office open edge on X = 798.
- `images/v_lightlead_shot6.jpg`: extra, from the Office back through the shot1 arch into the dead cell and the warm Level 0 beyond.
- Also: `var_lightlead_shot1-4.jpg` (same files), `var_lightlead_sheet.jpg` (4 rows, before | after), `var_lightlead_close_post.jpg` and `var_lightlead_close_arch.jpg` (close frames).

## What was built

- **Lamp colour per theme.** Office (0.90, 0.96, 1.00) at 6.0 (+9 %); Level 0 unchanged (1.00, 0.96, 0.88) at 5.0.
- **Cool Office lens** `Troffer_Lens_Cool`: emission (1.989, 2.121, 2.210), same luminance (2.100) and textures as `Troffer_Lens`.
- **Border rule** for Auto lamps: Level 0 cell with an open or arch crossing into Office -> dead; Level 0 cell with only door or window crossings -> dim; Office cell with any crossing -> steady.
- **B0.** Corner posts: no two pieces of different finish overlap; the owner's outside skin wraps the post (per skin, a refinement found in iteration 1). Grime band: each face of a height-border wall in its own height block.
- **Optional `lightlead_soft`** (all Level 0 border cells dim) rendered as a full 4-shot set.

## Change list (clone)

1. `FrontRoomsMapWorld.cs`: 8 hooks marked `// TRANSITION lightlead`, +111 / -13 lines: `lampColor`, theme values and lens, `light.color`, the border rule after the tier roll, neighbour lookup, per-side blocks, per-skin corner reach, post lookup.
2. New: `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (B0.1 rule, pure), `FrontRoomsTransitionLightLead.cs` (colours and border rule, pure).
3. New: `Resources/Surfaces/Troffer_Lens_Cool.mat` (2.1 KB).
4. Tools only: `Editor/Transitions/FrontRoomsLightLeadTools.cs`, `FrontRoomsLightLeadPostCounts.cs`.
5. Harness and `shots.json` unchanged. Layout frozen: all `-plan` files identical to `logs/shot<k>_plan.json`. With `-transitionsOff` the frames are byte-identical to BEFORE.

## Results

| Shot | Lit lamps before -> after | Level 0 border walls (luma) | Office beyond (luma, blue/red) |
|---|---|---|---|
| shot1 | 84 -> 81 | 114 -> 43 (-62 %) | 72 -> 70, 0.98 -> 1.19 |
| shot2 | 82 -> 77 | 107 -> 38 (-64 %) | 87 -> 83, 0.80 -> 0.97 |
| shot3 | 83 -> 79 | Level 0 Low room 117 -> 104 (-10 %) | Office walls 71 -> 73, 0.85 -> 1.04 |
| shot4 | 88 -> 86 | 114 -> 70 (-39 %) at (265,201) | 107 -> 106, 0.69 -> 0.82 |

- The contrast flips: the Office is now 1.6x to 2.2x brighter than the Level 0 cell in front of it (before, it was darker).
- Office areas next to a border are 1-7 % darker in absolute terms, because the dead Level 0 lamp no longer spills into them.
- Rule per chunk (1620 chunks): 1.08 dead, 1.52 dim, 2.49 Office steady. 2.0 % of Level 0 cells forced dead, 2.8 % dim.

## Costs measured (within 46 m of each eye)

- Light lead itself: 0 triangles, 0 renderers, 0 draws, 0 MB textures, one 2.1 KB material. 2-5 fewer lit lights, 0-1 fewer shadowed.
- B0: +1.0 to +1.4 % triangles (175,766 -> 177,890 in shot1), renderers -1 to +14 (two skins on height-border walls).
- Draw-call proxy (renderers in the frustum): -5 to +1. Batch-mode `UnityStats` reads 0, so true draw counts need a Play capture.

## What the real implementation needs

- **Map chat:** `lampColor` in `ThemeMaterials` and `BuildFixture`; the border rule in `BuildFixture` after the Auto roll and before the module and `SetLampMode` overrides, **and the same rule in `LampModeOf`** (not through `SetLampMode`); B0.1 and B0.2 with a per-chunk post table; tests updated. Rebase on Red's 12:38 `FrontRoomsMapWorld.cs` (lamp interface v1).
- **Visual chat:** `Troffer_Lens_Cool` as a `SurfaceDef` row in `FrontRoomsRenderSetup`; the colour values; a check of the Office post grade; optional dim-lens look.
- **Others:** 系统设计 (Relay Warn), wallpaper chat (ink gate), sound chat (no hum at frameless borders).

## Open issues

1. The Relay's lamp warning (`LampFx.Warn`) cannot show in a dead cell. Flag for 系统设计: let Warn force a blink, use soft, or keep colour only.
2. The phosphor ink gate opens fully in dead cells and partly in dim ones, so every frameless border becomes an ink site.
3. Darkness at every frameless border becomes a tell. Soft halves it (-32 % instead of -62 %).
4. V5 hides the split arch jambs and flat seams; it does not fix them. Pair with V1 Frame or V3 Neck.
5. Dim cells read weakly in stills (the 0.42 lens still blooms); check in Play.
6. Red decides: dead, soft or colour only. The game runs colour only today; the shadow lead in the Figma variation needs the border rule on, after the E1 patch.
