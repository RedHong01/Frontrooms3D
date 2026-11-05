# 60 · Visual build of the tech_art rows (游戏视觉)

Line 游戏视觉 (`visual`), workflow run of session local_be0f3747 (account A), started 2026-11-04 13:0x. This file is the one build log for the rendering rows of `20_ranked_changes.md` that 游戏视觉 owns; one section per stage. `20_ranked_changes.md` and its sheets stay 技术美术调研's (read only here).

Red's words: in 总控, the research ask "在做无限走廊的时候，我需要你也开启一个task用来 research专业的Technical artist会怎么做，提高画质表现的同时，节省性能。"; on 2026-11-04 12:49 in the 技术美术调研 chat, on the asks of `30_for_red.md` §3: "全部按推荐" (LANDINGS 12:49 row). That is A: the ranked list goes ahead in order, 游戏视觉 decides what lands and when; C: the 60 additive VolumetricBeam renderers leave the default (Lobby) rooms, rebuilt as depth and view-angle faded shafts (one shared material, no MPB) only in families that want dust; B (fill-light shadow off) is 关卡设计's.

This run builds rows 16, 1, 9, 3 and 7, in that order, as stacked packages `ta_lamp_lightdata` (16), `ta_door_culling` (1), `ta_beams` (9), `ta_ssao` (3), `ta_lod_fade` (7). Row 3 needed no package: 设置菜单与画质预设 had landed it at 13:16 and 13:23 (section Row 3). Row 4 goes to the title_stream run; rows 5, 6, 8, 10, 13, 13b, 14 and 15 are later runs. The Package stage (22:0x-22:2x) rebuilt rows 16, 1 and 9 over main after title_stream_rooms had landed at 20:02 (section Package); the last section gives the landing order, the contracts by owner and the open rows.

Done here: code in the line's clone `W/proj_unlock` (synced to main bc9cf07c + working tree at 13:07), packages in `W/apply/` (not applied; the 游戏视觉 session lands them), contracts for 关卡设计's files, captures with the tech_art probe's views. Not done here: nothing written into main; no download; no iOS, TestFlight or snapshot build; no sound (every Unity run and player silent, rule 17).

## Row 16 · lamps get URP's per-light data (package `ta_lamp_lightdata`)

Row 16 (`20_ranked_changes.md` §3.16, found by 技术美术调研's claim check): a light made with `AddComponent<Light>()` and no `UniversalAdditionalLightData` may be filtered at Low in a player while the Editor shows High. Checked first because it protects the desktop reference itself.

### 16.1 What URP 17.6.0 does (source, read only in `W/proj_unlock/Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8`)

- `Runtime/ShadowUtils.cs:648-658` `SoftShadowQualityToShaderProperty`: soft = 1.0 when the asset supports soft shadows and the light is `LightShadows.Soft`; only if the light has a `UniversalAdditionalLightData` is it multiplied by `max(quality, Low)`, with quality = the asset's when the component says "use pipeline settings". A bare lamp therefore sends 1.0.
- `ShaderLibrary/Shadows.hlsl:139-142` (OFF 0, LOW 1, MEDIUM 2, HIGH 3), `:161` / `:177` (the value is `_MainLightShadowParams.y` / `_AdditionalShadowParams[i].y`), `:294-305` (`SampleShadowmapFiltered` picks Low 4-tap, Medium 9-tap or High 16-tap tent), `:326-334` (used under `_SHADOWS_SOFT`; on Mac, iOS and WebGL `SupportsPerLightSoftShadowQuality` is true and `SetSoftShadowQualityShaderKeywords` turns the global LOW/MEDIUM/HIGH keywords off). `FrontRooms/Surface` declares `_SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH` and lights through URP's `GetAdditionalLight`, so it takes this path.
- `Assets/Settings/FrontRooms_URP.asset`: `m_SoftShadowsSupported: 1`, `m_SoftShadowQuality: 3` (High), `m_SupportsLightLayers: 0`; renderer `m_RenderingMode: 2` (Forward+). `FrontRoomsQuality` never writes the soft-shadow quality (grep), so High holds on every tier; in URP 17.6 `UniversalRenderPipelineAsset.softShadowQuality` is `internal`.
- What the component changes, at its defaults (`Runtime/UniversalAdditionalLightData.cs:37` `m_UsePipelineSettings = true`, `:99` tier High, `:170` `SoftShadowQuality.UsePipelineSettings`): only that value. Unchanged: the bias kernel (`ShadowUtils.cs:424-446`: a bare light counts as Medium = 2.5; "use pipeline settings" falls to the switch's default, 2.5), the bias itself (`UniversalRenderPipeline.cs:2139-2148`: bare or `usePipelineSettings` -> the asset's 0.6 / 0.6), the shadow size (`:2150-2162`: bare -> default tier High; component -> tier High = 1024), `light.renderingLayerMask` (only the component's setters call `SyncLightAndShadowLayers`), cookies (`LightCookieManager.cs:439, 877`: directional only). `ForwardLights.cs:696-699` would add the component in a player only with light layers on (off here); `DeferredLights.cs:1218` is deferred only.
- The Editor: `UniversalRenderPipelineCore.cs:1624-1638` (`#if UNITY_EDITOR`), URP's `Lightmapping.RequestLightsDelegate`, calls `light.GetUniversalAdditionalLightData()`, which adds the component at exactly these defaults (`UniversalAdditionalLightData.cs:17-25`).
- A trap the row did not list: `UniversalAdditionalLightData` is `[RequireComponent(typeof(Light))]`, and `FrontRooms3DGame.cs` (l. ~2151) `Destroy()`s the Light COMPONENTS of the title stream after the start door shuts. Unity refuses to remove a component another one requires; the release Mac player (`Variations/macos_arm64_player_nondevelopment_mono/.../UnityPlayer.dylib`) carries the message "Can't remove %s because %s depends on it", so the check runs in players too. Contract hunk G2 removes the URP data first.

### 16.2 The runtime check

Kit (clone only, in `W/proj_unlock`, never packaged): `Assets/Scripts/CloneOnly/FrontRoomsTechArtR16Capture.cs` (runtime; acts only with `-frR16Out <dir>`) and `Assets/Editor/TechArtVisual/FrontRoomsTechArtR16.cs` (`RunEditor` play-mode entry; `BuildPlayer`, which refuses any id that is not a `com.redwang.frontroomsss.test.*` one); runner `W/techart_visual/tools/run_player.sh`. Views are the tech_art probe's: title corridor 0 / 1 / 2 at 4 / 9 / 15 s, Level 0 seed 2554 heavy / median / lights poses, 1920x1080, film grain off, at a fixed 1/60 s step. Per view it logs the light census (which lights carry the component), the soft-shadow values URP actually sent (`_AdditionalShadowParams[i].y` read back after the render), draws (sum of 6.6's split counters), SetPass, shadow casters, visible lights, and saves PNGs in states "asis" (as built), "bare" (default components removed = the lamps as shipped) and "add" (URP's default component on every light = what row 16 creates). Capture v5 also pins SSAO's blue-noise index (URP advances it every render, `SSAOUtils.cs:235-247`), turns the camera's 8-bit dithering off for the shot (`FrontRoomsPostStack.cs:73` turns it on) and keeps the no-grain volume alive through the maze phase (the tech_art probe switches every scene root off for the maze, its own no-grain volume included, so its maze PNGs carry film grain). Noise floor after that (asis vs asis2): 0 px in five views, 0.30 % of px by at most 4/255 in maze_median.

Runs (UNDER LOAD: load average 120-680 while other runs of the line built and rendered):
- **Mac test player of the clone, today's code** (release, Mono, Standalone id `com.redwang.frontroomsss.test.unlock`, private name; build 30:46; silent: FMOD COREAUDIO -> NOSOUND at frame 1, Unity audio disabled; it quit itself after 45 s). Census: 71 lights in the title, 30 soft-shadowed, **1** with the component (the scene's fill); 1,668 in the maze, again 1. URP sent **y = 1 (Low)** for every soft-shadowed visible lamp: corridor 8 / 8 / 12, maze 7 / 7 / 7. With URP's default component added in the same frame: **y = 3 (High)** for all of them.
- **Editor, batch play mode** (quality level 3, the project's current level; the player runs level 5, the Standalone default), same views with the player's maze poses: census and values identical to the player: **the Editor does not add the component to runtime lamps either**, y = 1 everywhere, y = 3 with the component.
- **Editor, option D** (capture v6, a state "low" = every bare light given the component with `softShadowQuality = Low`, bias on pipeline settings; 15:10-15:11, same poses): y = 1 for every soft lamp, as bare; frame changes in the table below.
- Self-test, Editor and player alike: `Destroy(light)` beside the component leaves the Light and logs "Can't remove Light because UniversalAdditionalLightData (Script) depends on it"; `Destroy(data); Destroy(light)` in the same frame removes both; so does `DestroyImmediate(data)` first. Hence contract hunk G2.

**Result.** The player half of the inference is confirmed at runtime: the shipped Mac game filters lamp shadows at Low. The Editor half is refuted in batch play mode: the Editor is at Low too, so Editor and player agree today. An interactive Editor was not run (never on main; the clone runs batch only); both GI modes are off in `FrontRooms3D.unity` (`m_EnableBakedLightmaps: 0`, `m_EnableRealtimeLightmaps: 0`), and nothing in the census suggests the interactive Editor would differ.

What each variant does to the frame (Editor, capture v6, noise pinned, the same frozen frame, against today's bare lamps):

| view | option D, explicit Low (what lands): px > 1/255, > 8/255, max | pipeline settings, High (not chosen): px > 1/255, > 8/255, max | draws / SetPass / shadow casters / visible lights (shadowed), every state |
|---|---|---|---|
| corridor0 | 10, 0, 2 | 61, 0, 3 | 718 / 80 / 246 / 24 (8) |
| corridor1 | 0, 0, 1 | 0, 0, 1 | 698 / 66 / 244 / 24 (8) |
| corridor2 | 14, 0, 3 | 117, 0, 3 | 735 / 83 / 362 / 36 (12) |
| maze_heavy | 14,012 (0.68 %), 57 (0.0027 %), 11 | 11,067 (0.53 %), 0, 6 | 4,470 / 1,174 / 1,491 / 87 (7) |
| maze_median | 232, 0, 4 | 1,227, 0, 4 | 4,585 / 1,288 / 1,982 / 85 (7) |
| maze_lights | 790, 0, 4 | 81, 0, 2 | 3,907 / 1,179 / 1,303 / 86 (7) |

Noise floor of the pinned capture (asis vs asis2): 0 px in all six views of this run. D sends y = 1 like a bare lamp, so the filter does not move; what moves is the bias (kernel 2.5 -> 1.5), and it shows only on contact-shadow edges (under the desk chairs, the partition feet, wall-floor lines in maze_heavy), at most 11/255 on 57 px: no acne or banding on lit surfaces in these views, and the same order as the noise floor tech_art counts as identical (0.001-0.074 % over 8/255). High would move the penumbrae by at most 6/255 at +11..+40 % GPU (tech_art `21_measured.md`, UNDER LOAD), which is why D was chosen. The player's own pair with noise on (dithering, SSAO noise, grain in the maze) differs by 1-2/255 over 4.6-54 % of px, which is that noise.

**The after-player was not built.** The today build alone took 30:46 under load, and from 14:33 the line's one Unity slot was held by other runs, so the queued build was cancelled before it started (no Unity launched). What stands instead: (a) a real Mac player sends y = 1 for bare lamps and y = 3 once the component is added in play, so a player obeys the component exactly as the Editor does; (b) D's state (component with Low) was measured in the Editor through the same C# path a player runs (`ShadowUtils` has no Editor branch); (c) Roslyn compiles the package and G2. The after-player capture is combined-test row 2 below.

### 16.3 The decision moved while this stage ran: option D

While this stage measured, 技术美术调研 ran its own Mac player capture (`21_measured.md`, 14:08: players Low; High a noise-floor change at +11..+40 % GPU at 3024x1964, UNDER LOAD) and re-proposed row 16 as option D: the component with `softShadowQuality = Low` set explicitly, not pipeline settings. Red, 14:14 in the 技术美术调研 chat: "按推荐" (`30_for_red.md`: "D 已定 … 创建灯时加组件并明确设成"低"，不跟随渲染管线设置"; LANDINGS 14:15). 关卡设计 landed D at 14:18 for their nine sites (`fr land lamp_filter_low`: `FrontRoomsMapWorld.ShippedShadowFilter(light)`). This stage's spec ("left on pipeline settings") is superseded by Red's newer words, so `ta_lamp_lightdata` was rebuilt as D. The pipeline-settings build (13:43) is void; its edit script stays as `W/techart_visual/tools/r16_edits_pipeline.py`.

Two corrections to D's premise, from 16.2 and the source: the Editor was already Low (so D does not change the Editor's filter), and explicit Low moves URP's bias kernel from 2.5 (a bare light counts as Medium) to 1.5 (`ShadowUtils.cs:428-446`), so every lamp's depth and normal bias drops to 60 % in the Editor and in players. What that does to the frame: "D vs today" in 16.2.

### 16.4 The code (our files, package `ta_lamp_lightdata`, option D)

One line right after each `AddComponent<Light>()` of ours: `UnityEngine.Rendering.Universal.LightExtensions.GetUniversalAdditionalLightData(light).softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.Low` (the short form where the file already uses `UnityEngine.Rendering.Universal`), the same as 关卡设计's `ShippedShadowFilter`, inline so our files do not depend on theirs. Nothing else is set.

| file (base sha1 -> payload) | lights |
|---|---|
| `Assets/Scripts/FrontRoomsRoomStream.cs` 32482f23 -> e2ae59c1 | the corridor troffers (l.1200; 12 a room, the 4 centre ones soft-shadowed), the Run exit sign's red spot (l.2167, soft) |
| `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` c0c64842 -> dbf343ff | Run! head lights (soft; Hard on handheld, cut on WebGL), the goal window light (no shadow), the wrap lamps (some soft) |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVisual.cs` 912c1ec6 -> 51de8a76 | the breach light (soft) |
| `Assets/Scripts/Rendering/HeldItem/FrontRoomsHeldItem.cs` aaa9af47 -> 71eb6383 | the held-item fill (point, no shadow) |
| `Assets/Scripts/Rendering/Shots/FrontRoomsShotLook.cs` e5fbd816 -> 360e58cf | the unlock shot light (point, no shadow) |
| `Assets/Scripts/Rendering/Relay/FrontRoomsRelayBody.cs` 56ed1e42 -> 01b56cb2 | the Relay's contact and lens lights (no shadow) |

Already on the component before this row: `FrontRoomsStationTvLight.cs:98-99` and `Stairwell/FrontRoomsStairwellDresser.cs:689` (`usePipelineSettings = false` for their own bias; their filter is the pipeline's High; left as they are). Prefabs and kits hold no `Light`; the scene's one serialized light (the fill, unshadowed since row 2) already carries the component. `FrontRoomsRelayRig.cs` and `Scripts/Office` make no lights. Lights without shadows gain nothing; they get the line so every runtime light is on one rule. title_stream's `FrontRoomsRoomStream.cs` (W/proj_doorlean, held) keeps the same two light sites: `git merge-file` of ours over it is clean.

Main moved under this stage (run_recede_head 13:36, run_t2_body and run_door_tell 13:39-13:40: `FrontRoomsRunRig.cs` 97727e79 -> f56090ab -> c0c64842); the RunRig edit was re-made on each, same three hunks.

Contract for 关卡设计 (`W/apply/ta_lamp_lightdata_contract`): one hunk, G2. `FrontRooms3DGame.cs` (l. ~2151) `Destroy()`s the title stream's Light components after the start door shuts; with the URP component on them Unity refuses (16.2), so G2 removes the component first (inert alone). Made on 030c4a82 -> 757b6ebb, `patch --dry-run` clean. `ta_lamp_lightdata.sh` prints WAITING (exit 3, nothing written) until G2 is in main. Also noted to them: `Structures/FrontRoomsStructuresWell.cs:660` and `Structures/FrontRoomsStructuresWater.cs:511` make lamps without `ShippedShadowFilter`.

### 16.5 Evidence sheet

`sheets/r16_lamp_lightdata.jpg` (3840x2826, Shot format, PASS; fit `W/techart_visual/sheets/r16_lamp_lightdata_fit.txt`, ALL LABELS FIT; builder `W/techart_visual/tools/make_r16_sheet_d.py`). Columns: Editor today | player today (the real Mac build) | option D | D vs today x16. Rows: maze_heavy; a 4x crop of its densest D change (today, D, and the unchosen pipeline High side by side, with the difference); corridor2; COST (draws, SetPass, shadow casters, visible lights, the soft-shadow value sent, GPU note). Every pair and view: `W/techart_visual/tmp/r16_stats.tsv`.

### 16.6 Package and gate

Superseded at 22:1x by section Package, P.1 (rebuilt on main: script f1870e76, RoomStream b67034b1 -> d988bce5, RunRig 1c1fd71c -> c9292733, G2 on a9317638 -> 408a0315). The 15:0x figures below are this stage's own.

- `W/apply/ta_lamp_lightdata` (v2, mkapply), option D: 6 replaced paths, 6 `.meta` kept, no new path; WAITING gate added by `W/techart_visual/tools/r16_add_wait_gate.py`. First package of this run's stack, so its bases are main alone.
- Roslyn (`fr gate`, real argument lists): main + our 6 files, macOS + iOS 0 errors, 6 overrides (FR GATE OK, `W/techart_visual/logs/gate_ours.txt`); main + ours + G2, 7 overrides, 0 errors (`gate_ours_plus_contract.txt`).
- Dry run on main: bases, kept metas and payload OK, then WAITING on G2 (exit 3). Rehearsal on a scratch copy of main (`W/techart_visual/tmp/rehearsal`, APPLY_REAL): without G2 WAITING; with G2 DRY RUN OK, `--apply` MANIFEST OK, again ALREADY APPLIED.
- No shader text, so no shader compile check.

### 16.7 Handheld and WebGL

With D the filter stays Low on every tier, so iPhone, iPad and WebGL keep today's 4 taps; the GPU question of the pipeline-settings build is gone. What D moves on every tier is the bias kernel (2.5 -> 1.5), the same as on desktop.

### 16.8 Combined test rows (rule 25; after G2 and `ta_lamp_lightdata` have landed)

1. Title -> start door -> maze, Editor and Mac player: no "Can't remove Light because UniversalAdditionalLightData (Script) depends on it" in the console or `Player.log`; the stream lamps fade out and the rooms dispose as before.
2. Mac player capture with this stage's kit (clone-only `FrontRoomsTechArtR16Capture.cs` + `FrontRoomsTechArtR16.cs` in `W/proj_unlock`, `W/techart_visual/tools/run_player.sh`): census = every lamp carries the component, `_AdditionalShadowParams[i].y` = 1 for every soft lamp (as before), frames equal the Editor's at the same quality level.
3. Lamp shadows under the new bias (kernel 1.5, D, ours and 关卡设计's): corridor, Level 0, Office and a structures room at eye height and at grazing angles to walls and floor, Editor and player: no acne or banding on lit surfaces (16.2 "D vs today" for the probe views).
4. Lamp logic unchanged on the component: flicker, dim, dead, Relay warning, blackout, the stream's Off / Striking / Unstable / Dead.

## Row 1 · door culling (package `ta_door_culling`)

Row 1 (`20_ranked_changes.md` §3.1, sheet `ta01_corridor_rooms_behind_doors.jpg`): rooms behind shut title doors are drawn and cast into shadow maps though nothing of them can be seen, and the room behind the camera keeps its 12 lamps lit until it is recycled. Built on row 16's `FrontRoomsRoomStream.cs` (e2ae59c1), the second package of this run's stack.

Result: frames identical (no pixel moves more than 2/255 in any probe view or film frame; noise floor 1/255), draws -30 / -47 / -29 %, shadow casters -3 / -52 / -33 %, visible lights 24 -> 12 and 35 -> 23 in the tech_art corridor views. Less than tech_art's -50 / -71 / -50 % on draws, because the measurement below showed that a shut door in sight is not a closed portal: the room just past it has to stay drawn (1.2).

### 1.1 What the stream does today (main 32482f23 + row 16, read in full)

- Five pooled rooms. A title door unlatches with the camera 4 m before it (to 1 m past it), swings 0.9 s, and snaps shut once the camera is 4 m past it (`TickNearbyDoors`, the shut-behind branch). Rooms ahead stay unlit (`LampState.Off`) until the door that connects them has opened fully (`ScheduleRoomLight`); their renderers draw all along. The room behind keeps its lamps on until it is recycled. The title camera always faces +Z (`FrontRooms3DGame` sets it to identity and never turns it).
- The door's vision lites (`FrontRoomsDoorLite`) are LIVE once the door moves, or once the room past it is connected with its rear seal down (`LiteReady`, :625-632). So the shut door **behind** the title camera has LIVE lites (the camera's own room is connected, its seal down): read literally, "seen through a LIVE lite" would keep the room behind and its 12 lamps on. Hence 1.3 counts lites looking back only once the player holds the camera.
- Who else reads the stream's lamps: `FrontRoomsSoundDirector` voices the 4 lamps nearest the listener within 10 m (`fixtureVoices 4`, `fixtureRadius 10`) from `light.enabled` / `light.intensity` and plays a starter tick or strike when a voiced lamp jumps across 0.15 / 0.6 between two frames; `FrontRooms3DGame.UpdateStartRooms` fades and destroys the stream lights after the start door shuts (Tick no longer runs then). The title TV's decoder runs while its surface `isVisible` (`FrontRoomsScreenVideo`).

### 1.2 Measured first: a shut door in sight still shows the room behind it

The first build followed §3.1 as written: every room not reached through an open, moving, broken or LIVE door hidden (capture `W/techart_visual/captures/r1_v1`, script `tools/r1_edits_v1_rule_as_specified.py`, RoomStream 89432695; not packaged). The four probe views were identical (0-3 px over 1/255, max 2), but the film windows were not: while the room past the camera room's shut far door was hidden, 100-440 pixels per frame moved by up to 13/255 (door 0, camera 4.8-5.0 m away), 34/255 (door 1, same distance) and 11/255 (door 1 at 7.6-8.1 m), and they came back the frame the room was drawn again, which is a pop. Every one sits on the edges of the door's two lites: a 1 px line along the top edge of each lite (fainter along the bottom and the inner side), where the lite's edges show a sliver of whatever stands past the door (zoom: sheet bottom row; `W/techart_visual/tmp/r1/look/r1_gap_C_010.png`). The probe's own variant (`rooms_portal`) could not see this, because it measured frozen views only. Noise floor of the same runs (a second "today" session): 0 px over 1/255.

So the room just past the first shut door in sight is drawn and lit as today. Nothing past a second shut door can show through: a ray through a lite edge of one door cannot meet the same edge of the next one (the title camera is on the centreline, the lites sit at the same place on every door, and the rays diverge).

### 1.3 The rule (`FrontRoomsRoomStream.UpdatePortals`)

At the end of every Tick (after the doors, lamps, pool and rebase have moved, so the frame that follows shows it) and at the end of `Initialize` and `EndStreamAt`, each pooled room is SEEN or not, walking from the camera's room ahead (+Z) and back through each room's far door:

| looking through a door that is ... | the walk |
|---|---|
| open, opening, broken, closing or part-way; LIVE lite (looking ahead, or back once the player holds the camera); the camera within its unlatch reach + 0.75 m (4.75 m before it .. 1.75 m past it) | the room past it is seen; go on |
| shut: the first one ahead, or the first one behind once the player holds the camera | the room past it is seen (drawn and lit as today: the door's lite edges show slivers of it); go on only through open doors |
| shut: behind the title camera (it never turns; the door behind shuts 4 m behind it) | stop: the room past it is not seen |
| shut: any further one | stop |

`BeginDoorOpening` (at the rear-seal lift, where the latch releases) and `BreakDoor` make the room past the door seen in that same call. An ended stream (`frozen`, after `EndStreamAt`) and the lab switch `FrontRoomsRoomStream.PortalCulling = false` make every room seen: the handoff to the map and play run exactly as today (in play every loaded room is reachable through open doors or LIVE lites anyway). In the title this hides the rooms two or more shut doors ahead and the room behind the camera once its door has shut. The 0.75 m wake margin makes the room two doors ahead drawn 0.65 s before the nearer door moves at title speed.

Not seen means: every `Renderer` under the room's root gets `forceRenderingOff` (camera, shadow and depth passes; `enabled` is left alone, so the beam, LOD and lite code that toggles it keeps working), collected when the room goes out of sight (so pieces added after the build go too) and given back from the same list (a part moved meanwhile, such as `EndStreamAt`'s terminal seal, comes back as well); the room's `FrontRoomsWallFixtureSet`s are disabled, which drops them out of the outlet renderer's registry. The title TV is left out (its decoder warms while the set is in the frustum, as today). Colliders, AudioSources, door logic, the body doors, lites and every lamp clock are untouched.

Lamps: the room's `portalGain` (1 = what the lamp's own clock says) multiplies only the `Light` (`intensity x gain`, enabled while gain > 0); the lamp state machines (delay, strike, flicker, rise, unstable sway, dead) keep running exactly as today, so a lamp that comes back shows the state today's lamp would show at that moment; lens and beam visuals are written as today. A room going out of sight fades its lamps over 0.4 s (the lite's own LIVE / DARK time); a seen room is at full in that frame. In the title only the room behind the camera ever fades (rooms ahead are unlit until their door has opened), and it comes back only at `EndStreamAt`, in that frame: the only place its light reaches in view is the 1/255 on the floor near the camera described in 1.5. Sound: the room behind is out of sight only with the camera 4 m or more past its door, so its nearest lamp is at least 5.3 m away while the camera's room has four lamps within 4.0 m; it is never one of the sound director's 4 voices, so hum and tick / strike events do not change.

### 1.4 The code (one file, our own: `Assets/Scripts/FrontRoomsRoomStream.cs` e2ae59c1 -> 81a23e5c, +162 / -7)

Made by `W/techart_visual/tools/r1_edits.py` (nine exact, single-match edits on row 16's payload, so it can be re-made on a moved base):
- `RoomSlot`: `portalSeen`, `portalDrawn`, `portalGain`, and two reused lists (`portalRenderers`, `portalFixtureSets`; filled with the allocation-free `GetComponentsInChildren(true, list)`).
- Beside `LightRandomSeed`: the lab switch `public static bool PortalCulling = true` (as `FrontRoomsDoorLite.ForceNotReady`), `PortalWakeMargin = .75f`, `PortalFadeSeconds = .4f`.
- Calls: end of `Initialize` and of `Tick` (`UpdatePortals`), end of `EndStreamAt` (everything back), `BeginDoorOpening`'s seal lift and `BreakDoor` (`ShowPortal(next)`).
- `SetFixtureOutput` keeps its order and stores; the two lines that wrote the Light now go through `ApplyLampLight` (the same arithmetic times `portalGain`; at gain 1 the product is bit-identical), and `ApplyLampLights` re-applies a room's lamps while its gain moves.
- New: `UpdatePortals`, `FarDoorOpenToSight`, `ShowPortal`, `ApplyPortal`, `SetPortalDrawn`, `CollectPortalParts`, `ApplyLampLight`, `ApplyLampLights`.

Not touched: the pool, recycling, rebase, door timing and audio, `FirstClosedDoorSequence` (the forward walk passes the door it names and stops at the next shut one), `EndStreamAt`'s handoff, the body-doors partial, lites, exit signs (a Run room's red sign light is the sign's own; Run rooms appear only in the legacy playable sequence the edit-mode tests drive), every collider.

### 1.5 Evidence (capture `W/techart_visual/captures/r1`, 2026-11-04 17:10-17:13)

Kit (clone only, never packaged): `Assets/Scripts/CloneOnly/FrontRoomsTechArtR1Capture.cs` (acts only with `-frR1Out`; reads and sets `PortalCulling` by reflection, so the clone compiles without row 1) and `Assets/Editor/TechArtVisual/FrontRoomsTechArtR1.cs` (one Editor batch: the RoomStream edit-mode tests, then three play sessions). Runner `W/techart_visual/tools/r1_run.sh` (waits for the line's Unity slot; the row-1 RoomStream is in `W/proj_unlock` only for the run). Sessions: "off" (`PortalCulling` false = every room drawn and lit, i.e. main + row 16), "on" (row 1), "off2" (a second "off": the noise floor). Each at a fixed 1/60 s step from the title start, desktop ULTRA preset (Editor quality level 3, the tech_art probe's setting), 1920x1080, film grain, dithering and SSAO noise off for every captured frame, silent (FMOD NOSOUND, Unity audio off). Views: the tech_art probe's cameras (title camera at z 4.72 / 10.47 / 17.37, their probe log), then `EndStreamAt` as the game's start calls it and the same camera forward (`handoff_fwd`) and turned round (`handoff_back`: the room behind, through the shut door's LIVE lites, back and lit). Films: every frame of three windows, A door 0 opening (z 0.95-3.10, 112 frames), B the door behind shutting and its room fading (z 9.85-10.46, 32 frames), C door 1 opening (z 13.00-15.15, 113 frames). Counts: UnityStats medians of 30 frames (as the probe) and URP's visible lights.

| view | draws | SetPass | triangles | shadow casters | visible lights (shadowed) | px > 1/255 (> 8/255) |
|---|---|---|---|---|---|---|
| corridor0 (room 0, door 0 open) | 710 -> 495 (-30 %) | 83 -> 80 | 122.7k -> 97.2k | 171 -> 166 | 20 (5) -> 20 (5) | 0 (0) |
| corridor1 (room 1, both doors shut) | 731 -> 388 (-47 %) | 71 -> 63 | 128.3k -> 68.2k | 245 -> 118 | 24 (8) -> 12 (4) | 0 (0) |
| corridor2 (room 1, door 1 open) | 831 -> 593 (-29 %) | 100 -> 92 | 189.6k -> 142.3k | 386 -> 260 | 35 (12) -> 23 (8) | 0 (0) |
| handoff_fwd / handoff_back | 549 / 544, unchanged | 98 / 90 | 152.2k / 99.2k | 326 / 307 | 35 (12), unchanged | 0 (0), bit-identical |

Today's draws are below tech_art's 846 / 833 / 952 because row 2 (the fill light's shadow, landed 13:00) is in this clone; their "dirshadow_off" variant measured 709 / 730 / 831, matching. For comparison, the rule as first specified (1.2) gave 355 / 214 / 425 draws and 138 / 72 / 204 casters.

Pixels, "on" against "off", outside the title TV's box (its video is not frame-locked): no pixel over 2/255 anywhere. Over 1/255: 0 in every view; 0 in films A and C; 4 px (2/255, one each in 4 frames) in film B while the room behind fades. The one systematic change is at 1/255: 3,427 px of the floor in the lower fifth of corridor1, and about 3,300 floor px per frame in film B: the light of the room behind's eight unshadowed side lamps, which today passes through its end wall into the camera's room; it is gone once the room behind is out of sight and is 0 px by corridor2. Film A's 223 px at 1/255 (89 frames) equal the noise floor's exactly; film C's 929 are below its 1,273. Noise floor (off2 against off): 0 px over 1/255 in every view and film frame. Every pair per frame: `captures/r1/r1_stats.tsv` (`tools/r1_diff.py`).

Timeline (`tools/r1_timeline.py`): the room past the camera room's shut door is drawn the whole time; the room two doors ahead is drawn from frame 16 of film A (z 1.265) and frame 14 of film C, 39 frames (0.65 s) before the nearer door first moves (frames 55 and 53); the room behind goes out of sight in the frame its door snaps shut (film B frame 8, z 10.010) and its lamps reach 0 within 24 frames.

RoomStream edit-mode tests, once, with the row-1 RoomStream in the clone (both runs): `FrontRoomsStreamVerification.Run` 5 passed, 0 failed; `FrontRoomsExitSignTests.RunBatch` 13 passed, 0 failed (R2 walks 82 rooms twice and checks every sign light every frame); `FrontRoomsBodySpecialDoorTests.RunBatch` 21 passed, 0 failed (the real stream's terminal pair after `EndStreamAt`). Reports in `captures/r1/tests/`.

Not measured: frame time (load average 20-420 on this Mac while other runs rendered; rule 25 leaves it to the combined test); a Mac player (the Editor and the player drew the same frames in row 16's capture).

### 1.6 Sheet and film

- `sheets/r1_door_culling.jpg` (3838x5517, Shot format, PASS; fit `W/techart_visual/sheets/r1_door_culling_fit.txt`, ALL LABELS FIT; builder `W/techart_visual/tools/make_r1_sheet.py`). Columns: today | door culling | difference x32 (the TV box outlined). Rows: corridor0, corridor1, corridor2, handoff_back; the lite edges of 1.2 (today, the room past the door hidden, difference x16, all x4); COST (both states, and the change with every film frame).
- `video/r1_door_culling.mp4` (1918x562, H.264, no audio, 257 frames at 15 fps = 4x slow; builder `W/techart_visual/tools/make_r1_film.py`): every frame of films A, B and C in the Shot format, today | door culling | difference x32.

### 1.7 Package and gate

Superseded at 22:1x by section Package, P.2 (rebuilt over title_stream: script 8ff70986, RoomStream d988bce5 -> 43557490).

- `W/apply/ta_door_culling` (v2): built with `W/techart_visual/tools/mkapply_stacked.py` (base view `W/techart_visual/view`, `--after W/apply/ta_lamp_lightdata_payload`): 1 replaced path, `FrontRoomsRoomStream.cs` e2ae59c1 -> 81a23e5c, `.meta` kept (629abdbd), no new path. Stacked: its base is row 16's payload, so it lands after `ta_lamp_lightdata`, which waits for 关卡设计's G2.
- `fr gate --override Assets/Scripts/FrontRoomsRoomStream.cs=...` over main: FR GATE OK, macOS + iOS 0 errors, 1 override (`W/techart_visual/logs/gate_r1.txt`; row 16's two lines are in the same file, so this also re-gates them over today's main).
- Dry run on main: REFUSE at the base only (main 32482f23 is not the stacked base e2ae59c1), nothing written. Rehearsal in order on a scratch copy of main (`W/techart_visual/tmp/r1/rehearsal`, APPLY_REAL): G2 patched, `ta_lamp_lightdata.sh --apply` MANIFEST OK, `ta_door_culling.sh` DRY RUN OK, `--apply` MANIFEST OK, again ALREADY APPLIED.
- No shader text, so no shader compile check.

### 1.8 Handheld and WebGL

The same rule on every tier (it only removes work): no `FrontRoomsHandheld` or `UNITY_WEBGL` branch. On iPhone, iPad and WebGL, where a draw costs most (about 40 µs of main thread per draw on WebGL, `research/webgl/03`), the corridor saves the same share; the 32-light tiers (row 12) see 12 fewer lights with the room behind out of sight.

### 1.9 title_stream (held) and this row

title_stream's clone (`W/proj_doorlean`, read only) carries its own "lights off behind shut doors" in the `TitleRooms` partial: `DimRoom` switches the lamps of the room behind a shut title door off in the shut-behind branch and `UndimRoom` switches them back on in `EndStreamAt`, both only while a title programme other than Today runs; renderers are not hidden. `git merge-file` of ours over their `FrontRoomsRoomStream.cs` (1cf9d6d1) gives three mechanical conflicts: the `RoomSlot` fields (keep both), the end of `Tick` (keep both lines), and `SetFixtureOutput` (their `&& !room.lightsDimmed` goes into `ApplyLampLight`'s `light.enabled`). With row 1 in main, `DimRoom` / `UndimRoom` do nothing row 1 does not already do (row 1 fades, hides the renderers and covers Today too); whether to keep them is title_stream's call when it rebases. Their family vignettes and own lamps live under the slot roots (`ownLights` swap into `roomLights`), so row 1 hides and fades them with their room: `CollectPortalParts` gathers a room's renderers when it goes out of sight, not when it is built. For furnished families the room just past the first shut door costs its full draws (a Run! B room +666, title_stream survey); drawing only what the door's opening can frame (a portal frustum through the door rectangle) is the next step if that cost matters.

Update 22:1x: title_stream_rooms landed at 20:02 (Tour by default). Row 1 is rebuilt over it (section Package, P.2): DimRoom / UndimRoom stay (they act only with a title programme), `ApplyLampLight` honours `lightsDimmed`, and the door culling implements the partial `TitleVignetteMoved`, so a vignette moved into a slot out of sight is hidden with it.

### 1.10 Combined test rows (rule 25; after G2, `ta_lamp_lightdata` and `ta_door_culling` have landed)

1. Title crawl, Editor and Mac player, desktop: watch each door from far away to fully open: nothing of the room past it appears late; lamp strikes and the hum as today; the title TV shows the ad as soon as it is in view.
2. Start (Space) at three points of the crawl (door ahead shut; door ahead opening; just after the door behind snapped shut), then turn round at once: the room behind is there and lit through the door's lites; walk back through it.
3. The start door to the map, the stream lamps' fade and the rooms' disposal as today (`UpdateStartRooms`).
4. iPhone or iPad title crawl (device run on 总控's word): same as 1.

## Row 9 · corridor beams (package `ta_beams`)

Row 9 (`20_ranked_changes.md` §3.9) as Red decided it (C, 12:49 in the 技术美术调研 chat: "全部按推荐"): the 60 additive VolumetricBeam renderers (12 per room, a MaterialPropertyBlock each, density 0.012, their peak about 1 % of the lamp colour) leave the default (Lobby) rooms; visible dust comes back only where a room family asks for it, as one shared-material shaft with no MPB, faded where it meets the scene and with no edge to show edge-on. Built on row 1's `FrontRoomsRoomStream.cs` (81a23e5c), the third package of the stack.

Result: in the tech_art probe views the beams' faint glow goes (at most 11-24/255, on 0.018-0.22 % of the pixels over 8/255, the share 技术美术调研 measured) and nothing else moves; 7-21 fewer draws and 7-19 fewer SetPass calls a view (with door culling off, as tech_art measured: -7 / -6 / -11 draws, -11 / -6 / -18 SetPass; theirs -7 / -6 / -11, -11 / -8 / -18). No room family asks for dust in main, so the new shafts cost nothing today; a dusty room costs one draw per lit lamp in view, a few SetPass calls (9.6) and about 1.1-1.2 ms of GPU at 1920x1080 on this Mac (UNDER LOAD).

### 9.1 What the beams were (base 81a23e5c, read in full)

- `BuildRoom` hung `volumetric fluorescent beam <i>` under each of the 12 troffers: a 1.0 m tapered sheet 4 cm below the ceiling (8 vertices, two faces, 0.36 m wide at the lens to 0.56 m, 6-12 cm deep), material `Resources/Lighting/VolumetricBeam` (`FrontRooms/VolumetricBeam`: additive, `Cull Off`, `ZTest LEqual`, alpha = density x 0.82 x height and side fades), render queue 3000, no shadows or probes.
- `CacheRoomLightVisuals` gave each one an MPB; `ApplyFixtureVisual` wrote `_Color`, `_Density` (0.012 x the lamp's level) and `_DiffuseCoefficient` into it whenever the lamp's output was set (every frame on desktop) and enabled it while its lamp was lit. An MPB takes its renderer out of the SRP Batcher, so every beam drawn cost a draw and a SetPass of its own (19 / 11 / 22 / 34 beams drawn in corridor0 / corridor1 / corridor2 / the handoff views).
- What they showed: a faint tapered glow under each lit lamp, max 11-24/255; the difference column of `sheets/r9_beams.jpg` (x32) shows each one, the nearest with its polygon outline.

### 9.2 The change (`Assets/Scripts/FrontRoomsRoomStream.cs` 81a23e5c -> c6f28e70, +78 / -111)

Made by `W/techart_visual/tools/r9_edits.py` (12 exact, single-match edits on row 1's payload, so it can be re-made on a moved base):
- Gone, for every room (all title rooms are Lobby rooms in main; title_stream's families are not in main yet): `volumetricDensity`, `volumetricRange` (serialized fields; the stream is added at runtime, so no scene or prefab holds them, and nothing else in main reads them), `RoomSlot.volumetricRenderers` / `volumetricBlocks`, `volumetricLightMaterial`, `EnsureVolumetricLightMaterial` and its two calls, `BuildVolumetricBeam`, `CreateVolumetricFrustumMesh`, the beam half of `CacheRoomLightVisuals` and of `ApplyFixtureVisual`.
- Kept exactly: every Light (intensity, colour, range, spot angles, shadows, row 16's per-light data, row 1's door-culling gain), the lens emission and colour MPBs, the lamp state machines (delay, strike, flicker, rise, failing, dead), what the sound director reads, door culling. Shadow casters and visible lights are unchanged in every view (9.5).
- New in the stream, inert until something asks for dust: `RoomSlot.dust`, `dustShafts`, `dustLights`, `dustValues`; `SetRoomDust`, `BuildDustShafts`, `ApplyDustShaft` (called where the beam was written; it returns at once while a room has no shafts, which is every room today).
- Left in place: `Resources/Lighting/VolumetricBeam.shader` (+ `.meta`). Nothing loads it after row 9; it still ships as a Resources shader (a few KB). Deleting it is Red's call (candidate).

### 9.3 Dust shafts, built only when a room asks (`Scripts/Rendering/FrontRoomsDustShaft.cs` 26c427f7, `Resources/Rendering/FrontRoomsDustShaft.shader` 03df0eaa, both new)

The RunBeamCone idea (`Resources/Rendering/RunBeamCone.shader`, Run! B: an additive cone whose silhouette fades with the view angle and whose depth fade hides the line where it meets a wall) done as a small volume, so that it fades by construction rather than by its polygons:
- **Geometry.** One box per lamp below its lens (troffer: lens 0.54 x 1.12 m, box 2.56 x 3.14 m, 1.7 m down to about 1.2 m above the floor), its far faces drawn (`Cull Front`, `ZTest Always`, additive, queue 3100), so a camera inside the box still sees the shaft once. One mesh per lamp shape (the sizes ride in its UVs), shared by every shaft of that shape.
- **The march.** The fragment intersects its view ray with the box, clamps it at the scene depth (`_CameraDepthTexture`; the dust ends where the floor, a desk or a wall begins: the depth fade as a volume, no intersection line) and at the near plane, and takes 8 steps along it (5 on handhelds), offset per pixel by interleaved gradient noise so a few steps leave no bands. Dust nearer the eye than 0.35 m fades, so walking through a shaft does not fog the lens.
- **Light.** Brightest just under the lens; the light halves 0.5 m down and the last part of the box fades out. Sideways it is full under the lens's rectangle and falls off in a smooth tail that widens 0.24 m per metre down. There is no flat side anywhere, so nothing lines up edge-on: the view-angle fade as a volume. Henyey-Greenstein forward scattering (g 0.35, 1 at 90 degrees, up to 4.3 looking toward the lamp) makes a shaft glow most when the eye looks up toward its lamp.
- **Dust.** A 32^3 tiling value-noise volume (two octaves; R8, 32 KB) made once on the CPU when the first room asks for dust (no asset, no download), sampled at two scales, squashed 1.7x vertically into thin layers and drifting at 4 cm/s; contrast 0.8. It reads as faint smoke and dust layered under the ceiling and lit by each lamp, densest at the lens.
- **One material, no MPB.** Every shaft shares the material; the lamp's colour (RGB565), its level times the room's dust (8 bit) and a per-shaft seed (8 bit) ride in the renderer's shader user value (`MeshRenderer.SetShaderUserValue`, read as `unity_RendererUserValue`, URP 17.6 / Unity 6.3+), so the shafts stay in the SRP Batcher. A shaft follows its lamp through `ApplyDustShaft`: strike, flicker, failing and dead lamps show in their dust; a level of 0 disables the renderer.
- **First build, measured and replaced.** The first version (soft-edged frustum, 24 steps of hashed noise, 2.4 m boxes, T/tmp/r9/v1) cost +9.1 ms at 1920x1080 and +252 ms at 7680x3544 in the tune view (camera inside room 1, 11 lit lamps; medians of 12 alternating synchronised renders), and its flat-topped frustum showed a sharp side where the camera grazed it; the version above costs +1.1 ms and +32 ms in the same view (both UNDER LOAD, load average 22-45).

### 9.4 The one-line API for title_stream

`SetRoomDust(room, dust)` (private member of `FrontRoomsRoomStream`, so title_stream's `TitleRooms` partial calls it directly): `dust` 0 = clear air (every room today) .. 1 = thick; the dusty family shown in 9.6 is **0.5**. Where a slot takes its family (title_stream's `AttachVignette`): `SetRoomDust(room, familyDust);` and where it gives the family back (`ReleaseVignette`): `SetRoomDust(room, 0f);`. The first call above 0 builds the room's 12 shafts (they stay built, switched off, at 0); they follow the room's lamps, and they go with the room when door culling hides it (a shaft built while its room is hidden joins the room's hidden parts).
- A shaft shows only the lamp it hangs under: while title_stream's `SwapToVignetteLamps` hands a slot's lamp channels to a vignette's own lights, the slot's shafts stay dark (`dustLights` guard); a family with lamps of its own builds its shafts with `FrontRoomsDustShaft.Build(lampTransform, lensCentre, lensHalfX, lensHalfZ, length, name)` and drives them from its own lamp tick with `FrontRoomsDustShaft.Drive(shaft, FrontRoomsDustShaft.Pack(colour, level * dust, seed))`.
- Rebasing title_stream over this row (checked read-only on `W/proj_doorlean`: RoomStream b67034b1, TitleRooms 7b0eaa69): `git merge-file` gives 4 conflicts, all mechanical (row 1's three of 1.9, plus `Initialize`: keep their `BeginTitleRooms(false)`, drop `EnsureVolumetricLightMaterial`), and their partial drops the 3 statements that read `room.volumetricRenderers` (`CaptureOwnLamps` :333, `SwapToVignetteLamps` :437, `RestoreSlotLamps` :458-459; `ownBeams` can go too). That merged tree compiles: Roslyn macOS 0 errors (`W/techart_visual/tmp/r9/merge/`).

### 9.5 Evidence: the Lobby rooms (capture `W/techart_visual/captures/r9`, 2026-11-04 19:25-19:45)

Kit (clone only, never packaged): `Assets/Scripts/CloneOnly/FrontRoomsTechArtR9Capture.cs` (acts only with `-frR9Out`; reaches row 1 and row 9 by reflection, so the clone compiles with or without them) and `Assets/Editor/TechArtVisual/FrontRoomsTechArtR9.cs` (one Editor batch: the shader compile check, then the play sessions). Runner `W/techart_visual/tools/r9_run.sh` (waits for the line's Unity slot; the batch's RoomStream and the dust files are in `W/proj_unlock` only for the run). Sessions as row 1's (fixed 1/60 s step from the title start, desktop ULTRA preset, 1920x1080, film grain, dithering and SSAO noise off, silent): "base" (rows 16 + 1 with the beams), "base2" (the noise floor), "row9", and "basepc0" / "row9pc0" with door culling off (as the tech_art probe measured). Views: the tech_art probe's cameras (title camera at z 4.72 / 10.47 / 17.37), then `EndStreamAt` and the camera forward (`handoff_fwd`) and turned round (`handoff_back`). Film D: every frame from z 5.00 to 9.60 (240 frames, 4 s), through door 0 into room 1.

| view | draws | SetPass | shadow casters | visible lights (shadowed) | px > 1/255 | px > 8/255 | max |
|---|---|---|---|---|---|---|---|
| corridor0 (room 0, door 0 open) | 495 -> 488 (-7) | 80 -> 69 (-11) | 166 = | 20 (5) = | 0.24 % | 0.018 % | 11 |
| corridor1 (room 1, both doors shut) | 388 -> 381 (-7) | 63 -> 56 (-7) | 118 = | 12 (4) = | 2.10 % | 0.086 % | 11 |
| corridor2 (room 1, door 1 open) | 593 -> 582 (-11) | 92 -> 74 (-18) | 260 = | 23 (8) = | 0.43 % | 0.086 % | 17 |
| handoff_fwd / handoff_back | 549 / 544 -> 538 / 523 (-11 / -21) | 98 / 90 -> 80 / 71 (-18 / -19) | 326 / 307 = | 35 (12) = | 0.43 / 3.52 % | 0.086 / 0.216 % | 17 / 24 |
| door culling off (corridor0 / 1 / 2) | 710 / 731 / 831 -> 703 / 725 / 820 | 83 / 71 / 100 -> 72 / 65 / 82 | = | = | same pixels | same pixels | - |

Pixels outside the title TV's box (its video is not frame-locked). Triangles -26 to -44 (4 per beam). Film D: every frame changes where a beam was (> 8/255 on 285-8,062 px a frame, median 3,413, max 17/255). Noise floor (base2 against base): 0 px over 1/255 in every view. Today's numbers equal row 1's "on" capture exactly (corridor0 495 draws, 80 SetPass). Every pair: `captures/r9/r9_stats.tsv` (`tools/r9_diff.py`).

Shader: `FrontRooms/DustShaft` compiled offline in `W/proj_unlock` (ShaderData.Pass.CompileVariant, as the pool look's check) for the four fog keyword sets on Metal macOS (vertex + fragment), Metal iOS, D3D (vertex + fragment) and GLES3x / WebGL: 24 OK, 0 failed; control (the same text with one undeclared identifier, made in memory) 6 failed -> PASS (`captures/r9/shader_check.txt`). GLES3x answers success with 0 bytes here, so WebGL is not proven by it (WebGL builds no shafts).

### 9.6 The dust shaft: still, film and cost

Test room: room 1 (sequence 1) of the title stream at `SetRoomDust(room, 0.5)` from the first frame, through the capture kit's clone-only switch (nothing in main calls it); session "dust" of the same capture, every other setting as 9.5. Room 1's lamps are still striking when film D starts (8 of 12 lit, 11 by its end; the twelfth stays off), and their dust comes with them.

- `sheets/r9_dust_shaft.jpg` (3840x3426, Shot format, PASS; fit `W/techart_visual/sheets/r9_dust_shaft_fit.txt`, ALL LABELS FIT; builder `W/techart_visual/tools/make_r9_dust.py`): film D frame 130 (camera z 7.50, inside room 1 a metre before its first centre lamp, eye height 1.62 m), Today | Dusty family, the same frame; under them what each adds to row 9's frame x8 (the title TV's box left out: its video is not frame-locked): today's beams at most 15/255, over 8/255 on 0.17 % of the frame; the dust at most 45/255, over 8/255 on 8.4 %, most of it a few /255 of haze under the ceiling; then the cost.
- `video/r9_dust_shaft.mp4` (3840x1340, H.264, no audio, 240 frames at 60 fps = real time, 4 s, 5.65 MB; same builder): every frame of film D, Today | Dusty family, walking from a metre before door 0 into room 1 at the title speed (1.15 m/s).
- What it looks like (my one look, rule 26): a faint haze layered under the ceiling, lit by each lamp and densest in the half metre under its lens, drifting slowly; under the far side lamps it gathers into soft smoke-like patches. No edge, no cone and no polygon shows, edge-on or walking under a lamp; the floor, walls and doors are untouched (the dust stops at the first surface). It reads as dust and smoke in lamp light, not as fog: at 0.75-1 the far patches turn into billowing clouds (a smoky storeroom), at 0.25 only the lenses glow. Levels tried with the kit's own shafts in today's build (`captures/r9/tune`, `captures/r9/tune_v1` for the first shader).
- Cost, room 1 dusty at 0.5 (the level does not change the cost): one draw per lit lamp whose box is in view (corridor0 +7, corridor1 +7, handoff_back +11; the sum over the room's lamps when the camera stands inside it); SetPass +9 / +7 as captured, because the shafts were sorted among the other transparents (queue 3000) and every break costs a SetPass; the package draws them at queue 3100, back to back after the other transparents (one SRP batch), which is not yet measured: batch C (`W/techart_visual/tools/r9_run.sh B ... dust`) waited for the clone behind another run (9.7). GPU, synchronised renders of the whole frame (12 alternating each, UNDER LOAD 25-60): handoff_back (room 1 from its far end) 1920x1080 6.62 -> 7.83 ms (+1.20), 7680x3544 89.5 -> 120.4 ms (+30.9); in the tune view +1.13 and +32.1 ms. The first shader cost +9.1 and +252 ms there.
- The capture ran `FrontRoomsDustShaft.cs` 245eb5ee; the package carries 26c427f7, which differs only in the material's render queue (3000 -> 3100). Additive dust adds the same light in either order; only where a shaft is seen through glass (a door lite) can the order change a frame, by what that glass does to the light behind it.

### 9.7 Package and gate

Superseded at 22:1x by section Package, P.3 (rebuilt over title_stream: script f6436bef, RoomStream 43557490 -> 6893ef1a, TitleRooms partial 94d456b0 -> 489c721b).

- `W/apply/ta_beams` (v2): built with `W/techart_visual/tools/mkapply_stacked.py` (base view `W/techart_visual/view`, `--after` the two earlier payloads): 5 paths, 4 new (`Scripts/Rendering/FrontRoomsDustShaft.cs` + `.meta` GUID 79076ea2, `Resources/Rendering/FrontRoomsDustShaft.shader` + `.meta` GUID af764fcf; both GUIDs unused in main) and `FrontRoomsRoomStream.cs` 81a23e5c -> c6f28e70 (`.meta` kept, 629abdbd). Stacked: its RoomStream base is row 1's payload, so it lands after `ta_door_culling`.
- `fr gate --override Assets/Scripts/FrontRoomsRoomStream.cs=... --override Assets/Scripts/Rendering/FrontRoomsDustShaft.cs=...` over main: FR GATE OK, macOS + iOS 0 errors, 2 overrides (`W/techart_visual/logs/gate_r9.txt`).
- Dry run on main: REFUSE at the stacked base only (main 32482f23 is not 81a23e5c), nothing written. Rehearsal in order on a scratch copy of main (`tools/r9_rehearsal.sh`, APPLY_REAL): G2 patched, `ta_lamp_lightdata.sh --apply` MANIFEST OK, `ta_door_culling.sh --apply` MANIFEST OK, `ta_beams.sh` DRY RUN OK, `--apply` MANIFEST OK, again ALREADY APPLIED.
- Found on the way: `ta_lamp_lightdata` now refuses on main at `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` (main 75ac054e, its recorded base c0c64842; its RoomStream base still matches), so it needs `W/tools/rebase_apply.sh ta_lamp_lightdata` before it lands; the rehearsal used its recorded base copy there.
- Not done: the SetPass count of the shafts at queue 3100 (9.6). The re-run (batch C: `zsh W/techart_visual/tools/r9_run.sh B W/techart_visual/captures/r9 r9_editor_C dust -frR9DustFile W/techart_visual/tmp/r9/dust_level.txt`, then `make_r9_dust.py`) waited from 19:48 for the line's Unity slot: surface_detail's `pt_v1` run held `W/proj_unlock` from 19:49:08 and wrote nothing after 19:51:38 (its Unity at 0.1 % CPU). It changes no file of the package; it belongs to the combined test if no later stage runs it.

### 9.8 Handheld and WebGL

Removing the beams is the same on every tier (it only removes work): -7 to -21 draws a corridor view matters most on iPhone, iPad and WebGL (about 40 µs of main thread per draw on WebGL, `research/webgl/03`). The dust: WebGL builds no shafts (`FrontRoomsDustShaft.Supported` is false there and on any GPU without 3D textures); handhelds march 5 steps. The shafts need the camera depth texture: both pipeline assets ask for it today (`FrontRooms_URP` and `FrontRooms_URP_Handheld`, `m_RequireDepthTexture: 1`); a tier that drops it (row 13 looks at the prepass) must keep it for a dusty family, or the dust would show through walls (the boxes draw with `ZTest Always` and stop only at the scene depth). Not measured on a device.

### 9.9 Combined test rows (rule 25; after G2, `ta_lamp_lightdata`, `ta_door_culling` and `ta_beams` have landed)

1. Title crawl, Editor and Mac player, desktop: the Lobby rooms look as before minus the beams' faint glow under each lamp (9.5); lamp strikes, flicker and the hum as today.
2. Start (Space), play into Level 0: no warning about `Lighting/VolumetricBeam` or `FrontRooms/DustShaft`; the start door's lamp fade and the rooms' disposal as today.
3. Once title_stream sets dust for a family: the shafts follow their lamps (a failing lamp's dust flickers with it, a dead lamp has none), go with their room behind a shut door, stay dark under a vignette's own lamps, and cost what 9.6 measured; iPhone or iPad title crawl on 总控's word.

## Row 3 · SSAO samples (landed by 设置菜单与画质预设; no package `ta_ssao`)

Row 3 (`20_ranked_changes.md` §3.3, sheet `sheets/ta07_ssao_settings.jpg`): SSAO High (12 samples) -> Medium (8) on the desktop, where 技术美术调研 measured the frame unchanged at the noise floor for about a third of the AO pass; Medium samples on the handhelds too.

Result: row 3 was in main before this stage began. 设置菜单与画质预设 (account A, local_3cce1607) landed it in two packages, with the 游戏视觉 session's yes (local_be0f3747, 13:1x-13:2x): the handheld renderer at 13:16 and the desktop renderer, `FrontRoomsRenderSetup.cs` and the handheld hook at 13:23 (LANDINGS 2135-2142). This stage therefore built no `ta_ssao` package and took no capture: a peer's landing is not re-verified (RULES rule 26), and 技术美术调研's run6 and run8 are the evidence (3.3). The 游戏视觉 session's note to this stage (journal, 13:08:56): "So the Row 3 stage builds NO package and runs no measurement (rule 26; tech_art's ta07 is the evidence)".

### 3.1 Who sets the samples (main read only, 2026-11-04 20:1x)

- Desktop: `Assets/Settings/FrontRooms_URP_Renderer.asset` f3efd6ba, the renderer of `FrontRooms_URP.asset` (guid 9f471c95), which quality levels 0-5 use (Standalone default 5 Ultra, WebGL default 3 High). Its ScreenSpaceAmbientOcclusion feature: `Samples: 1` (URP 17.6 `AOSampleOption`, `Runtime/RendererFeatures/ScreenSpaceAmbientOcclusion.cs:78-83`: 0 High = 12, 1 Medium = 8, 2 Low = 4), Source 1 (depth normals), NormalSamples 2, Intensity 1.6, Radius 0.45, Downsample 0, AfterOpaque 0, BlurQuality 0 (bilateral). Only the sample count moved at 13:23 (15ec2379 -> f3efd6ba).
- Shader prefilter: `Assets/Settings/FrontRooms_URP.asset` 9c3c038b, `m_PrefilterSSAOSampleCountLow / Medium / High` = 1 / 0 / 1 (1 = stripped, `Runtime/Data/UniversalRenderPipelineAssetPrefiltering.cs:161, 280, 355`): only the 8-sample variant is built. Their NOTE also records "SSAO mode SelectOnly->Select (what URP writes at build now that one renderer has no SSAO)", the pool mirror's renderer below.
- Editor setup: `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` 036d5d2f (our file, landed by 设置菜单 with our yes): `:141` `Set(fso, "m_Settings.Samples", 1)`, so a rerun of the rendering setup keeps Medium; `:56` calls `FrontRoomsHandheldPipeline.Sync(pipeline)` after the quality-level loop (`Assets/Editor/FrontRoomsHandheldPipeline.cs` 9a667d9d, 设置菜单's), whose `KeepSsao` list (Source, AfterOpaque, Samples) keeps the handheld twin's own SSAO values.
- Handheld: `Assets/Settings/FrontRooms_URP_Handheld_Renderer.asset` bd8ffd3c (iPhone and iPad, quality level 6 Handheld, excluded on Standalone and WebGL): `Samples: 1`, Source 0 (depth), AfterOpaque 1 (row 13), NormalSamples 2. `FrontRooms_URP_Handheld.asset` b46205e5 strips the same sample counts (1 / 0 / 1) and the depth-normals variant.
- Runtime: `Assets/Scripts/FrontRoomsQuality.cs` 6028b19d (设置菜单's) writes only SSAO's half-resolution switch (`Downsample`, by reflection on the internal `m_Settings`, `:396-424`; on at the LOW and MEDIUM presets or with AMBIENT OCCLUSION = HALF, `:160`, `:275`, `:301`), never the sample count, and never switches the feature off (its off variant is stripped). No preset rewrites the samples, so no contract to 设置菜单 was needed.
- No other renderer carries SSAO: the three renderer assets in `Assets` are the two above and `FrontRoomsWaterReflection_Renderer.asset` 4cdf3cd7 (the pool mirror, no features); `Resources/Rendering` holds none.

### 3.2 What landed (LANDINGS 2026-11-04 13:16-13:23; at 20:1x main holds every file below at these sha1s)

| time | package (设置菜单与画质预设) | row 3 paths, sha1 before -> after |
|---|---|---|
| 13:16 | `stage_A` (rows 13 / 13b; row 3 on the handhelds) | `Settings/FrontRooms_URP_Handheld_Renderer.asset` new -> bd8ffd3c (.meta 07e41777), `Settings/FrontRooms_URP_Handheld.asset` new -> b46205e5 (.meta b0e6c427), `Editor/FrontRoomsHandheldPipeline.cs` new -> aca69aa0, `Scripts/FrontRoomsQuality.cs` 88027cf8 -> 6028b19d, `ProjectSettings/QualitySettings.asset` a8ae1402 -> c1aac3f8 (+ the settings menu, nine language tables, `DISPLAY_SETTINGS.md`) |
| 13:23 | `stage_ssao_medium` (row 3 on the desktop + 游戏视觉's pool_water settings contract) | `Settings/FrontRooms_URP_Renderer.asset` 15ec2379 -> f3efd6ba, `Settings/FrontRooms_URP.asset` 18584f2b -> 9c3c038b, `Editor/Rendering/FrontRoomsRenderSetup.cs` a871837a -> 036d5d2f, `Editor/FrontRoomsHandheldPipeline.cs` aca69aa0 -> 9a667d9d (+ `Settings/FrontRoomsWaterReflection_Renderer.asset` new -> 4cdf3cd7, `DISPLAY_SETTINGS.md` 66d1fa58 -> a55e8cf8) |

Gate, from their NOTE rows: macOS + iOS 0 errors for both packages.

### 3.3 Evidence (技术美术调研's, `W/tech_art/MEASURED.md`; not re-measured here)

- Look, run6 (Editor batch, Metal, 1920x1080, desktop ULTRA, the probe's six views, film grain off; share of pixels over 8/255 against the same frozen frame, where identical frames give 0.00-0.07 %): Medium against High 0.002 / 0.002 / 0.003 % in corridor 0 / 1 / 2, 0.001 / 0.046 / 0.037 % in maze heavy / median / lights. For scale: Low samples up to 0.16 %, half resolution 0.19-0.42 %, SSAO off 3.4-9.6 %.
- Time, run8 (7680x3544, GPU-bound, base and Medium alternated three times each): the mean frame of the six views 61.4 -> 52.8 ms (-14 %); corridor -15 / -26 / -27 %, maze -11 / -5 / -8 %. Their ESTIMATE: Medium saves about 0.32 ms per million pixels on the M3 Max, about 0.7 ms at 1920x1080 and 2.5 ms on a 3456x2234 panel. Editor batch, so a ranking, not a budget; a player capture (row 15) gives the real figure.
- Sheet: theirs, `sheets/ta07_ssao_settings.jpg`. This stage rendered nothing, so it adds no sheet.

### 3.4 For the other rows of this run

- `W/proj_unlock` was synced to main at 13:07, before 13:23. Its `FrontRooms_URP_Renderer.asset` is still 15ec2379 (`Samples: 0`, High), its `FrontRoomsRenderSetup.cs` a871837a, and its `FrontRooms_URP.asset` fc08c486 is neither main's old nor its new copy. Every before / after of rows 16, 1 and 9 ran the same High SSAO on both sides, so those comparisons stand; their absolute milliseconds are at 12 samples, a little above what main draws now. A stage that wants main's exact frame copies main's three files into the clone first.
- The stack is now `ta_lamp_lightdata` -> `ta_door_culling` -> `ta_beams` -> `ta_lod_fade`; no `ta_ssao` sits between them.

### 3.5 Handheld and WebGL

- Handheld: Medium samples since 13:16, with SSAO from depth after the opaques (row 13) and half resolution at LOW and MEDIUM. The device A/B is 设置菜单's.
- WebGL: it shares the desktop asset and renderer (default quality level 3), so it has drawn Medium samples since 13:23 as well; that is the row's tier "all" (§2), not a WebGL-only branch.

### 3.6 Combined test rows (rule 25)

None added by 游戏视觉: 设置菜单's list carries row 3 (`handoff/settings_quality.md`: desktop SSAO at Medium samples "looks as before (corners, baseboards, under desks; tech_art sheet ta07)", and the handheld device A/B).

## Row 7 · LOD cross-fade (package `ta_lod_fade`)

Row 7 (`20_ranked_changes.md` §3.7, `11_draws_culling_lod.md` §4.1 and DA9): every LODGroup hard-swaps (fade mode None) and no project shader has `LOD_FADE_CROSSFADE`, although both URP assets have cross-fade on; props, troffer housings and door plates pop. Stage built 2026-11-04 20:18-21:5x. Parent notes for this stage (journal 13:08:56, 13:42:51): add a MotionVectors pass to `FrontRooms/Surface` in the same package (the prerequisite of row 13b, handheld STP) and prove the desktop frame identical; build on MAIN's Surface and keep our edits in the pass headers and at the fragment's clip point, so the pool tile run's `_FR_TILE` edits merge around them.

### 7.1 What is there today (main read only, 20:2x)

- Where LODGroups come from. (a) The kit importer `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` (5714cf2b, version 2): every kit FBX with `_LOD1` under `Resources/Props/Models` and `Resources/Creatures` gets `fadeMode = LODFadeMode.None` (l. 114, 121). (b) Five kit prefabs serialize `m_FadeMode: 0`: `Props/Troffer/Prefabs/Troffer_L03` (housing) and `Troffer_L03_Soil`, `Props/Lamps/{Pendant,Strip,RemotePlate}/Prefabs/ZL_PEND|ZL_STRIP|ZL_RPLATE`. (c) Runtime builders that make a group of their own: `FrontRoomsRunRestack.cs:171`, `Glass/FrontRoomsWindowFoil.cs:876, :888`, `Glass/FrontRoomsWindowSash.cs:430`, `Exit/FrontRoomsExitChain.cs:292`, `Doors/FrontRoomsDoorBreakVisual.cs:482` (all set None) and `Glass/FrontRoomsGlassFloorGlass.cs:232` (`AddComponent`, None by default). The others only set the levels of an imported group again (`FrontRoomsDoubleDoorKit` ReducedTier, `FrontRoomsRoomStream.CullWithBracket`, `FrontRoomsStationDress`, `FrontRoomsStationPaper`, `Media/FrontRoomsScreenVideo`, `AlarmTell`, `AlarmDoorLook`, `TransitionKit.Chunk`, `FrontRoomsRelayBody`), which keeps the group's mode. No LODGroup code in `Scripts/FrontRoomsMap/*` or `FrontRooms3DGame*.cs` (grep): the map instances our kits, so no contract for 关卡设计. `Assets/Scenes/FrontRoomsScreenAdsDemo.unity` holds 3 standalone groups at None (not ours; left).
- Shaders on the kits' levels (census in the clone with row 7's shaders, 229 LODGroups, `r7_lod_census.tsv`): `FrontRooms/Surface` 227, `FrontRooms/Glass` 17 (door lites, cabinet and vending fronts, the cooler bottle, ashtray, blotter), URP Lit 18 (imported FBX materials), URP Unlit 1 (troffer soil, multiply). Surface and Glass had no `LOD_FADE_CROSSFADE`; URP's shaders have it. No renderer sits in two levels of a kit.
- URP: `FrontRooms_URP.asset` (9c3c038b) and `FrontRooms_URP_Handheld.asset` (b46205e5) `m_EnableLODCrossFade: 1`, `m_LODCrossFadeDitheringType: 1` (Blue Noise); every quality level `enableLODCrossFade: 1` (`QualitySettings.asset` c1aac3f8).
- LOD bias: the desktop player's default level Ultra 2.0, so every switch happens at twice the authored distance (troffer housing 8 m and 24 m, push plate 3 m and 12 m, kick plate 5 m and 20 m, the cart 8 m); Handheld 0.7 (iPhone default level 6); WebGL High 1.0; the Editor's current level High 1.0 (the probe views below ran there, as tech_art's did; the film sets 2.0).

### 7.2 The change

- `Assets/Scripts/Rendering/FrontRoomsLodFade.cs` (new, 641946bc; .meta guid 849587ffccec4553ba92a61d90852ed1): `Apply(group)` = Cross Fade, animated, and each level's `fadeTransitionWidth` for a dissolve over the last 10 % of its switch distance; `Enable(group)` = the mode only, for a group whose levels are set later. Animated, not distance-based: the switch stays at the authored distance, the dissolve lasts `LODGroup.crossFadeAnimationDuration` (Unity's 0.5 s; measured 30 frames at the fixed 1/60 s, so it runs on game time), a still frame always shows one level, and a player standing at a switch distance never sees a half-dithered object (a distance band would leave it dithered for as long as he stands there). The widths matter only for the GPU Resident Drawer (row 6), which drops animated fades to distance-based ones.
- Callers: the importer (version 2 -> 3, so every model reimports once in each project; `Apply` at both former None sites), the five prefabs (`m_FadeMode: 1`, `m_AnimateCrossFading: 1`, widths in float32 from the same formula; read back after import against `FrontRoomsLodFade.Width`: `r7_prefab_check.txt` PASS), and the builders: RunRestack, WindowFoil (both groups), WindowSash, ExitChain, DoorBreakVisual (`Apply` after `SetLODs`), GlassFloorGlass (`Enable`, its levels are set later). DoorBreakVisual's leak cards sit in LOD0 and LOD1 at once; the rig (7.3) shows Unity draws such a renderer whole through a fade, so it stays in. DoorBreakVisual is stacked on `ta_lamp_lightdata`'s payload (51de8a76).
- `FrontRooms/Surface` (dae1432d -> 9bf91c5c): `#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE` in ForwardLit, ShadowCaster, DepthOnly, DepthNormals and the new MotionVectors pass. Fragment-only (URP's Lit declares it for both stages; none of our vertex programs reads it), so the vertex variants do not double. ForwardLit clips with URP's `LODFadeCrossFade(positionCS)` after the last texture read and before the lighting, where URP's Lit does (`LitForwardPass.hlsl:249`): the reads before it take derivatives over the whole 2x2 quad. Depth, depth-normals and motion vectors use URP's pass code with the same screen-space dither as the colour pass, so SSAO and the prepass see the same pixels. ShadowCaster: no dither goes into a shadow map; the two levels hand over at mid-fade instead (`clip(x - (x >= 0 ? 0.5 : -0.5))`; `unity_LODFade.x` is x for one level and -x for the other, core `Common.hlsl` `LODDitheringTransition`), so exactly one level casts at any moment and nothing in a shadow can crawl as the cascades step or the shadow atlas re-packs. Without the keyword every pass is the code it was.
- `FrontRooms/Glass` (e135b40e -> 4fb9fad6): the same keyword in ForwardLit and FRGlassRTPrepass, the clip last (after every derivative of the pane). Without it a kit's pane would draw twice for the half second (doubled reflection and tint).
- MotionVectors pass (Surface): URP 17.6's `ObjectMotionVectors.hlsl`, `ColorMask RG`, `Cull [_Cull]`, no alpha test (Surface never clips). URP draws it only for a camera that asks for motion vectors (STP, TAA, motion blur); the desktop asks for none (MSAA, no TAA, no motion blur), and the identity capture below shows the frame and every count unchanged. For 设置菜单 (row 13b): `FrontRoomsQuality.SurfaceWritesMotionVectors()` (6028b19d, l. 429) looks for a pass whose LightMode is MotionVectors on `FrontRooms/Surface`; after `ta_lod_fade` lands it finds one (pass 4). One catch, measured (pass probe, `W/techart_visual/captures/r7/probe/r7_pass_probe.txt`, Editor batch in the clone, 21:47): before anything has drawn a shader, `Shader.passCount` reads 1 and `FindPassTagValue(0, "LightMode")` is empty, for `FrontRooms/Surface`, `FrontRooms/Glass` and even URP's Lit, whose MotionVectors pass is there; after one camera has drawn them: Surface 4 passes (UniversalForward, ShadowCaster, DepthOnly, DepthNormals; the clone's own Surface, without row 7), Glass 2, URP Lit 9 with MOTIONVECTORS. The capture's play sessions read the same at their first frame. So in the Editor `SurfaceWritesMotionVectors()` answers false until Surface has drawn once, even with the pass; a player was not checked. Asking after the first rendered frame (or again whenever it answered false) avoids it.

### 7.3 Evidence (capture `W/techart_visual/captures/r7/run1`, 2026-11-04 21:04-21:09, load 6-9: not under load)

Kit (clone only, `W/proj_unlock`, never packaged): `Assets/Scripts/CloneOnly/FrontRoomsTechArtR7Capture.cs` (acts only with `-frR7Out`) and `Assets/Editor/TechArtVisual/FrontRoomsTechArtR7.cs`; launcher `W/techart_visual/tools/r7_run.sh`. One Editor batch: sessions row7 (row 7's shaders + `FrontRoomsLodFade.Apply` on every LODGroup in the scene, what the importer, prefabs and builders give them), shader (row 7's shaders, groups as today), base (main's Surface dae1432d and Glass e135b40e swapped in by the batch); C's own Surface (the pool tile run's 5f96f272) went back at 21:08:43, each swap only while the file was still the one this run wrote. Views: the tech_art probe's frozen corridor0/1/2 and handoff_fwd/back, 1920x1080 raw PNG, grain, dithering and SSAO noise off. Film W: after the game's start hand-off, the terminal door held shut (`TerminalDoorHeld`, the game's own hold while the map loads), LOD bias 2, a janitor cart (`Kit_JanitorCart`, a kit prop) by the door, the camera at eye height 1.62 m walking +Z at 1.44 m/s from 16 m to 2 m before the door, one engine render per frame, every LOD change of every group in the two rooms logged per frame. Rig: three Unlit quads under one LODGroup far from the rooms, A red in LOD0, B green in LOD1 1 mm behind, S blue in both, the camera backing across the switch at 1.44 m/s.

| check | today | row 7 shaders, nothing fading | row 7 + cross-fade |
|---|---|---|---|
| probe views, px > 1/255 against today (5 views) | | 0 in all 5 (corridor0: 3 px at exactly 1/255 beside the title TV, whose video runs on the clock; the others max 0) | 0 outside the TV box in all 5 (no fade in progress at the probe's freezes) |
| draws / SetPass / tris / casters / visible lights | 710/83/122.7k/171/20(5), 731/71/128.3k/245/24(8), 831/100/189.6k/386/35(12), 549/98, 544/90 | equal in every view | equal in every view |
| film W frame by frame (JPG q92) | | 131 of 146 saved frames byte-equal, 15 with one 8x8 block at 1-2/255 (no px > 8/255) | 285 frames hold a fade (237 with px > 8/255, most 1,808 px at f204); of the other 299, 287 byte-equal and 12 with single 8x8 blocks at 1-8/255 (no px > 8/255) |
| extra draws while a level fades (row 7 minus today, same frame) | | 0 | housing +2 draws +2 SetPass (+432 tris), 4 push plates +8 / +2, cart +36 / +9 (+17.7k tris); 285 of 584 frames hold a fade, outside them the counts are today's |

The sparse 8x8 blocks outside fades (1-8/255, never a pixel over 8/255) appear with row 7's shaders in both comparisons and follow no LOD event; no second today session was run to measure the session-to-session noise of the film, so they are not traced further. The identity proof is the probe views in raw PNG: max 0 in four views, 3 px at exactly 1/255 beside the title TV in the fifth.

Fades (film W, events `r7_lod_events.tsv`): every switch happens on the same frame in all three sessions (cart LOD1 -> LOD0 at 7.999 m, f203; housings at 7.99 m; push plates 12.0 m and 3.0 m; kick plates 5.0 m); today swaps in one frame, row 7 dissolves over exactly 30 frames (0.50 s). Rig (`r7_rig_recount.txt`; the batch's own colour counter missed the post stack's graded green, so the crops were re-counted by nearest colour): the switch at 6.012 m (built for 6 m); today and the shader session swap A for B in one frame; row 7 red / green / background px in A's box f63 4414 / 23 / 0, f70 2891 / 1359 / 0, f80 918 / 2970 / 0, f90 74 / 3468 / 0: the two levels' dithers are complementary (no background shows through at any point of the fade), and S, in both levels, stays whole (0 background px).

Shadows: the cart is the only shadow caster that fades in film W (housings and door plates cast none). Its two levels hand their shadow over at f218, half way through the fade; on the floor below the cart no pixel moves by more than 12/255 against today in any frame of the fade (the two levels cast alike), and with no dither in the shadow map there is nothing that could crawl.

### 7.4 Compile check, variants, build size

- Offline compile check (`W/techart_visual/captures/r7/run1/r7_shader_check.txt`; the same Editor batch, 21:08-21:40, 1,893 s; the method of the pool look's `FrontRoomsPoolSurfaceCompileCheck`, `ShaderData.Pass.CompileVariant`, extended to every pass and to the new keyword; row 7's Surface and Glass compiled from their text under hidden names, so the clone's own Surface stayed the pool tile run's): **323 compiles, 0 failed** -> PASS.
  - Surface ForwardLit: 21 keyword sets (the 14 sets of the clone's 258 Surface materials, each `shader_feature` alone, none, the pool check's `_FR_PRINT` pairs), each without and with `LOD_FADE_CROSSFADE` on Metal macOS (no globals; the full global set), Metal iOS, D3D and GLES3x (WebGL) with the full global set (`_MAIN_LIGHT_SHADOWS_CASCADE _ADDITIONAL_LIGHTS EVALUATE_SH_MIXED _ADDITIONAL_LIGHT_SHADOWS _SHADOWS_SOFT _SCREEN_SPACE_OCCLUSION _LIGHT_COOKIES _LIGHT_LAYERS _CLUSTER_LIGHT_LOOP FOG_EXP2 INSTANCING_ON`), plus the vertex program: 231.
  - ShadowCaster, DepthOnly, DepthNormals, MotionVectors: without and with the keyword on Metal macOS, Metal iOS (instancing), D3D, GLES3x, and each vertex program on Metal and D3D (the shadow one with `_CASTING_PUNCTUAL_LIGHT_SHADOW`).
  - Glass ForwardLit: 4 sets (the 3 of the clone's 6 Glass materials, none, each feature, both), without and with the keyword on Metal macOS (with and without `_FR_GLASS_RT`), iOS, D3D, GLES3x; FRGlassRTPrepass without and with on Metal macOS, iOS, D3D (with and without `_FR_GLASS_GRIME`).
  - Control: the same Surface text with one undeclared identifier inside each `LOD_FADE_CROSSFADE` branch (ForwardLit's clip, the shadow hand-over) fails with the keyword (ForwardLit Metal and D3D, ShadowCaster Metal: 3 of 3) and compiles without it (2 of 2): the check really compiles the keyword's variants.
- Variants and size. Every pass's fragment programs double (fragment-only keyword: the vertex programs do not), and a keyword variant is as large as its twin (ForwardLit, Metal macOS, 49 pairs: 9.63 MB without, 9.68 MB with, x1.005). In the Mac player built at 13:2x (`W/techart_visual/logs/build_today.log`, after URP's stripping): Surface 335 Metal programs (ForwardLit fragment 320, vertex 8, the other passes 1-2 each), 58.11 MB -> 9.09 MB packed; with row 7 about 662 programs (+320 ForwardLit, +3 shadow / depth / depth-normals, about +4 for MotionVectors), about 18 MB packed. Glass 103 programs, 2.51 MB packed -> about 201, about 4.9 MB. ESTIMATE: about +11 MB of packed shader data per graphics API in every player (Mac Metal; the same order for Windows D3D, iOS and WebGL), against a 2.7 GB Mac build. Unity's own variant count (`ShaderUtil.GetVariantCount`, internal) did not answer by reflection in 6000.6 (wrong parameter count), so the figures above come from the build log.
- Build time: in that build Surface's ForwardLit fragment programs took 1,428 s wall (21,600 CPU-s, UNDER LOAD); the first build after landing compiles the new 320 once, later builds take them from the shader cache.
- Re-check after the rebase (main's Surface moved to 313c18c3 at 21:47; the package now carries 59d036b0): the same batch method in quick mode (`W/techart_visual/captures/r7/recheck/r7_shader_check.txt`, 21:59-22:05, 316 s), 8 sets that reach every `shader_feature` branch (none, `_FR_MESH_UV`, `_EMISSION _FR_LAMP_FIELD _FR_MESH_UV`, `_FR_PRINT`, `_FR_TILE`, `_FR_MESH_UV _FR_SHEEN`, `_FR_DAMAGE _FR_MESH_UV`, `_FR_OWN_LIGHTS`), each with the keyword on Metal macOS (no globals; full), Metal iOS, D3D and GLES3x plus one variant without it, and every other pass as above (Glass unchanged, skipped): **88 compiles, 0 failed**; control 3 of 3 fail with the keyword, 2 of 2 compile without -> PASS. Compiled against the clone's `FrontRoomsWaterSubmerged.hlsl` (8626e5b0; main's is 2243a740 since 21:47): row 7's hunks do not touch the water code, and without the keyword the text is main's own.

### 7.5 Sheets and film

- `research/tech_art/sheets/r7_lod_fade.jpg` (fit: `W/techart_visual/sheets/r7_lod_fade_fit.txt`, ALL LABELS FIT): per object (janitor cart 8 m, troffer housing 8 m, push plates 3 m, kick plates 5 m) today the frame before and the frame of the switch, what popped (today minus cross-fade at the switch, x8), cross-fade half way; crops at whole multiples with square pixels; cost, shaders, rig, desktop proof.
- `research/tech_art/sheets/r7_identity.jpg` (fit ALL LABELS FIT): the probe views today | row 7 shaders | row 7 + cross-fade | difference x32, the counts, the film's frame-by-frame numbers.
- `research/tech_art/video/r7_lod_fade.mp4` (3840x1382, 60 fps, 1544 frames, 25.7 s, 17.7 MB, no audio stream; sha1 2ba230a4): the walk in real time, today beside cross-fade, then each switch slowed 4x and enlarged (cart, housing, kick plates, push plates).
My look: at the desktop's LOD bias 2 the pops were already small (the levels swap 8 m away, the push plates' screws 3 m away), so the gain is a calmer walk rather than a new look. The plates' screws and the cart's bucket and handle no longer jump; the blue-noise dither shows only during the half second and only where the two levels differ. Nothing else in the frame moves.

### 7.6 Package and gate

Re-checked at 22:1x (section Package, P.4): unchanged, every main base unmoved.

`W/apply/ta_lod_fade.sh` (v2; 52724001), 16 paths: NEW `Assets/Scripts/Rendering/FrontRoomsLodFade.cs` 641946bc + `.meta` 806c0592; `FrontRoomsSurface.shader` 313c18c3 -> 59d036b0 (main moved at 21:47 with the parent's `water_light_fix_stage`, dae1432d -> 313c18c3; row 7's hunks, built and checked as dae1432d -> 9bf91c5c, merged three-way onto it without a conflict, their hunk in the lighting after our clip); `FrontRoomsGlass.shader` e135b40e -> 4fb9fad6; `Editor/Rendering/FrontRoomsKitImporter.cs` 5714cf2b -> 2fc70c14; prefabs `Troffer_L03` 819abb4f -> 166c1cf3, `Troffer_L03_Soil` 792e6abc -> 8a2e819c, `ZL_PEND` ad9b5e6c -> 575fa76f, `ZL_STRIP` fda980cd -> 0223d39f, `ZL_RPLATE` 9d9ea800 -> bef3af5a; `FrontRoomsRunRestack.cs` e2e71f90 -> 139ba5c4; `Glass/FrontRoomsWindowFoil.cs` e4408cbc -> 884a2c2d; `Glass/FrontRoomsWindowSash.cs` cf40b01b -> a9468258; `Exit/FrontRoomsExitChain.cs` d3d71ea9 -> d68dd68f; `Glass/FrontRoomsGlassFloorGlass.cs` 034e0236 -> 65d0e390; `Doors/FrontRoomsDoorBreakVisual.cs` 51de8a76 (`ta_lamp_lightdata`'s payload) -> db66bc4e; 14 `.meta` kept. Gate: `fr gate` 8 overrides, FR GATE OK macOS + iOS, 0 errors (`W/techart_visual/logs/gate_r7.txt`); shader compile check 7.4. Dry run on main: REFUSE only at the stacked base (DoorBreakVisual main 912c1ec6 != 51de8a76). Rehearsal on a scratch copy of main (`W/techart_visual/tools/r7_rehearsal.sh`): G2 -> `ta_lamp_lightdata` MANIFEST OK -> `ta_door_culling` MANIFEST OK -> `ta_beams` MANIFEST OK -> `ta_lod_fade` DRY RUN OK -> `--apply` MANIFEST OK (16 touched, 14 kept) -> again ALREADY APPLIED. Land order: after `ta_lamp_lightdata` (DoorBreakVisual); independent of `ta_door_culling` and `ta_beams`. The pool tile run's `pool_tile_real` (PACKAGE READY 20:47, Surface dae1432d -> 5f96f272) and this package both change the Surface: three-way merges are clean both ways (theirs onto dae1432d + ours: 6fc49055, our clip still after their tile derivatives; theirs onto this package's 59d036b0: c129a7c7; `W/techart_visual/tmp/r7/merge_test`, `merge_main`); whichever lands second goes through `W/tools/rebase_apply.sh`.

### 7.7 Handheld and WebGL

- The same change reaches every tier (row 7's tier is "all"): both URP assets have cross-fade on, so URP keeps the keyword's variants on iOS and WebGL too; a fade costs the same few draws for half a second. On handhelds the door plates already start at LOD1 (`FrontRoomsDoubleDoorKit` ReducedTier) and the troffers are lens only, so fewer groups fade there.
- LOD bias on handhelds is 设置菜单's (quality levels and presets): contract `W/apply/ta_lod_fade_contract/` (README, `CONTRACT_settings.diff`: Handheld level `lodBias` 0.7 -> 1, as tech_art §3.7 proposes; against `QualitySettings.asset` c1aac3f8, `patch --dry-run` clean). Not measured on a device (no iOS run). Desktop stays at 2 (quality); the Editor's level 3 at 1 is unchanged.
- WebGL: the extra variants enlarge the WebGL shader data too (7.4); the WebGL track may turn cross-fade off in a WebGL-only URP asset if the download matters more than the dissolve (URP then strips the variants).

### 7.8 Not covered, open

- Outlets, switches and jacks (`Office/FrontRoomsWallFixtureRenderer.cs`) draw instanced, without LODGroups, and switch their own levels by distance (1.5 / 4 / 12 m at LOD bias 1, scaled by the bias as a LODGroup is): they still swap at once. A cross-fade there needs a per-instance fade value; a later row.
- The ScreenAds demo scene's 3 groups (not ours).
- GPU Resident Drawer (row 6): the animated fades fall back to the widths set here (last 10 % of each distance).

### 7.9 Combined test rows (rule 25; after `ta_lamp_lightdata`, then `ta_lod_fade`)

- Editor: first open after landing reimports every model once (importer version 3). Walk the Level 0 maze and an Office zone at the slow gait: props and housings dissolve over about half a second at their switch distances, no pop; no double-drawn glass on cabinets, vending machines or door lites while they fade.
- Desktop Mac player: same walk; frame time unchanged outside fades.
- Handheld (when 设置菜单 has decided the bias and once STP is on below 100 %): no smear on fading props (motion vectors carry the fade).

## Package · the stack rebuilt over main (stage Package, 2026-11-04 22:0x-22:2x)

Every base was checked against main at 22:07 (none of this run's packages had landed: no `ta_*_applied_manifest.txt` in `W/apply`). Main had moved under three of them:

- `Assets/Scripts/FrontRoomsRoomStream.cs` 32482f23 -> b67034b1: **title_stream_rooms** landed at 20:02 (Red 19:3x in the 游戏视觉 chat, "全部按推荐": TF-1 Tour), with the new partial `FrontRoomsRoomStream.TitleRooms.cs` (94d456b0) and `Scripts/Rendering/Title/FrontRoomsTitleVignettes.cs` (0e0c98b8). `FrontRoomsRoomStream.Programme` now defaults to Tour; at Today every title hook returns at once.
- `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` c0c64842 -> 1c1fd71c (run_red_looks 17:43, run_red_pulse 20:11).
- `Assets/Scripts/FrontRooms3DGame.cs` 030c4a82 -> a9317638 (关卡设计's `title_c1`, 20:04). G2 is still not in main: l. 2151 destroys the stream's Light components as before.

Unmoved: every other base, all of `ta_lod_fade`'s (Surface 313c18c3, Glass e135b40e, the importer, the five prefabs, the builders), DoorBreakVisual 912c1ec6, HeldItem, RelayBody, ShotLook. The superseded package lists are kept in `W/techart_visual/tmp/pkg/old/`; mkapply moved the old payload and base folders to `W/_partial/`.

### P.1 `ta_lamp_lightdata` (row 16), rebuilt on main

The same edit script on main's six files (`W/techart_visual/tools/r16_edits.py`, 10 single-match edits): RoomStream b67034b1 -> **d988bce5**, RunRig 1c1fd71c -> **c9292733**; the other four payloads are unchanged (DoorBreakVisual 51de8a76, HeldItem 71eb6383, RelayBody 01b56cb2, ShotLook 360e58cf). A three-way merge of the old payload over the new main gives the same two files byte for byte. title_stream's RoomStream makes no Light beyond the two sites row 16 edits, and the vignettes' lamps call `FrontRoomsMapWorld.ShippedShadowFilter` themselves. `W/apply/ta_lamp_lightdata.sh` **f1870e76** (mkapply, then `tools/r16_add_wait_gate.py`): 6 replaced paths, 6 `.meta` kept. Dry run on main: bases, kept metas and payload OK, then WAITING (exit 3) on G2.

G2 re-made on main's a9317638 (after: **408a0315**): the hunk text is byte-identical to the 15:02 one (same `@@ -2148,7 +2148,14 @@`), `patch --dry-run` clean; the contract README carries the new sha1s. Until G2 is in, title_stream's `ReleaseTitleLampData` strips the URP data of the vignette lamps at `CloseTerminalDoor` (title programmes only); the corridor troffers and the Run exit sign light rely on G2.

### P.2 `ta_door_culling` (row 1), rebuilt over title_stream

`git merge-file --diff3` of row 1 (81a23e5c over its base e2ae59c1) onto the rebuilt row-16 RoomStream d988bce5: three conflicts, each where title_stream's hooks meet row 1, resolved by `W/techart_visual/tools/pkg_r1_resolve.py`:

- the `RoomSlot` fields: both blocks kept (title rooms, then the portal state);
- the end of `Tick`: `UpdateTitleLook()` first, then `UpdatePortals(dt)`, so a family's look change is drawn or hidden in the same frame;
- `SetFixtureOutput`: row 1's `ApplyLampLight` call; title_stream's `&& !room.lightsDimmed` (DimRoom, a shut title door behind the camera) moves into `ApplyLampLight`'s `light.enabled`.

The door culling also implements title_stream's partial `TitleVignetteMoved`: a vignette that joins a slot out of sight is hidden with it and joins the slot's parts list, so it comes back with its room; leaving a slot, it is drawn again. The result, **43557490**, is byte-identical to title_stream's own resolution (`W/title_stream/stacked_on_ta`, which they gated at 19:17). `W/apply/ta_door_culling.sh` **8ff70986** (`tools/mkapply_stacked.py --after` the row-16 payload): 1 replaced path d988bce5 -> 43557490, `.meta` kept. Dry run on main: REFUSE at the stacked base only (main b67034b1, base d988bce5).

### P.3 `ta_beams` (row 9), rebuilt over title_stream

`git merge-file --diff3` of row 9 (c6f28e70 over 81a23e5c) onto 43557490: one conflict, in `Initialize` (title_stream's `BeginTitleRooms(false)` stays, `EnsureVolumetricLightMaterial()` goes). Two more fits keep both sides' intent (`W/techart_visual/tools/pkg_r9_resolve.py`):

- Main's `TitleRooms` partial read `RoomSlot.volumetricRenderers` in three statements, to park a slot's own beams while a vignette with lamps of its own holds the slot and to give them back after. With the beams gone there is nothing to park: the three statements go (`FrontRoomsRoomStream.TitleRooms.cs` 94d456b0 -> **489c721b**, `.meta` kept) and so does the then unused `RoomSlot.ownBeams`. The dust shafts keep that intent by themselves: a shaft shows only the lamp that hangs under its own fixture (`RoomSlot.dustLights`), so a vignette's lamps swapped into the slot's channels leave it dark.
- `SetRoomDust` paints the shafts in title_stream's `RoomLightColor(room)` (the family's lamp colour; the profile's, as before, in a room without a family), the colour `ApplyFixtureVisual` already hands `ApplyDustShaft` on every lamp tick.

Row 9's own changed lines are carried over exactly (189 sorted +/- lines before, 190 after: the `ownBeams` declaration and one blank line). RoomStream 43557490 -> **6893ef1a**. `W/apply/ta_beams.sh` **f6436bef** (`--after` the row-16 and row-1 payloads): 6 paths, 4 new (`Scripts/Rendering/FrontRoomsDustShaft.cs` 26c427f7 + `.meta`, `Resources/Rendering/FrontRoomsDustShaft.shader` 03df0eaa + `.meta`; GUIDs 79076ea2 and af764fcf still unused in main), RoomStream and TitleRooms replaced, both `.meta` kept. Dry run on main: REFUSE at the stacked RoomStream base only.

### P.4 `ta_lod_fade` (row 7), unchanged

`W/apply/ta_lod_fade.sh` **52724001** as built at 21:51: every main base unmoved, and its stacked DoorBreakVisual base (51de8a76) is the rebuilt row-16 payload's. Dry run on main: REFUSE at the stacked DoorBreakVisual only.

### P.5 Gate and rehearsal (rule 25)

| check | result | log (`W/techart_visual/logs/`) |
|---|---|---|
| `fr gate --staging`, row 16's 6 files | FR GATE OK, macOS + iOS 0 errors, 6 overrides, Assembly-CSharp 273 files | `pkg_gate_lamp.txt` |
| the same + G2 | OK, 7 overrides | `pkg_gate_lamp_g2.txt` |
| row 16 + row 1 (RoomStream 43557490) | OK, 6 overrides | `pkg_gate_door.txt` |
| row 16 + row 9 (RoomStream 6893ef1a, TitleRooms 489c721b, DustShaft.cs) | OK, 8 overrides, 274 files | `pkg_gate_beams.txt` |
| the whole stack + G2 (rows 16, 1, 9, 7: every `.cs` of the four payloads + `FrontRooms3DGame.cs`) | OK, 16 overrides, 275 files | `pkg_gate_stack.txt` |
| dry runs on main | row 16 WAITING on G2; rows 1, 9, 7 REFUSE at their stacked bases only | `pkg_dry_*_main.txt` |
| rehearsal in landing order on a scratch copy of main (`tools/pkg_rehearsal.sh`, `tmp/pkg/rehearsal_222141`, APPLY_REAL) | row 16 WAITING without G2; G2 patched; then each of `ta_lamp_lightdata`, `ta_door_culling`, `ta_beams`, `ta_lod_fade`: DRY RUN OK, `--apply` MANIFEST OK (6 / 1 / 6 / 16 paths, 6 / 1 / 2 / 14 `.meta` kept), again ALREADY APPLIED | `pkg_rehearsal.txt` |

Each gate ran in its own work dir (`W/tools/_fr/gate/ta_pkg_*`), main head c9c80b61 plus its working tree at 22:1x.

### P.6 What this stage did not redo, and why

- No Unity run: no shader text changed (the dust shaft 03df0eaa compiled 24 / 0 at 19:43; Surface 59d036b0 88 / 0 at 22:05; main's Surface is still 313c18c3).
- The frame proofs of rows 1 and 9 stand for the title at Today by construction: every title hook returns at once there (`TitleRoomsOn` false), `lightsDimmed` stays false, `TitleVignetteMoved` is never called, and `RoomLightColor` is `ProfileLightColor` without a family. With Tour, the default since 20:02, they are new and go to the combined test (`W/techart_visual/combined_rows.md`: R1-2, R9-2).
- The after-player capture of row 16 (cancelled at 14:45 for the Unity slot) is a combined-test row (R16-2), with the exact commands.

## Landing order and open items

Written by the Package stage, 2026-11-04 22:2x. Nothing of this run is applied; the 游戏视觉 session on the ledger lease lands each package with CLAIM, `--apply`, LANDED and a MERGED row, re-running the dry run first. A package that REFUSEs because main moved again goes through `W/tools/rebase_apply.sh <track>`; for the stacked RoomStream, re-run `tools/pkg_r1_resolve.py` / `tools/pkg_r9_resolve.py` on fresh diff3 merges.

### L.1 Landing order, and what each changes for the player

| step | what | whose | what the player gets |
|---|---|---|---|
| 1 | G2: `W/apply/ta_lamp_lightdata_contract/CONTRACT_map.diff` into `FrontRooms3DGame.cs` (a9317638 -> 408a0315; one hunk) | 关卡设计 applies it | Nothing by itself: the URP data of a stream lamp goes right before its Light, so the start door's lamp fade ends without an error. |
| 2 | `bash W/apply/ta_lamp_lightdata.sh --apply` (6 paths) | 游戏视觉 | Nothing in the shipped filter (Low, as players drew it all along); the Editor and every clone now render what ships. Our lamps' shadow bias drops to 60 % (URP kernel 2.5 -> 1.5): in the corridor probe views 10-14 pixels move by 2-3/255 along contact shadows (the maze's lamps took the same change with 关卡设计's landing at 14:18: at most 11/255, on 0.68 % of the densest view). |
| 3 | `bash W/apply/ta_door_culling.sh --apply` (RoomStream) | 游戏视觉 | The title looks the same (no pixel over 2/255 in any measured view or film frame); rooms the title camera cannot see are not drawn and their lamps fade: draws -30 / -47 / -29 %, visible lights 24 -> 12 and 35 -> 23 in the probe views; most on the 32-light tiers and WebGL. |
| 4 | `bash W/apply/ta_beams.sh --apply` (RoomStream, TitleRooms partial, 4 new) | 游戏视觉 | The faint glow under each corridor lamp goes (about 1 % of the lamp; at most 11-24/255 on 0.02-0.22 % of pixels); -7..-21 draws and -7..-19 SetPass a view. Dust shafts exist as an API until a family asks for them (none does yet). |
| 5 | `bash W/apply/ta_lod_fade.sh --apply` (16 paths), any time after step 2 | 游戏视觉 | Props, troffer housings and door plates dissolve over 0.5 s at their LOD switch instead of popping; the desktop frame is otherwise identical; the first Editor open reimports every model once (importer v3); about +11 MB of packed shader data per graphics API (ESTIMATE). |
| after 5 | tell 设置菜单与画质预设 that `FrontRooms/Surface` has its MotionVectors pass (their handoff item 0a waits for this message), with the `passCount` catch (L.2) | 游戏视觉 | Their STP path for handhelds below 100 % (row 13b) can switch on. |

Paperwork per landing: the combined-test rows in `W/techart_visual/combined_rows.md` (copy each block into `handoff/visual_combined_test.md` as its package lands), the VL spec `W/vl_pending/tech_art.md`, this doc and its sheets and films as an own-folder landing (`W/techart_visual/to_main/Documentation/research/tech_art/`: this file, `sheets/r16_lamp_lightdata.jpg`, `r1_door_culling.jpg`, `r9_beams.jpg`, `r9_dust_shaft.jpg`, `r7_lod_fade.jpg`, `r7_identity.jpg`, `video/r1_door_culling.mp4`, `r9_dust_shaft.mp4`, `r7_lod_fade.mp4`; one LANDED row).

If G2 cannot land soon, the stack waits at step 2 (rows 1 and 9 carry row 16's two RoomStream lines; row 7's DoorBreakVisual carries row 16's line). The way out, only on the lease holder's word because it reorders the stack: re-derive rows 1 and 9 onto main without row 16 (the same resolve scripts on a diff3 onto b67034b1) and row 7's DoorBreakVisual onto main's 912c1ec6 (`rebase_apply.sh ta_lod_fade`), then land 3, 4, 5 and row 16 last.

### L.2 Contracts and asks, by owner

| owner | contract / ask | what it needs | blocks |
|---|---|---|---|
| 关卡设计 | `W/apply/ta_lamp_lightdata_contract` (README + `CONTRACT_map.diff`): G2, one hunk in `FrontRooms3DGame.cs` (`GetComponent<UniversalAdditionalLightData>()` + `DestroyImmediate` before `Destroy(streamLights[i])`), made on a9317638, `patch --dry-run` clean, inert alone; Roslyn with it 7 and 16 overrides, 0 errors | apply it as given (their file, rule 3) | the whole stack (step 1) |
| 关卡设计 | note only: `Structures/FrontRoomsStructuresWell.cs:660` makes a lamp without `ShippedShadowFilter` (shadows None, so nothing on screen changes; one rule for every runtime light) | their call | nothing |
| 设置菜单与画质预设 | `W/apply/ta_lod_fade_contract` (README + `CONTRACT_settings.diff`): Handheld level `lodBias` 0.7 -> 1 in `QualitySettings.asset` (c1aac3f8, clean), optional, either order | their call; a device look (iPhone level 6) | nothing |
| 设置菜单与画质预设 | row 13b: the MotionVectors pass lands in `ta_lod_fade`; `FrontRoomsQuality.SurfaceWritesMotionVectors()` (6028b19d, l. 429) answers false before Surface has drawn once (`Shader.passCount` reads 1 for every shader then): ask after the first rendered frame, or again when it answered false | their code (a one-line timing change if they want it) | STP on handhelds below 100 % |
| 设置菜单与画质预设 | row 9: a quality tier that drops the camera depth texture must keep it while a dusty family is in view (both URP assets ask for it today); the shafts draw with ZTest Always and stop only at the scene depth | nothing today (no tier drops it, no family is dusty) | nothing |
| title_stream run (our line, no contract) | rows 1 and 9 are built over its landed code (P.2, P.3); its next package rebases over RoomStream 6893ef1a and TitleRooms 489c721b once these land; a dusty family calls `SetRoomDust(room, level)` where its slot takes the family (`AttachVignette`) and `SetRoomDust(room, 0f)` in `ReleaseVignette` | a look round per family for the level | nothing |

Row 3 (SSAO samples) was 设置菜单与画质预设's own landing (13:16, 13:23; section Row 3). Rows 2, 11 and 12 were 关卡设计's (`fill_shadow` 13:00, `lamp_tick` 13:04, `light_budget` 13:11), row 16 on their sites too (`lamp_filter_low` 14:18). Rows 13 and 13b's settings side was 设置菜单's (13:16).

### L.3 Open items and the next runs

| row | where it goes | what it needs, from whom |
|---|---|---|
| 4 Corridor spot shadows per room family | the title_stream run | The mechanism is in main since 20:02 for the title programmes (`FamilyShadows`, `ApplyShadowRoles`: Level 0 and pool rooms cast none, a family riding the slot's L03 casts from up to 4 lamps over its furniture as a tight cone; Today untouched unless `FamilyShadowsInToday`). Left: the furnished-room look round per family (Office, Run! B, waiting room, pool), captured in the honest state of row 16 (filter Low); per family, whether kept lamps earn Medium or High (set after the creation call in our code: `GetUniversalAdditionalLightData(light).softShadowQuality`); whether the Lobby title at Today takes it. Needs Red's eye on the round; nothing from other lines. |
| 5 MaterialPropertyBlock diet | next run of 游戏视觉, with a contract to 关卡设计 | Lamp level through `Renderer.SetShaderUserValue` (row 9's dust shafts already use it: one shared material, colour, level and seed in 32 bits), steady lamps on shared materials, the maze shell's `_CeilingHeight` into a vertex channel. Ours: RoomStream's lens and halo blocks (`ApplyFixtureVisual`), `FrontRooms/Surface` reading `unity_RendererUserValue`. 关卡设计's: the map's lamp tick blocks and the maze shell in `FrontRoomsMapWorld.cs` (contract). Prerequisite of row 6. |
| 6 GPU Resident Drawer + GPU occlusion culling | after row 5 | One `DOTS.hlsl` include per pass of `FrontRooms/Surface` (the probe's `W/tech_art/probe/grd/` prototype) and BRG variants Keep All; desktop and handhelds, never WebGL. Needs 设置菜单与画质预设 for the Graphics setting and the URP assets' GPU Resident Drawer and occlusion switches (their call, VISUAL_STANDARD "shared URP asset"); a look check (0.1-0.7 % of pixels moved in the probe), variant count, build size and build time. Row 7's fade widths (last 10 %) are already set for GRD's distance-based fades. |
| 8 Box-projected reflection probe per room family | after the families settle (title_stream) | One cube per family captured once in the lit state, a child of the room root (travels with recycling and the 192 m rebase), box projection in `FrontRooms/Surface`; ships only with an A/B capture per family that shows the gain (Run B's per-room probes were dropped for no visible gain). Needs 设置菜单与画质预设 for probe blending and box projection on the desktop asset; 关卡设计 for the realtime Structures probes (time-sliced or captured offline); Red's eye on the A/B. |
| 10 Authored decals | a run of its own | DBuffer in `FrontRooms/Surface` (`_DBUFFER_MRT1/2/3`, `ApplyDecalToSurfaceData`), one Decal feature on Automatic (DBuffer on desktop, Screen Space on phones and WebGL), one decal atlas of staged wear (Red's "staged assets, not shader tricks"); reuse the depth prepass. Needs 设置菜单与画质预设 for the renderer feature on both renderers; 关卡设计 for placing wear where its cause is in the map; real reference first (VISUAL_STANDARD 1.1). |
| 13 Handheld renderer | 设置菜单与画质预设 (landed 13:16) | Left: their device A/B on iPhone and iPad (After Opaque can over-darken). Ours: nothing more. |
| 13b STP below 100 % on handhelds | 设置菜单与画质预设 (code landed 13:16) + `ta_lod_fade` (the pass) | Left after step 5: their device check (ghosting on flicker), the `passCount` timing (L.2), and 叙事设计's two STP strings (their handoff). |
| 14 Trim sheet + prop swatch atlas | kit by kit, as 游戏视觉 touches each kit (`Tools/Blender/frontrooms_kit`) | Nothing from other lines; each kit identical or a gain at 0.35 m, on a sheet. |
| 15 Measurement path | a run of 游戏视觉 in a clone | `DEBUG_DISPLAY` in `FrontRooms/Surface` (valid overdraw), a dev stats overlay on Unity 6.6's split draw counters, an Xcode GPU-capture kit for a Mac player, fixed capture views in players. Needs 声音设计 for the silent player switch (P-5). It also gives the per-pass GPU milliseconds no stage of this run could measure. |

Not done in this run, beyond the rows above: the after-player capture of row 16 (combined test R16-2), the SetPass of the dust shafts at queue 3100 (R9-3), every figure of the title at Tour for rows 1 and 9 (R1-2, R9-2), and all idle (not UNDER LOAD) GPU timings.
