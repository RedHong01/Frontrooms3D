# 平面视觉 (graphic visual) — work list

Owner: 平面视觉 chat. This is separate from 游戏视觉's `VISUAL_CHAT_TASKS.md`. Read it on resume and update status here.
Last update: 2026-10-07. The usage limit was reached mid-task; items 1–3 are open.

## 1. In-game wallpaper cue: IN PROGRESS
Red, 2026-10-07: "先把其中一套应用在游戏里，和关卡设计还有系统设计协作构思这个逻辑在游戏里什么时候启用，以及给我一个游戏内渲染的预览效果视频"

**Set chosen for the game:** A + M4 + V1.
- A: every arrow turns 90° and is fitted to its band (field scale 0.654, motif 0.255).
- M4: ratchet, turning in 30° clicks.
- V1: stepping with 2 speed systems; every other row shares one system.

**Demos:** `Tools/print/ink/art_from_graphic/cue_states/v2_motion/`. Scripts are `motion_demo.py`, `tiling_demo.py` and `tiling_staggered.py`.

**Preview path (needs no shader change):**
- `_FR_Print` is a Texture2DArray RG flipbook, encoded with hard_edge `encode_table` at 1024×1536 per roll.
- `_FR_PrintClock`: x = frame, y = number of slices, z = per-roll phase, w = live mix.
- Snap look: each state gets 4 slices and x advances fast.

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
1. Write the flipbook generator (encoded RG slices).
2. Make a clone per `~/FrontRoomsVisualWork/TOOLS.md`. Never run Unity on Red's project.
3. Build a play-mode harness from the room_visuals capture: seed 4242, Relay dormant, simulated order (lamp flicker → turn → step). Render 1920×1080 frames to MP4 for Red.
4. Send the driver interface and event list to 系统设计 and 关卡设计.
5. Send the shader snap flag and the per-cell texture read to 游戏视觉, who owns FrontRoomsSurface.

## 2. Wallpaper = hard-edge only: NEW, not started
Red, 2026-10-07: "当前游戏的墙纸纹理完全以hardedge 为主，不要当前有纹理干的stroke了"

Goal: drop the textured dry-brush stroke layer from the in-game wallpaper and keep the flat hard-edge bands (WP03 hard_edge geometry and palette).

Steps:
1. Find where the stroke lives: albedo, normal or print slices. See also the "ink baked into N/S" note in `research/wallpaper_motion`.
2. Find out who owns the texture: 游戏视觉 (kit/shader) or my print tools.
3. Regenerate in a clone and render before/after.
4. Red approves before anything goes to main.

## 3. TV / computer screen-ad pipeline: NEW, intake only
Red, 2026-10-07: an earlier chat started a TV/computer ad task so the in-game screens actually show content. Red wants it on this list: analyse it, revise it, and connect it to the game's whole visual system.

**Status:** the survey was stopped before it reported. Its last note says the screen-ad files are in git commits.

**Re-run the survey for:**
- task definitions and existing media and tools;
- which props have screens and how those screens are set up today;
- the owning chat and its status;
- constraints: era ≤1993; period ads are research only and must not ship; WebGL is its own tier; desktop is the high-spec reference.

**Then:**
- revise into the house system: type through 平面视觉 rules, palette, CRT treatment;
- plan integration with 游戏视觉 (materials) and 关卡设计 (placement).

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
