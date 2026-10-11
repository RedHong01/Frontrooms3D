# 平面视觉 (graphic visual) — work list

Owner: 平面视觉 chat. This is separate from 游戏视觉's `VISUAL_CHAT_TASKS.md`. Read it on resume and update status here.
Last update: 2026-10-07 evening. Items 1–3 are open; see each status line.

## 1. In-game wallpaper cue: IN PROGRESS
Red, 2026-10-07: "先把其中一套应用在游戏里，和关卡设计还有系统设计协作构思这个逻辑在游戏里什么时候启用，以及给我一个游戏内渲染的预览效果视频"

**Set chosen for the game:** A + M4 + V1.
- A: every arrow turns 90° and is fitted to its band (field scale 0.654, motif 0.255).
- M4: ratchet, turning in 30° clicks.
- V1: stepping with 2 speed systems; every other row shares one system.

**Demos:** `Tools/print/ink/art_from_graphic/cue_states/v2_motion/`. Scripts are `motion_demo.py`, `tiling_demo.py` and `tiling_staggered.py`.

**Status 2026-10-07 evening: the flipbook is built and the in-game render is running.**

**Flipbook:**
- Built by `Tools/print/ink/art_from_graphic/cue_states/cue_flipbook.py` into `.../cue_states/flipbook/`: 17 encoded 2048² slices, C00–C16.
- C00 is bit-identical to Q1b's K00.
- C01–C08 turn and step toward +u; C09–C16 do the same toward −u.
- V1 stepping: the upper chevrons tick 125 mm per click; the lower chevrons hop 375 mm on clicks 2 and 5. One loop is 6 clicks = 750 mm.
- Previews: `flipbook_states.png` and `flipbook_timing.mp4`.

**Clone:** `~/FrontRoomsVisualWork/proj_pgcue`.
- Its `FrontRoomsSurface.shader` carries a prototype `CueInk`: a per-cell 2-slice crossfade read from `_FR_WayCells` (RGBA8 64×64: R from, G to, B fade, A dir + 4·msg), with `_FR_WayOrigin` and `_FR_Cue`, sharing `sampler_FR_Print`. The FaceSign picks the +u or −u set.
- Its `_PrintTex` is Q1b's K00.
- The harness is `Assets/Editor/CuePreview/FrontRoomsCuePreviewCapture.cs`. It runs a simulated order: Warn bursts → stage-1 turn → chase wave at 8 m/s with lamp Dips → search retract.
- Frames go to `Verification/cue_preview/{A,B}`.

**Design choice:** the CPU writes per-cell state, so all timing rules (系统设计) stay in C# and the shader stays a two-slice player.

**系统设计 rules (RELAY_PURSUIT_REDESIGN v2):**
- Stage 1: arrows turn only. FLOW route only. Only the player's room set, never the Relay's room or within 2 cells of it.
- Chase: the stepping wave starts from the player's room at 8 m/s and may switch to the pressure route.
- Tempo is fixed.
- On Chase→Search, retract with a 2–3 s reverse.
- Reduce Motion: turn only.

**关卡设计 reply (2026-10-07):**
- FrontRoomsWayfinding does not exist yet.
- Proposal: per-cell data texture `_FR_WayCells` (RGBA8, 64×64) plus `_FR_WayOrigin`, both globals, read with `LOAD_TEXTURE2D`. Each texel holds message, flow direction, phase offset and a lit/dark gate.
- FaceSign = sign(dot(routeDir, cross(up, n))). STOP = flatten.
- Lamp gate: the cell hints when LampBaseLevel < 0.55, or during an override flicker below 0.55. It releases once the level stays ≥ 0.8 for about 1 s.
- What counts as an "exit" is open; Red or the narrative chat decides.
- Nothing gets built until Red and all three chats agree.

**Preview status (2026-10-08 00:2x):**
- **v1** (a hall with pillars, pass A only) was delivered to Red: `Research/week03/wallpaper_cue/cue_ingame_preview_v1_hall.mp4` (outside the git repo). Frames: `~/FrontRoomsVisualWork/pgcue_out/v1_hall/`.
- **v2** is rendering: a real corridor with walls on both sides, plus pass B (an oblique look along one wall).
- **Encoder:** ffmpeg plus pre-rendered caption PNGs. This ffmpeg has no drawtext, and per-frame PIL is too slow on the loaded machine.
- **VL slide:** due after v2, in `research/cue_driver/images/`.

**BLOCKER RESOLVED (2026-10-07 23:4x):** the cause was my own harness. Its Chinese text and C# tuple syntax made FrontRooms3DGame's MonoScript resolve NULL. It was rewritten ASCII-only and tuple-free; the render now runs in `proj_pgcue2`. History of the blocker:
- In clones `proj_pgcue` (from proj_audit + rsync main) and `proj_pgcue2` (from `proj_g14b`), `MonoScript(Assets/Scripts/FrontRooms3DGame.cs).GetClass()` is NULL. The scene's game component is MISSING, so the game never starts; the harness logs a DIAG.
- Other chats' clones run fine tonight: proj_cx_rt*, the touch chat's proj_touch, and `proj_g14b_r3`. Note that the working g14b run was `_r3`, not `proj_g14b`.

**Next try:**
1. Clone a known-good project (`proj_g14b_r3` or the touch clone, when its Unity is idle).
2. Run `FrontRoomsCuePreviewCapture.CheckGameScript` once *before* adding anything, then add the harness, the shader patch and K00 one at a time.
3. When the class resolves, run `RunBatch`, then `cue_ingame_video.py <clone>/Verification/cue_preview <out.mp4>`.

**Driver doc** `research/cue_driver/CUE_DRIVER_INTERFACE.md` is at v0.2 (系统设计 §9.6 + 关卡设计 answers).
- TODO: replace the "both tick paths" wording with 关卡设计's definition: its own LateUpdate on game time after MapWorld and the hunter, plus `TickForTools(dt)` for captures.
- TODO: answer narrative's two additions to the K00 hold rule: (a) cues also start from stage-0 lamp sags or dying lamps; (b) ambient resumes only at the burst lamps' restore slot.
- TODO: write the `FrontRoomsCueTiming` static table.

**Next:**
1. ~~Write the flipbook generator.~~ Done.
2. ~~Make the clone.~~ Done.
3. Render the harness (running), then build the MP4 for Red: pass A down the corridor, pass B oblique along a wall.
4. Send the driver interface and event list to 系统设计 and 关卡设计. Base it on 关卡设计's `_FR_WayCells` proposal, but with per-cell from/to/fade written by the CPU. Answered in principle on 2026-10-07.
5. Send the `CueInk` prototype to 游戏视觉 as the shader spec. They land the shader once Red and all four chats agree.

## 2. Wallpaper = hard-edge only: ROUTED to 游戏视觉 Q1b (lands tonight per 游戏视觉)
Red, 2026-10-07: "当前游戏的墙纸纹理完全以hardedge 为主，不要当前有纹理干的stroke了"

**Found:** the "dry stroke" is the OLD chevron print in main's `_PrintTex`, `Assets/Resources/Surfaces/Textures/Wallpaper_Print_P.png` (2048×3072, 10-03). Its ragged ikat edges are the stroke. The paper M/N/S maps are not the cause.

**Fix = 游戏视觉's Q1b:**
- the 2048² hard-edge K00 goes into `_PrintTex` with the GUID unchanged;
- plus the `Resources/Print/FR_Print_HardEdge` array;
- desktop BC5, WebGL R8G8 1024.
- Build and verify passed on 10-04; the package was lost in the 10-05 reboot. 游戏视觉 resumed `cont-q1-live-print` on 10-07 and put it first in their merge queue.

**My part, done:**
- At 游戏视觉's request, the staged importer `Tools/print/unity_staging/.../FrontRoomsPrintImporter.cs` now skips print sheets: `FrontRoomsPrintArray.IsPrintSheet`, which arrives with Q1b. Promote it only after Q1b.
- I noted the change in `Tools/print/README.md` and told the wallpaper/narrative chat, which owns that folder.

**Check after it lands:** a before/after frame of Level 0 for Red.

## 3. TV / computer screens: ANALYSED, plan written
Red, 2026-10-07: "之前chat开始做了一个电脑/电视机广告的task…把它接入到游戏中一整套视觉系统里"

**Analysis and plan:** `Documentation/SCREENS_VISUAL_SYSTEM.md`.
- **Fixes required:**
  - P1: the real LEVITZ trademark on card 1.
  - P2: macOS Arial and Courier type, overflow, illegible secondary lines.
  - P3: no CRT layer.
  - P4: every set is on, on one channel.
  - P5: Codex audit F6, `Shader.Find` fails in player builds.
  - P6: anchors hard-coded to `Kit_CRTMonitor`.
- **System:** three layers per screen, from the research finding "agency spot + station CG":
  - programme (agency type);
  - station (CG face, ID, stand-by and emergency slates, 4:3 safe areas);
  - tube (shader by 游戏视觉).
- **Warning language:** the screens join it with the same events and timing as the wallpaper cue: interference at Warn, emergency slate on the chase wave, roll back on Search. This is a proposal for Red.

**Next:**
1. Figma style frames (editable layers).
2. `Tools/screens/` generator: the four v0 cards rebuilt in the system, with a fictional brand from the narrative chat and Period1990 faces.
3. A tube look test in a clone.

**Owners:**
- strings: narrative chat;
- tube shader, F6 fix and kit anchors: 游戏视觉;
- events: 系统设计 / 关卡设计.

## 3a. Instagram promo (Red, 2026-10-08: "一个电视机在后室的场景里面，播放一个又一个你做的广告HDR"): v4 DELIVERED
**v4 (02:50), `FrontRooms_promo_WXRM_v4_{HDR10,SDR}.mp4`: no camera sway.** Red: "我不希望镜头摇晃，直接平滑拉近就可以了".
- The camera rides one straight line through the screen centre with a constant aim, so the set stays centred and only grows.
- 2.0 m → 0.80 m from the screen, smoothstep over 0–23.4 s, ending on v3's close-up (camera 1.05 m high). The v3 sway (2.5 cm / 1.2 cm sines) and its 27 cm crane-down are gone.
- Check (`Verification/promo/camera_path.csv`, VL214 `2877:6151`): ≤ 0.005 mm off the line, one forward vector, within 0.12 mm of the ideal smoothstep.
- Everything else as v3 (SS fix, mix). VL213 `2875:6165` logs the SS fix.

**v3 (2026-10-08), `FrontRooms_promo_WXRM_v3_{HDR10,SDR}.mp4`: the SS relay fixed.** Red: "当前后面的接力的两个ss的渐变不对", then "白色和黑色本质上是一个在白色背景下另一个在黑色背景下". Same shot, cut and mix as v2.
- **Rule (Red's own variants in Figma):** each relay S fades from the background colour into the logo's ink. S1 reaches the full ink; S2 reaches 25 %.
  - black background, white logo: white α0 → white (the game SVG; icon 2687:10459);
  - white background, black logo: white α0 → black (Week 1 lockup 2235:44);
  - yellow background, white logo: icon 2687:10454;
  - black background, yellow logo: black → yellow (icon 2687:10464);
  - pattern or photo background: transparent start, so the image shows through (icon 2687:10469).
  - The promo's set is a black screen, so the white version was already the right one.
- **The real fault (v1/v2): the S had no gradient at all.** The clone's batch import left both VectorImage gradient atlases uninitialised (128×128 of 0xCD, so every texel was 205/205). Both S drew as a flat ~49 % white.
  - The real project's import is correct (Library artifacts 0b361b3a…, 5353d629…), so the in-game title has its ramp. It does blend in linear light onto the screen, though, so its partial alphas read lighter than in Figma (not checked in-game; Red's call).
  - The harness now rebuilds the ramp from the SVG stops (`RepairLogoAtlas`).
  - It also composites the card in signal space (UNorm target, tube decode `_SignalDecode`), as Figma and a station's character generator do. Linear light would lift S2's 25 % to ~50 %.
  - Card check: S1 51/75/94/114 against the design's 53/74/94/115; S2 ends at 50 against 49.

**v2 (02:10), `Research/week03/instagram_promo/FrontRooms_promo_WXRM_v2_{HDR10,SDR}.mp4`, 24 s, with sound.** Red's notes, all done:
- camera closer and the set on a table (Kit_Credenza);
- the logo plays on the set, with the title's own motion logic. These are the game's SVG lockup layers via UI Toolkit to a RenderTexture, on the `UpdateLogoMotion` formulas: wordmark fade 1.2 s, then the S1/S2 relay on the 0.9 s door clock;
- a CRT texture (slot mask, beam-shaped scanlines, convergence error, rounded tube face);
- glass reflection (probe Fresnel scaled by the live lamp level, lamp highlights, GlassGrime smudges);
- bokeh DOF focused on the set.

**Sound:** `promo_audio.py`; notes in `MIX_NOTES.md`, stems in `audio_stems/`.
- The game's Lobby ambience (CC0) follows the lamps.
- Synthesised TV power, channel clunks, static, interference and power-off.
- WXRM ident and FRONT ROOMS jingle (MIDI rendered by `midi_render.swift` through the system GS synth), a 520 Hz temporal-3 alarm, a logo hit and S-relay whooshes.
- −14 LUFS / −1 dBTP.

**Not done (needs Red):** an announcer voice; the macOS TTS licence is unclear.

**Disk:** proj_pgcue2 (19 GB) holds the promo frames until Red signs off; then trash `Verification/promo`.
- **Shot:** Kit_CRTTV on the carpet of a Level 0 room against the hard-edge wallpaper. The set plays the WXRM loop from `Tools/screens` (station ID → 4 cards). Then interference with lamp Warn bursts, the EXIT-green alarm slate as the lamps die, power-off, and the FRONTROOMS wordmark end card. 9:16, 1080×1920, 30 fps, ~23.5 s, silent.
- **Pipeline:**
  - Clone `proj_pgcue2`, harness `FrontRoomsPromoCapture`.
  - Tube look from the clone-only shader `FrontRooms/PromoScreen`.
  - Capture renders HDR with tonemapping off, then `Hidden/FrontRooms/PromoEncode` writes 16-bit PQ/Rec.2020 and 8-bit ACES/sRGB per frame.
  - Encode with `Tools/screens/promo_encode.py`: HDR10 HEVC Main10 plus SDR H.264.
- **Output:** `Research/week03/instagram_promo/FrontRooms_promo_WXRM_v1_{HDR10,SDR}.mp4`. 23.5 s; screen peak about 600 nits. Red's notes so far (camera closer, TV on a table) are in v1: Kit_Credenza, push 2.0 → 0.85 m.

**Held for Red (blocked by the permission classifier 2026-10-08, not worked around):**
1. Swap the in-game ad `Assets/StreamingAssets/FrontRooms_Ad_01.mp4` (the real LEVITZ trademark, must not ship) for `Tools/screens/out/FrontRooms_Ad_02/FrontRooms_Ad_02.mp4`.
2. The request to 游戏视觉 for the tube shader, the F6 `Shader.Find` fix and the Kit_CRTTV anchor. The message text is in this file's §3; Red can forward it.

## 3d. Desktop UI masters + flows (Red, 2026-10-08: "当前mac/windows 的UI呢？我也需要你在figma里把所有 frame的 mockup也都给我做出来"): DONE
Every Mac/Windows screen as the game draws it today (main bc0aa79), rebuilt as editable 1920 × 1080 Figma masters. Report: `research/desktop_ui/README.md`.
- **DESKTOP UI** `2911:6716` (x 103377, y 2000):
  - the HUD·D / MENU·D kit;
  - 18 masters `SCR / Desktop / D01–D17` (+ D16b), each the captured scene with the UI as layers.
- **DESKTOP FLOWS** `2920:6693` (x 94697, y 2000, next to TOUCH FLOWS): five lanes, the key on each arrow, a way back under each screen, and the desktop findings.
- **Captured** in clone `W/proj_deskui` (harness `FrontRoomsDesktopUICapture`, prefs isolated). It drives every state through the game's own fields.
- **Checked line by line** against the game: tops within 1 px, frames mean 2/255 (VL216 `2922:6863`).
- **Serif:** Source Serif Pro stands in for the game's Source Serif 4 at optical size 20. Figma's Source Serif 4 has no opsz axis.
- **Findings, Red's call** (VL217 `2922:6873`):
  - ESC RESUME and R TRY AGAIN measure 1.11 : 1 on the wash;
  - RELAY / CHASE is clipped (36 px line in a 34 px box);
  - Bayon has no ·, and draws 0 as O;
  - no restart question on desktop;
  - no mouse on menus;
  - LOCKED is unreachable (doorsNeedKeys off).
- **Disk:** `W/proj_deskui` stays for re-captures.

## 3e. Touch run gaits, strings and TITLE PAGE review (touch chat, 2026-10-08): DONE, applied
- **Applied by the touch chat; checked on Pause master 2532:5112:** QUIT TO TITLE as a dark chip, 109 pt (the measured fit), centred 16 pt under the row. Pressed, it lightens 12 %.
- Playtest run 20: 73/73 on iPhone and Android 20:9.
- The hint wraps to two lines on every handheld (fixed 410 × 52 pt box).
- The question card (LEAVE THIS RUN?) is with Red as open question 10.
- **Approved:**
  - stick gaits (Touch / Stick 2530:3828, TC16 2907:6556, VL218). The dashes and approach ramp are text-free ghosts, which §3b allows; the RUN / WINDED labels are on the solid #0E0E0D chip.
  - Settings row RUN with STICK / BUTTON.
  - Pause legend "PAST THE RING  ·  RUN" and its new glyph.
  - First-run hint "Push past the ring to run: …" (touch chat to confirm two lines at Android 20:9).
  - RUN button.
- **Changes asked for the new pause chip:**
  - "TITLE PAGE" becomes "QUIT TO TITLE" (about 130 pt wide).
  - Solid dark chip (#0E0E0D, paper Bayon 17), not yellow.
  - 16 pt gap below the row, matching the horizontal chip gap; centred.
- **Flag for Red:** it ends the run without the question RESTART asks. Suggested "LEAVE THIS RUN?" with CANCEL / QUIT.
- The Bayon "·" fallback applies on touch too; the separator decision rides on the desktop fixes card (VL217).

## 3f. EXIT sign lettering review (游戏视觉, 2026-10-08, after the merge into main 19:5x): LETTERING APPROVED · COLOUR NOT YET
- **Lettering approved as built:**
  - geometry, not a font: E/T 0.354 H, X 0.394 H, strokes 1:8, square ends, stamped R 0.5 corners;
  - spacing optically even, measured on the polygons (mean gaps E–X 19.8, X–I 19.3, I–T 20.3 mm; optional +1 mm on X–I);
  - no bridges needed in EXIT;
  - the chevron knockouts' 0.3 mm score with four bridges is right.
- **Colour not approved:**
  - Measured in the graded frames: 0.3 m (199, 81, 57), hue 10°, saturation 0.71.
  - Lit Lobby at 3 m (196, 145, 123), hue 18°, saturation 0.37: salmon, with contrast to the face 1.66:1 (≈ 0.40 < UL 0.5).
  - Target sent (SDR graded, letter body):
    - hue 0–8°;
    - saturation ≥ 0.80 dark/close, ≥ 0.6 at 3 m in a lit room;
    - face contrast ≥ 0.5;
    - hot spots may warm toward orange or yellow-white, never pink.
  - 游戏视觉 fixes the rendering and sends 0.3 / 3 / 6 m captures, lit and dark, for my re-check.
  - **exit_sign_fix1 (agreed 10-08):**
    - dropped the planned ×3 emission, which would push the letters deeper into the ACES highlights;
    - letter body back to the mid-curve, maybe scaled with exposure;
    - diffuser transmission = the sRGB red primary;
    - less red spill on the ceiling. Moving the light into the sign with no shadow is an A/B for Red.
    - Measured after the tonemap. Deliverables in `research/exit_sign/images/fix1/` with measurements, plus the Run room at 6 / 12 m. My re-check follows.
  - The K-sheet `index.json` for the 8 kits is rendering; I place them from K82.
- **Review:** the stencil-cut face, 6 in letters, 3/4 in strokes, and the enclosed chevron with stencil bridges.
- **Sources:**
  - `research/exit_sign/20_build.md`, `30_integration.md`;
  - images in `research/exit_sign/images/build/` (es_b01 at 0.3 m, es_b04 near/far);
  - textures `Assets/Resources/Surfaces/Textures/ExitSign_*` (Face 2048×1024).
- **Colour:** at 3 m the lit letters read salmon-red after the tonemap (游戏视觉 asked whether to review it together).
- **Next:** K-sheets for the 8 kits (Kit_ExitSign, Hanging, HangingFlush, End, each with _Dead), once 游戏视觉 delivers `research/exit_sign/threeview/index.json`.

## 3c. Placard legend in the game's colours (游戏视觉 request, Q16, 2026-10-08): DONE
- **Adopted** their candidate: the WP03 swatches now use the Lobby print colours (encode_table → print_tool.shade).
- `legend_swatches.py` v3: the default is the game palette; `--reference` gives the old palette.
- New `placard_print_lit.png`: md5 5f71409a…, identical to their candidate. The glow mask is unchanged.
- Figma T24 2504:75 / 2504:76 updated.
- **In game (游戏视觉, 2026-10-08):** `Prop_EvacPlan_A.png` repacked (4096×2048, GUID unchanged); the glow mask is byte-identical; dark legibility carried over (1.37 / 1.32, the same md5 as the measured candidate).
- **K-sheet: DONE 2026-10-08.** **K81 · Evacuation plan** `2948:6863` in PROP KIT (2160, 24960), placed from `research/placard/threeview/index.json` (v3, `pending` gone).
  - Slots `img:Kit_EvacPlacard_{front,side,top,persp}` (`2948:6901/6902/6903/6882`), PNGs at 50 %.
  - Paper swatch #e7ded4; the lens is shown as an outlined empty swatch (clear).
  - Lede trimmed to two lines on the sheet.
  - Every K-sheet's page count is now / 82.

## 3b. Touch type review (touch chat, 2026-10-07): DONE
- Approved:
  - solid ink for the pause meta line and the caught labels;
  - an opaque settings card;
  - the stick SPRINT/WINDED label on a solid ink chip, #0E0E0D, 4/7 pt padding, no shadow. Audit passed: 14.3 : 1 and 17.2 : 1.
- **USE disc:** solid whenever it carries a verb; CONTROLS OPACITY scales only text-free ghosts.
- **TAP TO START:** static on the chip; the alpha breathing was dropped.
- **Rule:** no translucent ink or surfaces in in-game UI, because Unity blends UI alpha in linear light.

## 4. HUD KEY FINAL re-sync: queued
Waiting on 游戏视觉 FINAL (`03_figma.md` §0). Tools are in `Tools/figma/hud_key/`.

Steps:
1. Re-import the 13 SVG masters into KV-LIB, keeping IDs.
2. Rebuild the 114 twins from plan3_fit.
3. Add the 15 missing twins and the leaf background.
4. Rebuild the 4 Achip frames and add `A_L_fullframe_target_wallpaper`.
5. Update the KV03/KV04 text.
6. Recenter KV01.
7. Reply to 游戏视觉 with these decisions: prompt card ✓, yellow rule only for keys held this zone, the "used" column kept with a conditional label.

## 5. Waiting on others
- **Red:**
  - pick among M1–M4 and V1–V3 (the game uses A + M4 + V1 meanwhile);
  - the exit definition;
  - narrative §8;
  - the placard strings "AS PRINTED" and "WALLCOVERING SHOWN AT 1/25 SIZE".
- **游戏视觉:** hand-offs for the outlets and the placard three-views.
- **DOORS + WINDOWS section 2497:3804:** don't occupy it until Red restores it.
- **WhatTheFont:** needs Chrome connected.
