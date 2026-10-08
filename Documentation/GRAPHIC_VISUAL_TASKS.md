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
