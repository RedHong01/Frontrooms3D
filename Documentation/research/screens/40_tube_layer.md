# The CRT tube layer, the VL245 fix and the V5 sound hooks (游戏视觉, crt-tube build, 2026-10-24)

**Status: READY.** Package `W/apply/crt_tube.sh` (W = `/Users/redwang/FrontRoomsVisualWork`):
- dry run OK on main 4049c70b + working tree at 12:44;
- a rehearsal `--apply` on a scratch copy gave MANIFEST OK;
- it is not applied by the build stage. The 游戏视觉 session lands it.

**Where it was built:** the clone `W/proj_crttube`, re-synced from main on 2026-10-24 at 12:27 with our three files kept. Work dir `W/crt_tube`.

**Captures:**
- bench: `Verification/crt_tube/bench_r4`, 12:41;
- game: `Verification/crt_tube/game_r1`, 12:39–12:40;
- batch mode, Metal, Apple M3 Max, desktop quality High;
- 1-minute load 12–25: every timing is UNDER LOAD.

**Red's words behind it:**
- **平面视觉 · 广告 chat, 2026-10-22:** "C09 选 1，其余全部按推荐".
  - Item 5 is the tube layer: `SCREENS_VISUAL_SYSTEM.md` §3, layer 3.
  - C09 = 1 also puts §4 (screens joining the warning language) out of v1. So **no WarnBurst sag is built.**
- **动画和动态图形模型技术 chat, 2026-10-22 21:23:** "选择这个", on VL429's 1990 OFFICE MONITOR tile.
  - The desk monitor shows the whole 4:3 frame at about 276 × 207 mm.
  - The border is dark glass (`NOTE_1022_crt_border_READ_FIRST.md`).
- **游戏视觉 chat:** 15:02, integrate as soon as it is verified; 15:3x, "继续，做完一个合一个".

**Owners:**
- the look spec: 平面视觉 · 广告;
- the shader, the material and the per-kit faces: 游戏视觉;
- the V5 audio side: 声音设计;
- the C26 setting: 系统设计.

## 1. What the package changes (three files, every .meta and GUID kept)

| File | main → package | What |
|---|---|---|
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | c2f1dbd4 (V4) → 6dabb8ca | Tube runtime, V5 hooks, VL245 warm-up, the 4:3 mipmapped RT; details below |
| `Assets/Resources/Rendering/FrontRoomsScreenVideo.shader` | 707084cb → c73d8095 | `FrontRooms/ScreenVideo` becomes the tube; same name, so the RT glass still traces it as emission |
| `Assets/Resources/Media/CRT_AdSurface.mat` | b8d02e1a → db8ca4c6 | Shader changes from URP Unlit to `FrontRooms/ScreenVideo`; the codex audit F6 path is unchanged |

**What `FrontRoomsScreenVideo.cs` gets:**
- the tube runtime, in one bounded block `[CRT-TUBE-BEGIN]`…`[CRT-TUBE-END]`;
- the V5 sound hooks;
- the VL245 warm-up, in `FrontRoomsScreenAdBank`;
- the RT goes from 640 × 434 to a 640 × 480 mipmapped RT;
- two top-level enums.

**Small hooks outside the block** (one line each, for a clean 3-way merge):
- the optional `soundRole` argument of `Attach`;
- the visibility-edge call in `Update`;
- the `TubeAfterCreate()` call in `CreateSurface`.

**Untouched:** map files, `FrontRooms3DGame*.cs`, scenes and the Office kit. Nothing assigns a sound role, so V5 is inert.

**V4 (`v4_desk_still`, LANDED 10-22 20:41):** the package is built on main's V4 `FrontRoomsScreenVideo.cs` c2f1dbd4, so it needs no rebase.

## 2. The tube (SCREENS_VISUAL_SYSTEM.md §3, layer 3)

**Two curved surfaces follow each kit's own measured face.** The faces were read from the FBX with `W/crt_tube/tools/face_profile.py`:

| Kit | Face centre (local) | Half size | Bulge |
|---|---|---|---|
| Kit_CRTMonitor | (0, .248, .195) | .149 × .113 m | 6 mm |
| Kit_CRTTV | (0, .284, .224) | .2255 × .171 m | 10 mm |

The TV is found by its mesh name. A screen authored with its own anchor or size keeps the flat quad and is still drawn by the tube shader.

**1. The picture surface:** 2 mm in front of the face, `FrontRooms/ScreenVideo`, one opaque unlit pass plus DepthOnly and DepthNormalsOnly, with fog.
- **Raster:**
  - **Kit_CRTTV:** the broadcast is overscanned 4.5 % top and bottom (about 4.7 % at the sides) through a barrel of .05. The edges run under the bezel, with 0 mm of black and never a pillarbox.
  - **Kit_CRTMonitor (Red's pick): underscan.** The whole 4:3 frame is lit at 276 × 207 mm through a smaller barrel (.02).
    - The 12 / 10 mm round it is the unlit phosphor body behind the faceplate: dark glass, never flat black.
    - A V4 still lands on the same 276 × 207 mm area at 1:1. It has no barrel and none of the image effects: `_StillRect` undoes V4's Fit scale.
- **Scan lines (480) and the aperture grille** (0.25 mm on the monitor, 0.72 mm on the TV) are band-limited by screen-space derivatives:
  - each harmonic fades in only above 2–4 px per line (lines) or 3–6 px per period (grille);
  - both have a mean of 1, so they never alias and never change the level;
  - at play distance only a close-up resolves them.
- **Halation (the phosphor bloom):** in the faceplate, as an energy-conserving ring blur of RT mip 2. A uniform field keeps its level; bright type gets a faint halo.
  - The output never leaves 0–1. The post Bloom threshold is 1.05, so a screen never blooms and never borrows the lamps' glow.
- **The rest of the picture:** a slight convergence error, a corner vignette, rounded raster corners.
- **The phosphor body:** sRGB 38/45/40, lit only by the ambient probe. It is the black level of a lit room.

**2. The faceplate glass:** 3.5 mm in front of the face, desktop only.
- It is the glass system's own `FrontRooms/Glass` with `_RTReceive = 1`, F0 .045, a light dust film and no float-glass roll: one shared material.
- So the G14 RT glass traces its reflection, and the raster fallback is the same shader's own probe reflection: they match by construction.
- It joins the monitor's LODGroup like the picture (codex audit F7).

**Power:**
- **Off (1.9 s):** the picture folds to a line (0.18 s), the line shrinks to a dot (0.32 s), the dot fades (1.4 s).
- **On (1.8 s):** a dot opens to a line and the line to the picture while the cold tube warms up. The brightness rises along one smoothstep over the whole 1.8 s.
- **A set that is off is dark glass:** the picture surface swaps to one shared material with `_BaseColor` black, so the RT trace sees no emission. The body shows under the faceplate.
- On WebGL, on phones and on flat screens, an off set shows the kit's own face as before.
- A change nobody can see just happens; that is V4's still mix leaving a set off.
- The clock is game time, frozen by the C26 world hold. It ticks once per frame in `beginContextRendering`, so the toggle's own frame already draws the first step.

**Power and the warning language stay apart:**
- Nothing in the three files reads `LampLevel`, the Warn overrides, `WarnBurst`, `FixtureChanged` or `LampDipped` (grep of the payload: 0 code references).
- The only change a screen makes is a power change by the player (E) or V4's out-of-view mix.
- Its brightness never rises faster than the 1.8 s warm-up.
- The collapsed line concentrates the beam's energy but is capped at the steady picture's white.

**Screen-effects level (C26 is open; no menu built):**
- The enum is `FrontRoomsScreenEffects { Full, Reduced, Off }`. The default is Full.
- `FrontRoomsScreenVideo.SetScreenEffects(level)` sets it and `ScreenEffects` reads it; `EffectiveScreenEffects` is what this platform draws.
- **Reduced:** no grille, no halation or convergence error, half-depth lines, a dimmer beam.
- **Off:** a plain picture (overscan or underscan only) and a plain 1.5 s fade for power.

**Platforms (separate tracks):**

| Platform | Level | Faceplate glass | Other |
|---|---|---|---|
| Desktop (macOS, Windows, editor) | Full: the reference | Yes | — |
| WebGL (`#if UNITY_WEBGL && !UNITY_EDITOR`) | capped at Reduced | None | Draw count unchanged from main (WebGL is draw-bound) |
| Phones (`FrontRoomsHandheld.Active`) | capped at Reduced | None | V4 stills stay 1x as before |

## 3. VL245: picture at first sight (codex audit F7 kept)

**The cause** (`research/codex_audit/40_applied.md` §4):
- F7 culls the ad quad with its monitor, so far monitors no longer keep the shared bank warm.
- Its RT was black until the first decoded frame.

**The fix:**
- The bank prepares when the first screen registers. A chunk builds its desks before the player can see them.
- It plays to its first decoded frame and holds it paused until a screen is in view. Never under a close-up or the pause card (C26).
- The first visible frame shows **the picture** (not a warm-up look).
- A screen seen before its bank has any frame (a chunk built in view) draws a faint raster glow, never a black hole: `_Signal = 0`.
- **F7 is unchanged:** the picture and the faceplate stay in the monitor's LODs.

**Measured (bench, frame by frame, face-box luma mean/std, 16 frames from the moment the camera arrives 1.3 m in front of a kit built 4 s earlier behind it):**
- **Main's code** (a byte copy of main's bank): 4 frames black (.152/.002), picture from frame 4 (.371/.153).
- **Package:** 0 frames black, picture from frame 0 (.395/.133).

## 4. V5 sound hooks (agreed with 声音设计; `NOTE_1022_v5_sound_READ_FIRST.md`)

**The API:**
- `public enum FrontRoomsScreenSoundRole { None, TitleTV, Beacon }`: top level, exactly these names and this order.
- `public FrontRoomsScreenSoundRole SoundRole` on FrontRoomsScreenVideo, set by `Attach(monitor, adFile = null, soundRole = None)`.
  - A role given at Attach sticks; a later Attach without one keeps it.
- `public bool IsPoweredOn`: already in main, unchanged.

**The visibility edge:**
- On the frame a screen starts to count as seen, if `SoundRole != None`, the screen reads `FrontRooms.Audio.FrontRoomsScreenSound.LoopTime`.
- It is read by reflection: a public static float or double, property or field.
- If the value is ≥ 0, the shared AdBank player seeks there. A seek before prepare is applied on prepare.
- The class does not exist yet, so this is skipped.

**No role is assigned anywhere, so the game is unchanged.**

**The title TV is NOT a small change, so it is not in this package:**
- No title-corridor TV exists as a FrontRoomsScreenVideo, and no `Kit_CRTTV` is spawned anywhere in main's scripts.
- Spawning one is a change to the title stream's room build (`FrontRooms3DGame*.cs`, 关卡设计's) or a new placement owner.
- **What it needs:**
  1. A `Kit_CRTTV` in the title corridor's start room, placed by the title-room owner.
  2. `FrontRoomsScreenVideo.Attach(tv, "FrontRooms_Ad_01.mp4", FrontRoomsScreenSoundRole.TitleTV)`: powered on, the same AdBank as the desks, so one decoder.
  3. Nothing else on our side. The tube shapes the TV by its mesh name, with period overscan.
- The 游戏视觉 session can do it as a separate V5 package once a spot is agreed.

## 5. Checks

| Check | Result | Verdict |
|---|---|---|
| Shaders compile (Unity, Metal) | `FrontRooms/ScreenVideo` 3 passes, `FrontRooms/Glass` 1 pass; 0 errors, 0 messages | PASS |
| No aliasing at distance | Ladder 0.6 / 1.5 / 4 / 10 m, monitor and TV (VL405). Shimmer, mean \|ΔY\| per 5 mm camera step on a still frame of the ad: tube 4.94 at 4 m and 2.64 at 10 m; main's flat point-sampled quad 11.92 and 9.44 (/255) | PASS |
| RT mips exist (halation and distance filtering read them) | 10 mips; mip0 / 2 / 4 means 89.6 / 91.3 / 94.6 of 255 | PASS |
| Grille and lines resolve only in a close-up | Macro at 0.6 m through FOV 4 / 6: visible; at FOV 76: none | PASS |
| Monitor underscan (Red's pick) | Test card: the whole frame and its yellow edge lit inside a dark-glass border | PASS |
| TV overscan | Test card: the yellow edge and part of the action-safe frame cropped; barrel and vignette visible | PASS |
| V4 still on the tube | `_Still` 1, raster (0.9452, 0.9409) = 276 × 207 mm, no barrel, `_StillRect` .9048 = 1 / V4's Fit 1.1053; no lines or grille. Field 43/49/42 against an off set's dark glass 41/47/39 (post on) | PASS |
| Power off / on | Off: 11 frames at the tube's own clock (0–2.2 s) show fold, line, dot, fade, dark glass. On: 10 frames show dot, line, picture, warm-up. Ends at 1.9 s and 1.8 s (`IsPoweredOn` False / True) | PASS |
| Glass reflection, RT on / off (real Office desk and a floor TV, game seed) | Set off, face luma: desk 0.085 RT High vs 0.078 RT Off; TV 0.079 vs 0.085. Set on: 0.286 vs 0.283 and 0.294 vs 0.298. RT High shows the room in the dark glass; the fallback has the same level | PASS |
| Levels Full / Reduced / Off | Drawn as specified (VL406) | PASS |
| Phone (FrontRoomsHandheld.Force iPhone 16 Pro) | Effective Reduced, no faceplate layer | PASS |
| VL245 first sight | Main 4 black frames; package 0 (VL407) | PASS |
| No lamp or warning reads | 0 code references in the payload | PASS |
| Roslyn `--all`, clone | macOS 0 errors; iOS 0 errors | COMPILE OK |
| Roslyn `--all`, main 4049c70b + tree (Game 00d06f84) + payload override | macOS 0 errors; iOS 0 errors (12:45) | COMPILE OK |
| Package dry run on main | DRY RUN OK; rehearsal `--apply` on a scratch copy: MANIFEST OK, then ALREADY APPLIED | PASS |
| LF only, ASCII | Payload has 0 CR (asserted by `build_package.py`) and 0 non-ASCII bytes | PASS |

## 6. Cost (High, UNDER LOAD)

**Method:**
- Wall time per 1920 × 1080 render: 30 renders closed by one GPU readback, 8 interleaved rounds per configuration.
- Unity's GPU recorders report nothing for manual renders in a batch-mode editor, so this is the GPU-bound proxy.

**Results, as ranges of the sorted round differences:**

| View | Tube Full minus no screen surface | Tube Full minus main's flat Unlit quad |
|---|---|---|
| 20 monitors in view at 3.6 m | +0.12 … +0.25 ms (≈ 0.006–0.012 ms per screen) | +0.10 … +0.28 ms (≈ 0.005–0.014 ms per screen) |
| One monitor at 0.6 m (a quarter of the frame) | −0.20 … +0.03 ms (below the noise) | −0.11 … +0.07 ms |

**Draws:**
- The desktop adds one transparent faceplate draw per powered or off screen in view.
- WebGL and phones add none.
- Per frame, the CPU ticks only the screens in the active list and sets a material float only when its value changes.

## 7. Open items and notes

**Kit LOD wedges (not this package; for the kit owner, 游戏视觉):**
- At 4 m and 10 m, both `Kit_CRTMonitor` and `Kit_CRTTV` show two grey wedge triangles left and right at screen height.
- Main's own code shows them too (VL405, bottom row, MAIN tiles).
- They look like a far-LOD mesh fault in the kits. Check them in game.

**For 平面视觉 · 广告:**
- The tube follows §3 layer 3.
- Halation stands in for "phosphor bloom", inside 0–1, so screens never use the lamps' glow.
- The TV keeps period overscan. The monitor underscans per Red's 10-22 pick.
- The V4 still field 9/10/11 now sits on the dark-glass black level: about 2/255 above an off set, post on.

**For 声音设计:**
- The three APIs your apply waits for are in the package: the enum, `SoundRole`, and `IsPoweredOn` (already in main).
- The seek on the visibility edge reads `FrontRooms.Audio.FrontRoomsScreenSound.LoopTime` by reflection.
- The title TV needs a placement first (§4).

**For 系统设计 (C26 open):**
- The hook is `FrontRoomsScreenVideo.SetScreenEffects(FrontRoomsScreenEffects)`, default Full; no menu was added.
- WebGL and phones are capped at Reduced in `EffectiveScreenEffects`.

**RT reflection of a lit screen:**
- The RT system traces `_BaseMap × _BaseColor` over the surface's UV.
- So a reflected monitor picture fills the whole face rather than the 276 × 207 mm underscan.
- This is invisible at reflection scale and left as is.

**The game run used the code before one change:** the tube clock moved from unscaled to game time (`Time.deltaTime`, for fixed-step captures). Its frames are steady states, so they are unaffected. The bench (bench_r4) ran on the final code.

**Images (Shot format, `research/screens/images/`):**
- `VL405_tube_shape_distance.jpg`: ladder, macro, test cards, main against tube at distance;
- `VL406_tube_power_glass_levels.jpg`: power off and on, RT on and off in game, levels, V4 still, phone;
- `VL407_vl245_first_sight.jpg`: main against the package, frame by frame.

VL rows: `W/vl_pending/crt_tube.md`, VL405–VL407, plus the new text for VL245's row.
