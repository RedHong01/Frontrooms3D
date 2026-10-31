# 平面视觉 (graphic visual) — work list

Owner: 平面视觉 chat. This is separate from 游戏视觉's `VISUAL_CHAT_TASKS.md`. Read it on resume and update status here.
Last update: 2026-10-07 evening. Items 1–3 are open; see each status line.

> **2026-10-15: 平面视觉 is split into three sub-chats** (Red): Information Design, Advertising and Prints / Presentations. `GRAPHIC_VISUAL_SPLIT.md` says which sub-chat owns each item below and who owns which part of the type system. The items keep their detail here.
>
> **UI documentation DONE 10-15.** Figma section 3178:7075 "FRONTROOMS · UI MOCKUP · CONTINUED · UI SYSTEM" (x 7897, y −6800) holds UI10–UI19: the HUD key B, crosshair states, P4, L3·D + D·3, U·3 + F·1, B·4, legibility + prompts, desktop + phone, sound pass 1, open items. The index board 3180:7299 sits in "UI MOCKUP" 2256:8.

## 0. Information Design (信息设计) since the split, 2026-10-15

- **Audit of 系统设计's SD00–SD08 (3171:7042): SENT 10-15.** The derivation is right; the fixes are in encoding and alignment: yellow call cells instead of a 2 pt bar, three-class status tags, IDs on the baseline, one 14 pt grid, 12 pt gutters, hanging zebra, row heads in SemiBold, a drawn footer key, one sheet ID and "you". SD00: the bars collide (three states share one look).
  - Built: 12 local styles `Info / Sheet / …` and `Info / Table / …`; component set `Info / Status tag` 3189:8204; IT00 3195:7137 (specimen) and IT01 3190:7117 (SD04 re-set) in section **FRONTROOMS · INFO TYPE SYSTEM** 3189:8187 (x 11009, y 31660).
  - Spec: `INFO_TYPE_SYSTEM.md`. Script for 系统设计: `~/FrontRoomsVisualWork/info_type/sd_restyle.js` (tested on copies of SD01, SD03, SD06).
  - **DONE 10-15:** 系统设计 applied it (script on SD01–SD08, SD00 by hand). Re-check PASS, logged as **VL285** `3198:8473`. Two SD00 nits sent: 9 texts unlinked, and the neighbours line on 720.
- **A0 system-design sheet (系统设计, after Red called the Letter sheets "像ppt"): AUDITED 10-15.** Frame `3234:7322`; their section 3171:7042 moved to x 7897, y 35860.
  - The fixes sent:
    - the map keeps group trunks only, plus a new A2 dependency matrix (DSM, 52 × 52 at 10 pt);
    - A0 size ladder: Info / Wall / Title (Bayon 120, new style), zone heads Bayon 32;
    - an empty spine column 14 with a hairline;
    - group and step numbers in Plex Mono, not Bayon;
    - G labels on knockouts;
    - D lane contrast, the CHASE label and the axis unit;
    - H as a horizontal strip;
    - the drawn key once, in the header.
  - Large-format profile: `INFO_TYPE_SYSTEM.md` §1.4. Offered to build the DSM specimen.
  - **Rev 2 (frame `3239:126`) re-check PASS, logged as VL288 `3245:7280`.** Two nits sent: H step 7 "STEP · ·", and E's rotated label in the margin. **Both fixed by 系统设计 (10-15).** The lane sort is noted for rev 3.
- **System visualization: OWNED BY 信息设计 since 10-15** (Red to 系统设计: "这部分的任务应该交给平面视觉·信息设计来负责，你只提供系统本身的元数据和信息"). 系统设计 keeps the data in `system_design/*.json`.
  - **Built 10-15: the System Atlas.** Since 10-22 it has two links, one per Claude account, both kept by Red (15:00): account A https://claude.ai/artifact/FEPpCB6X8syv17wWXE1P3y (v14, data 22z2) and account B https://claude.ai/artifact/ErXVG51q42vUcJ7ns9GGEJ (data 22z). **Updates are paused** (Red 10-22 15:04: "先不用更新，节省token"). It shows decisions first by landing step, the 8 groups, a 52 × 52 matrix, the ledger, four mechanics views; generated from the JSON (`~/FrontRoomsVisualWork/info_type/atlas/`). Spec `INFO_TYPE_SYSTEM.md` §1.5.
  - Checked: VL289 `3261:7280` (A0 at fit: cells 4.4 px; atlas 15–18 px) and VL290 `3261:7294` (0 px overflow at 406 px; dark theme).
  - **v3 on data 2026-10-15c:** 系统设计 delivered all four fields; the page now says "decisions" (the glossary's "call" is the director sending the Relay), defines terms in place, keeps answered decisions, and draws the noise ladder from data. Checked: VL291 `3265:7280`.
  - **Waiting:** Red's read of the atlas (reading order, asked 10-22 09:07). Done: C10 no longer says "Your call" (系统设计's data, 22z9).
- **Playtest deck revised (Red 10-15: white space wasted, too much type, no charts; add the new testers): DONE, waiting for Red's read.**
  - Rebuilt from one data set:
    - Week 3 · DIARY #3 `3061:6889`: anonymised, 8 slides (was 10);
    - DEVELOPMENT ARCHIVE `2440:3804`, slides 11–18: named; that deck now reads "/ 18".
  - Slides:
    - the focus question answered part by part (9-square bars);
    - what works;
    - the themes × players matrix;
    - three recorded runs;
    - players three per slide;
    - next build in rows.
  - Round two:
    - Ophelia: 161 s · 15 zones · tier 4 · 0 doors.
    - Kevin: 170 s · 7 · 2 · 1. Red confirmed the attribution.
    - Jiang: a third new tester, no run recorded.
    - Screenshot 8 is Nana's existing run.
  - GTXY's sound mark is inferred from the earlier 4/6 count.
  - Logged as VL294 `3298:7280`.
- **Look-eye: E·1 PICKED (Red 10-15, "选择这个方案").** The dot opens into an almond eye, the dot is the pupil, one slow blink on appear, then still (paper + keyline).
  - E·2–E·4 and X·0 (the staring red eye to avoid) are history on the review page.
  - Spec: `research/ui_key_icon/lock_motion/README.md` §2e. Review page v16 (Look section + the "Look at a plan" scene, desktop and phone).
  - Sent to 游戏视觉 as DECISIONS #16, and to 系统设计 for 30_crosshair_grammar.md (gate, priority, touch, Flashing).
  - **Next:** add a UI20 board to the UI SYSTEM section (3178:7075), and check the grammar doc when it lands.
- **Information-design case research (Red 10-15: "目前我没有信息设计的案例，我希望你进行research，然后自己给我找案例"): RESEARCH DONE, waiting for Red's OK to download.**
  - `~/FrontRoomsVisualWork/info_design_research/CASES_games.md`: 17 cases. The closest precedents are Penumbra 2007 (the eye = examine) and King's Quest V 1990 (the LOOK cursor is an eye). Skyrim's detection eye is the one to avoid, and it looks like E·1 at rest.
  - `CASES_world.md`: 27 cases. It answers three open calls:
    - D10: Modern DOS 8×16 is **CC0** and fits the 16×32 cell at 2×; Terminus (OFL) is the fallback.
    - D11: pager and menu are both period-true; Ceefax-style right-aligned page numbers in the index.
    - Colour: VGA blue.
  - SD-sheet notes: 7 pt heads in Plex Mono Medium; GOV.UK fills only the states that need action, and wants tag text at least the cell size.
  - Download list: `DOWNLOAD_LIST.md` (117 files, 82.6 MB). Destination `Research/week04/info_design_cases/`, outside the repo.
  - Next, after Red's OK: download, then the case board in Figma, then `SOURCES.md`.

## 0b. Prints / Presentations (印刷与演示) since the split, 2026-10-15

- **Playtest answer sheet (Scantron edition, FR-PT 05): BUILT · waits for one real scan test.**
  - **Red 10-15:** turn the playtest form into a Scantron sheet; research US Scantron design and the simplest machine entry; then build it in the brand's language, "like information design".
  - **Research:** track D (`research/print_reference/SOURCES.md`).
  - **Shape:** box edition. Red rejected ovals ("不是特别喜欢椭圆形"), then the wallpaper arrow ("箭头没办法fit到网格里面").
  - **Figma:** canary 3290:7280 and white 3290:7602 in section 3198:7273; boards PF03 3295:7280 and PF04 3285:7280.
  - **Reader:** `Tools/playtest_scan/`; its README is the manual.
  - **Simulated scans:** 560/560 fields (VL292 ovals, VL293 boxes).
  - **InDesign print master (Red 10-15: "答题卡我也需要你给我做成indesign文件，同时rich black"):** `Print/FR-PT05_Playtest_Answer_Sheet.indd`, built natively from 3290:7602; every black is rich black, the scanner geometry is exact, 280/280 read (VL296, `PRINT_FORM_SYSTEM.md` §10.1).
  - **Red, 2026-10-22, "全部按推荐":** Caption yes, FR-PT04 may be overwritten, canonical FR-PT05 is the master (dated copies archived), §7 / §9.7 accepted. Also: "during play的section在未来的版本就不需要了".
  - **REV. 10-22-26 done (2026-10-22, VL352):** During play removed, look-eye item 05 added (wording final), cue on one row.
    - Figma section 3510:7478.
    - Reader template regenerated; 280/280 on every simulated route.
    - Both InDesign files rebuilt in rich black. Printable PDFs are in `Print/`; the 10-15 files are archived in `Print/_archive_REV-10-15-26/`.
  - **Final InDesign build, 2026-10-22 13:46–13:49 (VL372, PASS):** Source Serif 4 Variable at opsz 10 (Figma's own optical size; Adobe Fonts' Caption never synced) and the cue-dot spacing fixed. Both forms break 17/17 serif lines as in Figma; reader 280/280. The 10:24 Text-cut files are in `Print/_archive_REV-10-22-26_text-cut/`.
  - **Next:**
    - Print the new PDF on canary stock, fill it in pencil, scan it and run the reader (plan in the 印刷与演示 handoff).

## 1. In-game wallpaper cue: IN PROGRESS
Red, 2026-10-07: "先把其中一套应用在游戏里，和关卡设计还有系统设计协作构思这个逻辑在游戏里什么时候启用，以及给我一个游戏内渲染的预览效果视频"

**Status 2026-10-22 (平面视觉 · 信息设计):**
- **The set is decided: A + M4 + V1.** Red picked M4 + V1 on 2026-10-22 at 15:05, in the wallpaper line's chat, after the twelve in-game combination previews (earlier that day Red had asked to see the other options again). The flipbook C00–C16 is final.
- **Where the arrows point is decided:** C24 = A + A20. They point at the station TV until the floor key is taken, then at the floor EXIT. The map-side rules are `research/cue_driver/MAP_ROUTE_SPEC.md` (关卡设计调研).
- **Done:** the v1 and v2 in-game previews (`Research/week03/wallpaper_cue/`, 10-08; VL193 PASS). The driver doc is v0.2.2; the three TODOs listed below were done in v0.2.1 (10-07).
- **Nothing is in main yet.** Left: the map side (关卡设计, per MAP_ROUTE_SPEC) and `CueInk` (游戏视觉). Red, 10-22 via 总控: finished assets go in without waiting for the Alpha; 总控 sequences them. CueInk alone is inert (every cell stays state 0).
- Everything below is the 10-07/08 record.

**Set (decided 2026-10-22):** A + M4 + V1.
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

**Clone:** `~/FrontRoomsVisualWork/proj_pgcue2` (the first clone, `proj_pgcue`, is gone; see the blocker note below).
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
- **v2** (a real corridor with walls on both sides, plus pass B, an oblique look along one wall): done 10-08 00:33, `Research/week03/wallpaper_cue/cue_ingame_preview_v2_corridor.mp4`.
- **Encoder:** ffmpeg plus pre-rendered caption PNGs. This ffmpeg has no drawtext, and per-frame PIL is too slow on the loaded machine.
- **VL slide:** VL193 (Figma 2852:6206), PASS.

**BLOCKER RESOLVED (2026-10-07 23:4x):** the cause was my own harness. Its Chinese text and C# tuple syntax made FrontRooms3DGame's MonoScript resolve NULL. It was rewritten ASCII-only and tuple-free; the render now runs in `proj_pgcue2`. History of the blocker:
- In clones `proj_pgcue` (from proj_audit + rsync main) and `proj_pgcue2` (from `proj_g14b`), `MonoScript(Assets/Scripts/FrontRooms3DGame.cs).GetClass()` is NULL. The scene's game component is MISSING, so the game never starts; the harness logs a DIAG.
- Other chats' clones run fine tonight: proj_cx_rt*, the touch chat's proj_touch, and `proj_g14b_r3`. Note that the working g14b run was `_r3`, not `proj_g14b`.

**Next try:**
1. Clone a known-good project (`proj_g14b_r3` or the touch clone, when its Unity is idle).
2. Run `FrontRoomsCuePreviewCapture.CheckGameScript` once *before* adding anything, then add the harness, the shader patch and K00 one at a time.
3. When the class resolves, run `RunBatch`, then `cue_ingame_video.py <clone>/Verification/cue_preview <out.mp4>`.

**Driver doc** `research/cue_driver/CUE_DRIVER_INTERFACE.md` is at v0.2.2 (2026-10-22: route pointers to MAP_ROUTE_SPEC.md, motion marked unconfirmed). The three TODOs were done in v0.2.1 (10-07):
- ~~TODO: replace the "both tick paths" wording with 关卡设计's definition: its own LateUpdate on game time after MapWorld and the hunter, plus `TickForTools(dt)` for captures.~~ Done: §2, the driver row.
- ~~TODO: answer narrative's two additions to the K00 hold rule: (a) cues also start from stage-0 lamp sags or dying lamps; (b) ambient resumes only at the burst lamps' restore slot.~~ Done: §5 item 14.
- ~~TODO: write the `FrontRoomsCueTiming` static table.~~ Done: §6.

**Next:**
1. ~~Write the flipbook generator.~~ Done.
2. ~~Make the clone.~~ Done.
3. ~~Render the harness, then build the MP4 for Red: pass A down the corridor, pass B oblique along a wall.~~ Done 10-08 (VL193).
4. Send the driver interface and event list to 系统设计 and 关卡设计. Base it on 关卡设计's `_FR_WayCells` proposal, but with per-cell from/to/fade written by the CPU. Answered in principle on 2026-10-07.
5. Send the `CueInk` prototype to 游戏视觉 as the shader spec. Red picked the motion 10-22 15:05; the prototype went to 游戏视觉 in the 10-22 integration list.

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

## 3. TV / computer screens: Ad_03 APPROVED + RENDERED (平面视觉 · 广告, 2026-10-22)
**10-22:** Red, 2026-10-22, in the 平面视觉 · 广告 chat: "C09 选 1，其余全部按推荐".
- C09 = 1: no screens in the warnings in v1.
- Ad_03 (WTCZ-TV 61) is at `Tools/screens/out/FrontRooms_Ad_03/` and handed to 游戏视觉 for the swap.
- The tube look comes after the Alpha.
- The tutorial's still desk screens (V4) are at `Tools/screens/out/still_desk/`.

**10-15 status:** the station layer was rebuilt against 1990 research (156 sources).
- **Changes:**
  - a VP-2-class Helvetica CG (edge + shadow, whole scan lines) replaces VT323;
  - no corner bug and no time/temperature corner (both post-1993);
  - SMPTE EG 1-1990 bars with the time and date after sign-off;
  - the ID slide as the stand-by card;
  - the 61 numeral inside its ring;
  - a FRONT ROOMS dealer tag.
- **Where it lives:** `AD_TYPE_SYSTEM.md` §3; Figma 3226:7267 (Ad / styles, six masters, S09–S12); VL287; dev generator `~/FrontRoomsVisualWork/screens_dev/`.
- **Open:** Red's OK for Ad_03; narrative strings (city of licence, off-air time and date, sign-off notice, tag town); WXRM's name (WXRM-LP is a real NJ low-power FM station).

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
> **2026-10-22: Red chose option A (1): v4 is never published; v5 (station content) after the Alpha.** Earlier note, 2026-10-15 (平面视觉 · 广告): hold before posting. A24's *Backrooms* campaign (Apr–May 2026) opened with a 1990 local furniture-store TV spot ("Cap'n Clark's Ottoman Empire"; ARGNet, 27 May 2026), the same format as v4's furniture spots. If Red picks Option A (`research/ad_campaign/CAMPAIGN_PLAN.md` §8), v5 swaps the furniture spots for station content.
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
   - **APPROVED by Red, 10-14** (via 系统设计). After seeing the Ad_01 and Ad_02 file sheets, Red wrote: "保留下面的这些，上面那些只作为历史材料".
   - Ad_02 goes in the Office CRTs; Ad_01 is history only.
   - **DONE 10-14 18:17 (游戏视觉).** The file name is unchanged (`StreamingAssets/FrontRooms_Ad_01.mp4`) but the content is Ad_02: sha1 058d2e28, which I checked against `Tools/screens/out/FrontRooms_Ad_02/FrontRooms_Ad_02.mp4`.
   - The LEVITZ original is archived, with a README, in `research/codex_audit/history/`. That is outside the build but inside the git repo; flagged to Red.
2. The request to 游戏视觉 for the tube shader, the F6 `Shader.Find` fix and the Kit_CRTTV anchor. Owner since 10-15: 平面视觉 · 广告.
   - The 10-08 text was never sent. It is not in §3; it survives only in the 平面视觉 transcript (`658709b9…`, SendMessage at 2026-10-08 07:38 UTC).
   - **Checked 10-15:** F6 is fixed in main. `MakeMaterial` loads `Resources/Media/CRT_AdSurface.mat` (still untracked, so it lands with Red's next commit) on `FrontRooms/ScreenVideo`. `Shader.Find` is now only the editor fallback. Ad_02 is in too.
   - **Still open:** the tube layer (`FrontRooms/ScreenVideo` is a plain unlit video shader) and the Kit_CRTTV anchor (the anchor defaults to Kit_CRTMonitor's (0, .248, .194)).
   - **The 10-08 text is stale:** it asked for a V-hold roll on `FixtureChanged` / `LampDipped`, which the 10-14 power rule (SCREENS §3) forbids. A corrected version, tube and Kit_CRTTV only, waits for Red's go (asked 10-15).

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

## 3f. EXIT sign lettering review (游戏视觉, 2026-10-08, after the merge into main 19:5x): LETTERING APPROVED · COLOUR APPROVED 10-15 (fix1)
- **Colour APPROVED 2026-10-15 (信息设计), on exit_sign_fix1's captures** (`research/exit_sign/images/fix1/es_f03`, `es_f04`). Strict letter mask: lit 0.3 / 3 / 6 m 7.7 / 8.2 / 8.3°, sat 0.81–0.82; dark 7.4–7.5°, 0.82–0.83; hot spots 13–17° orange, 0 % pink. The lit 3–6 m body sits on the 8° bar; the rest is the room's bloom tint. The fix1 package is READY, not applied: `--apply` is Red's or 游戏视觉's call. Told 游戏视觉.
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
- **DONE 2026-10-15 (印刷与演示), from `research/exit_sign/threeview/index.json`:**
  - K56 `2653:9343` refreshed in place as the wall sign;
  - new K82 `3223:36` (hanging, 500 px/m, ceiling line), K83 `3223:83` (flush) and K84 `3227:8981` (end mount);
  - all with the index's dims, era 1977–93 "Current stock", 5 materials and the Dead variant thumbnail.
  - Red OK'd moving PROP KIT · REAL vs MODEL `2349:852` down 1240 (now y 29816) so PROP KIT `2324:852` could grow one row (27,440 high). Every K-sheet header now reads "N / 85".

## 3h. Office terminal (computer UI): G0 mock + colour and face (系统设计, 2026-10-14): G0 DONE 10-15 (信息设计), D1/D10/D11 with Red · **DECIDED 10-30:** C19 = DOS text mode + CUA lightbar, C20 = own 8 × 16 face (41_c19 spec); the G0 mocks are superseded
- **G0 boards:** section **FRONTROOMS · OFFICE TERMINAL · G0** `3218:7277` (x 11009, y 33100):
  - TM01 `3218:7278`: pager or menu (D11).
  - TM02 `3218:7292`: on the desk. The page sits on the real office CRT capture, with the LEVITZ ad erased, plus the memo READ opens.
  - TM03 `3218:7306`: how far it reads (D1). True-size crops at the stand box (0.99 m) and the aim reach (2.4 m), desktop and phone, all at one visual angle.
  - TM04 `3218:7323`: field colour (D10). VGA blue recommended, white phosphor, amber marked taken.
- **Source:** `~/FrontRoomsVisualWork/info_type/terminal/` (`g0_pages.py`, `g0_context.py`, `out/`). The pages are 640 × 480 logical, rendered at 2×. Strings are placeholders for narrative.
- **Face finding (D10):** VT323 (advance 0.4 em, cap 0.56 em) cannot sit on the 16 × 32 cell at whole pixels. An 8 × 16 bitmap face fits the spec exactly:
  - 2× = the 20 px body cap;
  - 9× = a 90 px second line (18.75 %);
  - 12× = the 120 px headline (25 %).
  - Options: draw our own 8 × 16, which raises no licence question; a permissive 8 × 16 face (licence check running); or Px437 (CC BY-SA; D10 for Red). VT323 only stands in.


The spec is `research/computer_ui/20_design.md` (Red: walk up to a computer and see its own UI). Type, colour and layout are mine.
1. **G0 mock frame, for Red's D1 and D11.** The front page seen from the back of the stand box and from 2.4 m, on desktop and on phone.
   - Raster: 640 × 480 logical, a 40 × 15 grid of 16 × 32 px cells.
   - Type: headline cap height 25 % of the screen (about 6 characters); second line 18 % (about 8); body cap 4.2 %.
   - Header bar: program name · desk user's name · the live local time (Red 2026-10-22, in 游戏视觉's chat: every clock and time display runs on real time; the frozen 10:08 is retired). US 12-hour `h:MM AM` / `h:MM PM`, right-aligned, no seconds and no blinking colon, so it changes once a minute and never reads as a timer. Any date: real month/day, year 1990, the 1990 weekday. Source: 游戏视觉's shared FrontRoomsRealTime (`research/live_clocks/`). G0 masters re-rendered 10-22 14:23 (Figma 3218:7277).
   - Footer: PAGE n OF m, with arrows drawn as shapes (VT323 has no ◄ ►).
   - D11 is Red's question: is a pager with a printed page index enough for "its own UI system", or should it be a menu program with a cursor (+6 days)? Red judges by this mock.
2. **Colour and face (§8.7).** Not the glow-ink pale yellow-green: one colour keeps one meaning. The research picks are amber or colour VGA. Px437 is CC BY-SA, so it goes to Red as D10; the spec prefers an OFL face or one I draw.
3. **DONE 10-14: SCREENS §3 amendment.** No sag, shrink or roll except on WarnBurst; no LampLevel or Warn reads; ambient follows only its cell's lamp temperament; every change has an attack of at least 1.5 s. Folded into `SCREENS_VISUAL_SYSTEM.md` §3.

## 3g. EXAMINE (close-up inspect) graphics review (游戏视觉, 2026-10-14): ANSWERED 10-15 (G1–G8, G12, G13); G9–G11 to do
- **Sent to 游戏视觉 2026-10-15 (信息设计):**
  - G1: verb + object (`E · READ PLAN`, `E · READ SIGN`, `E · EXAMINE KEY`, `E · READ SCREEN`). Asked which wins on a loose key: TAKE or EXAMINE.
  - G2: hints as built. G3: the prompt slot, not a card (DECISIONS #4).
  - G4: a Bayon fallback to the project's IBM Plex Mono through the font importer references; no new separator.
  - G5: accepted, the slot stays fixed. G6: the 1 px ink keyline, to be re-measured with the outline on. G7: `E  READ · EXAMINE  (PAUSES)`.
  - G8: exact regions from placard.swift: legend 300.0, 50.0, 110.8 × 174.5 mm (cap 2.4); plan 16.8, 54.8, 199.3 × 158.9 (cap 2.8); title block 300.0, 243.0, 116.2 × 16.0 (cap 3.6).
  - G12: Courier Prime Bold #1E1D1A; digit height = 0.43 × the insert diameter (round), 0.78 × the insert height (rect), 0.82 × (long).
  - G13: approved.
- **To do:** G9 transcript panel mock; G10 touch glyphs (READ, BACK, TEXT); G11 placard lines to 4.8 mm (a re-render when 游戏视觉 is ready to repack).

The contract is `research/inspect/contracts/graphic_inspect.md`. It is not urgent.
1. **Prompt strings.** Prompts become verb + object: `E · READ PLAN`, `E · EXAMINE KEY`, `E · READ SIGN`. These are proposals; I settle the strings and the layout.
2. **Spec review.** Review the prompt typography and the transcription panel spec, as built.
3. **G5.** At 1.5 m the prompt sits on top of the placard.
4. **G6.** The door-sign prompt is 2.2 : 1 on light plates, under 3 : 1. The likely fix is the same 1 px ink keyline Red chose for the HUD key prompts (no dark card).
5. **Legend rects.** The legend-area rects were measured off the textures. Replace them with the constants in `placard.swift`, so the zoom lands on the legend.
6. **Key object.** The key inspect object is already built as B (key on its tag). Check it against the B masters.

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

## 4. HUD key: B CHOSEN (Red, 2026-10-14) · padlock DONE · motion IN REVIEW

Red, 2026-10-14, three messages:
- "目前UI（手机和桌面）的钥匙扣相应的方案全部都确定为📄继续深化这种带有钥匙扣的版本。，其余简化版本就作为历史设计process试错保留但是不需要消耗资源去进一步做variation了。"
- "这里lock的时候应该显示锁头，而不是钥匙，因此这部分的ui设计我需要你呼应钥匙扣".
- Motion, asked for with "省 token": the key slides in from the left edge and its tag swings, and at the crosshair the lock opens like the iPhone unlock. Red wants several variations to review.

**Decision.** B (key + ring + tag) is the key for phone and desktop.
- A, A + chip and C stay in Figma as process history: no new variations and no re-sync.
- The old FINAL re-sync list (13 masters, 114 twins, Achip frames, the §10.1 outline fixes) is dropped. Only what B needs remains.

**Padlock (DONE, Figma).** It lives in section `3113:6872` "HUD KEY · B · PADLOCK + MOTION", at x 33937, y 423, just above HUD KEY `2532:4038`.
- **Component set `3113:6999`, "Lock glyph · B · padlock":**
  - 18 variants: Tag (Rect / Round / Long) × Zone (Red / Blue / White) × State (Closed / Open).
  - Size 80 × 48. The pivot is the insert centre (40, 32), which is the crosshair point.
  - The body is cloned from the B tag vectors.
  - The shackle is paper, 2.6 px. Half-width is 8.5 (rect), 6.5 (round) or 10.5 (long), with 4 px of leg showing.
- **Boards:**
  - KV11 `3113:7000`: tag ↔ padlock pairs.
  - KV12 `3113:7147`: the locked prompt at the crosshair.
- **HUD KEY section:** labelled with "B CHOSEN" and HISTORY on the old rows.
- **Spec:** `research/ui_key_icon/lock_motion/README.md`.

**Motion.**
- Review page: https://claude.ai/artifact/ArLGmLysHRtswHQjWKSthC (private to Red).
- Source: `~/FrontRoomsVisualWork/keyicon_motion/` (`lab_template_v2.html` + `assets.py` → `out/key_motion_lab.html`).
- **Pickup: P4 picked** (Red 10-14: "我希望还是P4吧，不要P1了"). The key takes off from where the game's pickup shot holds it: viewport (0.65, 0.30), ×3, at HoldUntil 0.75 s, with the 3D key hidden on the same frame. It flies down to its slot. The map-side change is with 游戏视觉 and level design. The pose check waits for D1.6 KeyAssembly.
- **Lock: L3·D picked** (Red 10-14: "我需要L3D"). It is the iris. The dot swells into the whole tag in paper, the colour closes in to the insert, and the shackle rises. With the key, the iPhone swing. Leaving, it runs backwards into the dot. L3·A–C, E, L1 and L2 stay folded on the page as history.
- **No card** (Red 10-14: "我不希望有背后的这个黑色底"). A 1 px ink keyline (#141414, 85 %) replaces it, and both prompt lines are paper.
- Timings: `research/ui_key_icon/lock_motion/README.md` §2.

**Next.**
1. DONE 10-14: KV12 without the card, the keyline in Figma and the SVG masters, and the final timing table plus build notes (README §2) sent to 游戏视觉. Waiting on their P4 start-scale render (x/y per tag and resolution).
   - **No key: D·3 picked (Red 10-14).** Tug, then shake; `DENY.tugShake` was sent to 游戏视觉.
   - **Has-key exit: F·1 drop picked (Red 10-14).** `FALL.drop` was sent to 游戏视觉 for DECISIONS.md.
   - **Broken door: B·4 (hangs off its hinge) picked (Red 10-14).** Broken stays broken for the run (Red to 系统设计, Q9). Sent to 游戏视觉 for DECISIONS.md.
   - **Desktop + phone in-context mockups** added at the top of the review page (README §2c).
   - **Unlock gesture in the key panel: U·3 push, fold, click picked (Red 10-14).** The key doesn't go into the lock; K·1–K·4 are history. Sent to 游戏视觉.
   - **Sound pass 1 is on the review page** (Red 10-14 asked 声音设计 for matching sound effects).
     - 9 mp3 previews and a manifest are in `~/FrontRoomsVisualWork/keyicon_motion/sfx`; the WAV masters are in `~/FrontRoomsSoundWork/hud_sfx/masters`.
     - PU3 and LK1 are silent on purpose.
     - The page has a "Sound" switch; cues fire from the desktop mockup (`patches/p14_*`).
     - Red's notes go back to 声音设计 for pass 2.
   - **Open: the unlock shot hides the HUD for 1.45 s.** Options are (b) play after it, or (d) the panel stays and syncs with the real key. Feasibility check with 游戏视觉 first, then Red decides (README §2d).
   - **DONE 2026-10-15 (印刷与演示): interactables K-sheet refresh.** 50 png_fix slots across 13 sheets (FIT; Brass/Nickel thumbnails from the hero PNGs), K53 resized as specified, K47's Aluminium swatch dropped. Every slot verified with one IMAGE fill. Was: QUEUED (游戏视觉 10-14).
     - Hand-off is in `research/interactables/threeview/HANDOFF_平面视觉.md` + `index.json` (new PNGs in `png_fix/`).
     - Re-image 13 sheets: K46, K47, K48, K51, K52, K53, K57, K60, K68, K73, K75, K77, K78.
     - K53 (2653:9218): slots front 243.5 × 61, side 40 × 61, top 243.5 × 40.
       - Positions: front y 877, side x 331.5, top y 813.
       - Labels: the Side (2653:9231) and Top (2653:9232) labels move to x 339.5 (side x + 8; 游戏视觉's correction 10-14); the Top label sits at y 820.
       - Ground line width 339.5; dimension label "W 909 · D 95 · H 180 mm".
     - New sheets start at K82/K83; K84 would collide with 2349:852. The rule is in 系统设计's RELAY_PURSUIT_REDESIGN.md §10, and its Q9 (rehang broken doors Ajar during Away?) is open for Red. Spec in README §2b.
2. The timing table goes to 游戏视觉 (owner of the key pipeline and drop-in) and to the touch chat.
3. B and padlock sprites at @1x, @2x and @3x come from 游戏视觉's pipeline.
4. Re-sync the B twins. Update desktop D08 and D12–D14 and touch HUD·T / Key once the game draws B. Add a VL entry.

## 5. Waiting on others
- **Red:**
  - pick among M1–M4 and V1–V3 (the game uses A + M4 + V1 meanwhile);
  - the exit definition;
  - narrative §8;
  - the placard strings "AS PRINTED" and "WALLCOVERING SHOWN AT 1/25 SIZE".
- **游戏视觉:** hand-offs for the outlets and the placard three-views.
- **DOORS + WINDOWS section 2497:3804:** don't occupy it until Red restores it.
- **WhatTheFont:** needs Chrome connected.
