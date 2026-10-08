# Codex audit · 10 · Review: what Red sees (visual track)

| | |
|---|---|
| Date | 2026-10-07, 16:30–19:55 PDT (first pass 16:30–17:46 was cut off before its report; this pass reused its captures, re-shot everything at HEAD and finished) |
| Track | visual: every room, window, door, key, sign, glass pane and HUD element Red sees in Editor Play Mode |
| Bases | pre-Codex `7320ed1`; Codex's last 10-03 commit `75cfdff`; reviewed at main HEAD **`6c6fe81`** (2026-10-07 18:29) plus its working tree (2 uncommitted touch files, not visual) |
| Red's project | Read only. Unity was never opened on it. Nothing committed. Files written: this report, `images/vis_*`, two patch files (`vis_F1_*`, `vis_F2_*`), rows VL151–VL154 in `Documentation/VERIFICATION_LOG.md` |
| Clones | `W/proj_cx_vis` (main, re-synced 18:46 to `6c6fe81`) and `W/proj_cx_pre` (`git archive 7320ed1`). `W` = `/Users/redwang/FrontRoomsVisualWork` |

---

## 0. Short answer (for Red)

- **Nothing is pink, cyan or missing on screen.** 84 frames from the same seed and cameras, before and after Codex: 0.000 % magenta and 0.000 % cyan pixels in every frame. No new error or exception in the Play logs.
- **Most of what changed is intended and already owned:**
  - every map window now has the kit frame and clear glass (−9 to −29 mean luma; window-landing and the kits review own it);
  - every room is +1 to +4 luma brighter from the zone reflections (glass-look F3, your call);
  - Office rooms are cooler from the V5 lamps (you keep V5 on).
- **Three real problems, all visible in your Editor today:**
  1. **V1 · The key panel is gone on desktop.** After you pick up a key, the HUD shows nothing. A Codex iPhone-UI change (`c1f2d31`, 10-04) re-anchored the panel to the top-left but kept the desktop offset, so it sits 118 px above the screen. One-line fix, tested in a clone.
  2. **V2 · Ray-traced window glass reflects Level 0 wallpaper as dark red.** G14 reads the wallpaper's paper code as a colour. 3.6–10 % of the frame turns red in close window shots. A 9-line patch in the RT material export removes it (≤ 0.03 %), tested in a clone.
  3. **V3 · The Office CRT ad shows a real brand.** "LEVITZ" (a real furniture retailer) for 3 s of every 12 s, and the "FRONTROOMS FURNITURE" title is cut off at both edges. This breaks the era lock (no trademarks) and the rule that period ads never ship.
- **Later commits:** the RT shader hook (`9eddc35`) is what made V2 visible; the kit LOD fix (`663e858`) holds (frames drawn to 16.5 m); the Office kit change (`04c3a6a`) is V3; the touch commits change nothing on desktop (HEAD vs 16:33: mean |dY| 0.06 over 84 frames).

---

## 1. How I checked

- **Harness.** `FrontRoomsCxVisCapture.RunBatch` (W/TOOLS.md §3), Play Mode, Metal, quality High, FOV 76 (50 for close-ups), run seed 516574485, map root (−576, 0, −576), title profiles at (4992, 0, 4992). 84 frames: live title, the 5 title-profile rooms (Lobby, Shift, Office, Run, Exit; wide, detail, doorway, sign, threshold, ceiling), title doorway, start room + handoff, Level 0 Standard/Tall, Office, the L0→Office theme seam (B0 posts both sides), heights band, CRT kit, prop glass, 4 windows (both faces at 1.5 m, head and sill at 0.3 m), door, key (2 m, 1 m, taken), dead lamp, glass hold, Relay. `frames.txt` holds every camera pose; `dump.txt` holds materials, kits, RT and HUD state.
- **Runs** (all in `W/cxv/`):

| Run | Code | RT | Frames | Use |
|---|---|---|---|---|
| pre | `7320ed1` | none (no G14 then) | 84 | BEFORE |
| run1 | `279c144` + work tree (16:33) | High | 84 + 27 extra (RT on/off at windows, frames at 1.5–25.5 m) | first pass |
| run2 | **HEAD `6c6fe81`** | High (default) | 84 | AFTER |
| run3 | HEAD, `-frGlassRT off` | Off | 84 | RT isolation |
| run4 | HEAD + V2 patch | High | 84 | V2 fix check |
| run5 | HEAD + V1 patch | Off | 84 | V1 fix check |
| trans | pre and run1, `FrontRoomsTransitionAudit.ShotsBatch` with `research/level_transitions/shots.json` | High | 4 + 4 | transitions B0/V5 |

- **HEAD = run1 on screen:** 84 frames, mean |dY| 0.06, max 1.62 (RT noise in a window frame) (`W/cxv/stats_run1_vs_run2.tsv`). So the run1 extras still describe HEAD.
- **Window families present:** only W-L0 (`Kit_WindowFrame_Wood`, walnut) and W-OF (`Kit_WindowFrame_Steel`). The map has two themes (Level0, Office); the title corridor has no windows, so W-RN and W-EX never appear.
- **Not placed anywhere in the game:** door kits (doors are the map's `Door leaf` cube in `Door_Veneer`), key kits (the key is the map's `Map test / key` cube), key hooks, tags, signage kits. Nothing to check on screen for them.

---

## 2. What Red sees, area by area

Numbers are pre-Codex → HEAD on the same camera (mean luma change dY, mean |dY|, share of pixels changed by more than 8/255). Sheets: `images/vis_sheet_title_rooms.jpg`, `vis_sheet_map_rooms.jpg`, `vis_sheet_windows.jpg` (VL151); per-frame values in `images/vis_sheet_stats.json` and `W/cxv/pairs/stats.tsv`.

| Area | Frames | Change | Verdict |
|---|---|---|---|
| Title Lobby, Shift, Office, Exit | 15 | +1.0 to +2.1 dY, ≤ 1.8 % > 8 | OK. Zone cube on the title (glass track, verified); decision is glass-look F3 |
| Title Run | 5 | wide +3.8 (10.5 % > 8), sign +1.6 | OK, same cause; Run takes the most (glass-look F3 measured the same) |
| EXIT signs (Run) | 1 | +1.6, sign unchanged | OK |
| Title doorway, start room, handoff doorway | 3 | +1.5 to +2.1 | OK. HUD crosshair dot present in both |
| Level 0 Standard / Tall | 6 | +0.9 to +1.2; `l0-tall_wall` −3.7 (9.2 %) | OK. The Tall-wall drop is the new window frame and clear glass in view |
| Office room | 4 | +1.4 to +3.1, hue shifts to blue-green | OK. V5 cool lamps (RED_DECISIONS: keep on) |
| L0 → Office seam, B0 posts | 6 | +0.2 to +2.6, 0 % > 8 on the L0 side | OK. No notch, gap or light leak at the two seam ends shot. MAP-2's revisit case is not covered by a fixed camera |
| Doors (shut, open, detail) | 3 | +0.5 to +0.9 | OK. No kit placed; the leaf is unchanged |
| Key (2 m, 1 m) | 2 | +0.5 | OK. Same cube and material |
| **Key taken, HUD** | 1 | +0.1 | **FAIL: the key panel is missing (V1)** |
| Dead lamp, dead lens | 2 | +1.5, +1.6 | OK |
| CRT kit close-up | 1 | +2.9 | OK for the model. **The ad on its screen is V3** |
| Prop glass, water bottle | 2 | +2.7, +3.7 | OK. Glass-track G4 values (glass-look §2) |
| Relay | 1 | +0.7 | OK. Same procedural rig |
| **Windows**, 4 × (2 faces at 1.5 m, head and sill at 0.3 m) | 20 | −2.8 to −28.7 dY, 24–57 % > 8 | Changed as designed: kit frame + clear `Glass_Window` replace the milky map pane. Frames meet the walls in the 7 of 12 close-ups I inspected by eye (no gap, no floating frame); stills cannot rule out flicker. The map's trim boxes are still drawn under each kit frame, seen as a second casing step at 0.3 m (kits F2). **With RT on, hall-side close-ups show V2** |
| Glass hold (break) | 4 | −19 to −31 | Same as windows. No shatter within 2.5 s in either build (the harness hold, not a regression) |
| Windows at distance (run1 extra) | 15 | — | Frames drawn at 1.5–25.5 m; nothing culls at 12 m (`663e858` holds, as VL114) |
| Transitions (4 fixed shots) | 4 | main vs pre \|dY\| 1.1–1.3 | OK on screen. Note for Red: main's V5 is colour only; the V5 render you saw in Figma also dimmed border lamps (\|dY\| ≈ 29 against pre). `images/vis_sheet_transitions_v5.jpg`; MAP-1 owns it |

---

## 3. Findings

### V1 · major · CONFIRMED · The desktop HUD no longer shows the held key

- **What Red sees.** After picking up a zone key, nothing appears. Before Codex, the bottom-left showed the key glyph and "LEVEL 0 / LOW ROOMS KEY". Frame `76_key_taken_hud`: `keyPanel True` in `frames.txt` (run2), nothing drawn. `images/vis_hud_key_panel.jpg` (VL152).
- **Cause.** `Assets/Scripts/FrontRooms3DGame.cs:1883` at HEAD:
  `keyPanel = TypographyGroup(g.transform, "HUD / Key", new Vector2(0, 1), mobileHud ? new Vector2(78, -72) : new Vector2(72, 118), …)`.
  Before, it was `new Vector2(0, 0)` (bottom-left) with (72, 118) (`7320ed1` L1802, `75cfdff` L1828). `TypographyGroup` sets anchor and pivot to the same value (L1810), so on desktop the panel's top-left sits 118 px **above** the top edge. `ApplyMobileSafeAreaLayout` returns at once when `FrontRoomsHandheld.Active` is false (L1955–1956), so nothing moves it back.
- **Who.** `c1f2d31` (10-04 19:50, "1"). The hunk is in Codex session `~/.codex/sessions/2026/10/04/rollout-2026-10-04T17-24-05-…jsonl` (Red asked for iPhone safe-area UI). The anchor change was not gated by `mobileHud`. Same session: the desktop stamina segments went from 24 × 6 to 24 × 5 px (L1879, also not gated); ask the owner whether that was meant.
- **Fix (verified in the clone).** Gate the anchor; mobile is unchanged because `ApplyMobileSafeAreaLayout` re-anchors it anyway (L1999):

```diff
-        keyPanel = TypographyGroup(g.transform, "HUD / Key", new Vector2(0, 1), mobileHud ? new Vector2(78, -72) : new Vector2(72, 118), mobileHud ? new Vector2(150, 22) : new Vector2(147, 22));
+        keyPanel = TypographyGroup(g.transform, "HUD / Key", mobileHud ? new Vector2(0, 1) : new Vector2(0, 0), mobileHud ? new Vector2(78, -72) : new Vector2(72, 118), mobileHud ? new Vector2(150, 22) : new Vector2(147, 22));
```

  run5 frame 76: the panel is back at the pre-Codex spot, with the new 80 × 44 glyph from `a5262fb`. Patch: `research/codex_audit/vis_F1_key_panel_anchor.contract.diff` (base md5 `bd50d906c29af949d7e50e8f3984bceb`).
- **Owner:** map (`FrontRooms3DGame.cs` is the map chat's file): send the diff as a contract. **Handoff:** 关卡设计 to apply; the touch session (it now edits the HUD layout in `FrontRooms3DGame.Mobile.cs`) to confirm the phone layout and the stamina height. This breaks Red's HUD now, so it should not wait for a larger HUD pass.

### V2 · major · CONFIRMED · G14 reflects every print wallpaper as dark red

- **What Red sees.** In Editor on a Mac (G14 on at High by default), close to a window and looking through it from a Level 0 hall, a hard-edged dark-red slab sits in the glass. It is the traced reflection of the Level 0 walls behind you. With `-frGlassRT off` it is gone. `images/vis_rt_print_wallpaper_red.jpg` (VL153).
- **Measured** (pixels whose red-minus-green rose by more than 10 between RT Off and RT High; same camera; `images/vis_rt_print_wallpaper_red.json`):

| Frame (hall side, 0.3 m) | RT High, main | RT High, patch | Region RGB: Off → High → patch |
|---|---|---|---|
| `win1_OF_hall_head` | 7.67 % | 0.02 % | (31, 40, 26) → (40, 34, 25) → (34, 42, 25) |
| `win2_L0_hall_head` | 3.56 % | 0.02 % | (59, 52, 25) → (66, 45, 27) → (60, 54, 26) |
| `win3_OF_hall_head` | 5.08 % | 0.00 % | (33, 43, 29) → (48, 44, 33) → (44, 54, 34) |
| `win4_L0_hall_head` | 9.99 % | 0.03 % | (62, 56, 28) → (71, 52, 31) → (66, 59, 30) |

  Room-side close-ups (looking out at an Office or Low room) show ≤ 0.04 %.
- **Cause.** `FrontRoomsGlassRTSystem.Describe` (`Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:472`, Surface branch L488–511) hands `_BaseMap` to the hit shader as albedo (L500). With `_FR_PRINT` on, that slot is not a colour: it holds the paper modulation code that `FrontRoomsSurface.shader` decodes with `FR_PAPER_SCALE/BIAS` and multiplies with the print (shader L40–41, L189–240). `Wallpaper_Paper_M.png` averages sRGB (195, 15, 51). `L0_Wallpaper`, `L0_Wallpaper_Shift` and `Exit_Wallpaper` all use it (`dump.txt`: keywords `[_FR_PRINT]`, `_UsePrint 1`). G14 was written before the print layer landed (`df4cb03`, 10-03 17:27); Codex promoted it at 19:10 (`8ef5b64`) without reconciling the two. It became visible on 10-04, when `9eddc35` gave main's glass shader the prepass, so G14 started tracing window glass.
- **Fix now (verified).** Until the hit shader composes paper × print, trace the print's mean ink tone, flat. 9 lines, visual-owned file, RT-only (raster and RT Off never call `Describe`):

```diff
             r.texMacro = (uint)Slot(m, "_MacroMap", Native.TexGrey);
+            // _FR_PRINT wallpapers: _BaseMap holds the paper modulation code (FrontRoomsSurface.shader,
+            // FR_PAPER_*), not a colour; traced as albedo it reads red-magenta (texel mean 195/15/51).
+            // Until the hit shader composes paper x print, trace the print's mean ink tone, flat.
+            if (m.IsKeywordEnabled("_FR_PRINT"))
+            {
+                r.texBase = Native.TexWhite;
+                var ink = Vector4.Lerp(Lin(m, "_InkGround", new Color(.8235f, .7608f, .4863f)), Lin(m, "_InkMid", new Color(.6745f, .6039f, .3216f)), .55f);
+                r.baseColor = new Vector4(r.baseColor.x * ink.x, r.baseColor.y * ink.y, r.baseColor.z * ink.z, r.baseColor.w);
+            }
             if (m.IsKeywordEnabled("_FR_MESH_UV")) r.flags |= Native.MatMeshUV;
```

  - 0.55 is the print's mean density (`Wallpaper_Print_P` R mean 0.278 → ramp position 0.556).
  - run4 vs run2 over all 84 frames: mean |dY| 0.25; every frame above 0.5 is a window or glass frame. Compile 0 errors.
  - Patch: `research/codex_audit/vis_F2_rt_print_albedo.diff`. Base = HEAD's file, md5 `e75df1b1bbfe0e65c5ee36ebe9277d8c`; re-check the base before applying.
- **Owner:** visual. This is a red material in Red's frame now, so it fits the Fix phase. **Handoff:** glass-rt-track for the real fix: add the print layer to the RT material table and decode paper × print in the Metal hit shader (`FR_KIND_SURFACE`, `NativePlugin/FrontRoomsGlassRT.metal:210–262`), then re-run their window set. It also feeds their open lamp-correlation failure (glass-look F5b).

### V3 · major · CONFIRMED · The Office CRT ad shows a real brand, and its title is cut off

- **What Red sees.** Every map Office monitor plays `Assets/StreamingAssets/FrontRooms_Ad_01.mp4` (640 × 434, 30 fps, 12 s loop, md5 `f9ec1412…`), attached by `FrontRoomsOfficeKit.cs:675` (`04c3a6a`, 10-04). Frame `48_kit-crt` (run1): "LEVITZ · LOWEST PRICES OF THE YEAR · SALE / 1990" on the CRT. `images/vis_crt_ad_ingame.jpg`, `images/vis_crt_ad_file_sheet.jpg` (VL154).
  - 0–3 s: **LEVITZ**, a real US furniture retailer (it is in our own research as a real chain: `research/hunter/04_period_wardrobe.md:102`).
  - 3–6 s: **"FRONTROOMS FURNITURE"** at 72 px overflows the 640 px frame; both ends are cut ("RONTROOMS FURNITUR").
  - The type is ffmpeg `drawtext` with Courier New Bold and a condensed sans, chosen by Codex; it never went through 平面视觉.
- **Rules it breaks.**
  - Era lock: no trademarks (`research/office_and_film/22_era_lock.md` §2, as quoted in `research/room_visuals/01_inventory.md:147`; `research/interactables/02_period_hardware.md:347`).
  - "Period ads are research only and must not ship" (`Documentation/GRAPHIC_VISUAL_TASKS.md` §3).
  - Type goes through 平面视觉.
- **Who.** Codex session `rollout-2026-10-04T12-17-14-…_01a10859-….jsonl`: Red asked (19:34 UTC) for watchable ad videos on in-game monitors; Codex rendered the clip at 19:45 UTC with `drawtext=…text='LEVITZ'`.
- **Note.** In the HEAD run the screen was still black at t = 11.75 s (the decoder had not prepared under load average ~800); in run1 it played. Red's editor plays it.
- **Fix.** Not this audit's to author: new ad type and content go through 平面视觉. Red picks the interim:
  - (a) stop attaching the ad (comment out `FrontRoomsScreenVideo.Attach(monitorObject);` at `FrontRoomsOfficeKit.cs:675`): the monitors go back to their `Prop_ScreenCRT` glass, as before 10-04. This also parks glass-look F6 (no material in a player build) and F7 (quad floats after the monitor culls);
  - (b) keep it and have 平面视觉 re-render the clip without the brand and with the title inside the frame.
- **Owner:** visual (the Office kit attaches it). **Handoff:** 平面视觉 (screen-ad pipeline, `GRAPHIC_VISUAL_TASKS.md` §3); Red for (a) or (b).

---

## 4. Later commits (after `75cfdff`), as seen on screen

| Commit | What | On screen at HEAD |
|---|---|---|
| `9eddc35`, `bf2e883`, `70644f0` (10-04, glass shader prepass, markers, roll gate) | G14 now traces map window glass | Visible: troffer highlights in the glass stretch into streaks, and V2's red slab appears. RT High vs Off over the 20 window frames (run2 vs run3): mean \|dY\| 0.24–3.92 (average 1.39), 0.4–6.6 % of pixels > 8. Shader-level checks: glass-look §1; cost and acceptance: `10_review_rt.md` F2/F3 |
| `663e858`, `1114d6b` (10-04, kit LOD cull) | Fixes Codex's 12 m frame cull | Fixed: frames drawn at every distance up to 25.5 m (run1 extra sheet `W/cxv/rt/vis_sheet_window_distance.jpg`; VL114) |
| `04c3a6a`, `b5f381f` (10-04, Office kit + screen video) | Ads on every Office CRT | V3 (content); glass-look F6/F7 (player build, floating quad) |
| `c1f2d31` (10-04 19:50, iPhone HUD) | HUD layout branches | V1 |
| `16f520e`, `5e6e7f9`, `07d2c84`, `6c6fe81` (10-07, touch) | Touch layer | No desktop change: 84 frames, mean \|dY\| 0.06 vs the 16:33 run |
| `a5262fb` (10-07, HUD key glyph 80 × 44) | New key silhouette | Not visible on desktop until V1 is fixed; with the V1 patch it draws at the old size (run5) |

**Which Codex errors later commits fixed:** the 12 m frame cull (`663e858`). The missing prepass (`9eddc35`) is fixed, which in turn exposed V2. None of the later commits touched V1's or V3's cause, which they introduced.

---

## 5. Against the producing workflows' own verified frames

- **Glass, G10 run 3** (`research/glass/images/g10r3_full_window_L0_1.5m_AFTER.jpg`): the same clear `Glass_Window` slab and grime; main adds the kit frame around it. With RT Off, main's glass matches the verified look (glass-look §1 #4: 60 / 60 variants byte-identical).
- **Window landing r6** (`research/interactables/window_landing/images/r6_*`): main matches their "kit, no trims removal" state. Their AFTER also removes the map trims (T) and adds putty; main has neither (kits F2, window-landing VL109). Their VL107 FAIL (intact glass does not read as glass) holds in my frames: from the hall, Office windows read as flat bright boxes at 4.5–16.5 m.
- **Level transitions** (`research/level_transitions/images`): main's B0 matches their B0 render (\|dY\| 0.14 against pre); main's V5 is colour only and does not match their V5 render (above; MAP-1).
- **G14** (`research/glass/rt/images`): none of their frames shows a print wallpaper reflected, because they predate the print layer. V2 is new.

---

## 6. Notes for other tracks (not findings of this review)

- **window-landing / interactables-kit:** at 0.3 m the map's trim box reads as a second casing step behind the kit frame on every head close-up (W-OF most, `win1_OF_room_head_0.3m`). Kits F2 owns it; the r6 "T" diff removes it.
- **level-transitions / Red:** `images/vis_sheet_transitions_v5.jpg` is the first render of what main ships (V5 colour only, B0 on) next to the V5 and B0 renders in Figma.
- **glass owner:** title rooms +1.0 to +3.8 luma from the Level 0 zone cube (glass-look F3).

---

## 7. Verification images (Figma, FRONTROOMS · VISUAL VERIFICATION LOG `2595:6093`)

| VL | Slide | Check | Verdict | Images |
|---|---|---|---|---|
| VL151 | `2814:6285` | Main vs pre-Codex, 84 frames | FLAG | `images/vis_sheet_windows.jpg` · `images/vis_sheet_title_rooms.jpg` · `images/vis_sheet_map_rooms.jpg` |
| VL152 | `2814:6298` | Key panel off-screen | FAIL | `images/vis_hud_key_panel.jpg` |
| VL153 | `2814:6309` | G14 paints wallpaper red | FAIL | `images/vis_rt_print_wallpaper_red.jpg` |
| VL154 | `2814:6320` | CRT ad shows a real brand | FAIL | `images/vis_crt_ad_ingame.jpg` · `images/vis_crt_ad_file_sheet.jpg` |

The section grew to 8,280 × 48,440 (39 rows); the cover (VL000) now reads 22 tasks · 153 checks · 477 images, and the codex-audit legend lists VL080–126, 151–154 (16 checks).

---

## 8. Files

- **In Red's project (docs only):** this report; `images/vis_sheet_{title_rooms,map_rooms,windows,transitions_v5}.jpg`, `images/vis_hud_key_panel.jpg`, `images/vis_rt_print_wallpaper_red.{jpg,json}`, `images/vis_crt_ad_{ingame,file_sheet}.jpg`, `images/vis_sheet_stats.json`; `vis_F1_key_panel_anchor.contract.diff`, `vis_F2_rt_print_albedo.diff`.
- **Work root:** captures `W/cxv/{run1_279c144,run2_6c6fe81,run3_6c6fe81_rtoff,run4_rtfix,run5_hudfix}/cxvis`, pre `W/proj_cx_pre/Verification/cxvis`; pairs and heatmaps `W/cxv/pairs/`; RT on/off `W/cxv/rt/`; transitions `W/cxv/trans/`; tools `W/cxv/{compare,quickstats,sheets,rtpair,trans_sheet}.py`; logs `W/logs/cxvis/`.
- **Clone state:** `W/proj_cx_vis` is back to HEAD's files (both patches reverted after their runs).
