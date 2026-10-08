# 03 — Code survey for the close-up inspect (read-only)

Date: 2026-10-07, 18:47 PDT. Stage: code survey (visual chat 游戏视觉). Nothing in Red's project was changed; nothing was rendered, so this stage adds no row to the VERIFICATION LOG.

**Red's request (translated):** every visual asset the player can really look at (example: the Q16 evacuation-plan placard, `research/placard/images/q16_u1_A3_legend_0p6m.jpg`) should be something the player walks up to, interacts with, and sees in a close-up shot. While the close-up is up, the game is paused.

**What this file answers:** how the game works today at every point a close-up would touch, with file:line, and what each owner would have to change. The asset list is in `01_inventory.md` (parallel stage); this file only cross-references it.

**Line numbers** are from the main project at the time above. `Input/FrontRoomsTouchControlsView.cs` was being edited live by the touch session (mtime 18:46), so its numbers may move.

---

## 0. The short version

1. **The aim path is one ray, one map call.** `UpdateAim` casts 2.4 m from the rig's `BaseEye` (`FrontRooms3DGame.cs:1266-1268`), asks `map.Describe(collider)` for a prompt (`:1270`), and on E calls `map.Use` (`:1300`) or the glass hold (`:1303-1346`). `Describe` knows only doors and windows (`FrontRoomsMapWorld.cs:2194-2211`). There is no third kind of target.
2. **The close-up targets have no colliders, and every gameplay ray ignores triggers.** 73 of 113 kit sidecars set `noCollider` (all signs, plates, keys, the clock, the key boards; the placard by spec D10, `placard/10_spec.md:47`). All 8 gameplay queries use `QueryTriggerInteraction.Ignore` (list in §1.4). So the aim ray goes straight through every MUST target to the wall behind it. **Fix (recommended): a visual-owned registry of inspect boxes with a ray-box test, no physics and no layer change** (§10.3).
3. **Today's pause is a phase gate, not a time stop.** `Time.timeScale` is only ever set to 1 (`FrontRooms3DGame.cs:347`). Esc sets `Phase.Paused` (`:2131`), which stops `UpdateMapPlay` (`:2155`). But `FrontRoomsMapWorld.Update` keeps running (`FrontRoomsMapWorld.cs:832-850`): lamps flicker, doors keep swinging, and a door that starts its swing during the pause still sends its noise to the Relay (`:2418-2423` → `FrontRooms3DGame.cs:907-917` → `relay.Noise`). FMOD keeps playing everything: **nothing subscribes to `FrontRooms3DGame.Paused`** (`:45`), although `AUDIO_CONTRACT.md` lists it.
4. **`Time.timeScale = 0` during the close-up is safe and closes those leaks.** No Rigidbody, coroutine, Animator, particle system or shader `_Time` use exists in `Assets/Scripts` or the project shaders. Every system that must keep moving already runs on unscaled time: the HUD fade (`FrontRooms3DGame.cs:2174`), the touch layer (`FrontRoomsTouchMotion.cs:38-60`), the zone-reflection crossfade (`FrontRoomsZoneReflectionDriver.cs:13`), the sound director (`FrontRoomsSoundDirector.cs:146, 162`). The three things a close-up adds must also use unscaled time: the rig tick, the post volume weight, and the close-up's own input.
5. **The camera rig can frame a close-up, with three limits.**
   - It is ticked only inside `UpdateMapPlay` (`FrontRooms3DGame.cs:1092`), so it stops in any non-Playing phase. The game must tick it with `Time.unscaledDeltaTime` while inspecting.
   - FOV changes are clamped to ±15° (`FrontRoomsShotTimings.cs:13`, `FrontRoomsCameraRig.cs:163, 349`): 76° → 61° at most. So the close-up frames by distance: **0.31 m from the placard face frames the whole frame at 61°** (§11).
   - The pose and FOV are scaled by the Camera motion setting (`FrontRoomsCameraRig.cs:318-325`). At 50 % the camera stops halfway; at Off there is no close-up at all. Inspect needs a rig exception: full pose at any setting, with a cut instead of a dolly at Off.
6. **A new `Phase.Inspect` hits four fall-throughs in map-owned code:** R would restart the run (`FrontRooms3DGame.cs:2136`), the desktop pause wash would cover the screen (`:2054-2057`), the cursor would unlock (`:2075`), and on touch the phase maps to the **Caught** card (`FrontRooms3DGame.Mobile.cs:187-190`). Each needs one line.
7. **The post stack has nothing for shots yet.** `FrontRoomsPostStack.cs` (76 lines) makes one global volume and zone volumes. The profile has no Depth of Field component. `FrontRoomsGlassShot.Beat` / `Weight` (`FrontRoomsGlassShot.cs:33, 40`) have no runtime subscriber. The audit's `PostStack.PushShot` (`interaction_audit/10_audit_report.md` §6.4) was never built. The inspect volume is new visual work.
8. **A second, private E path exists.** `FrontRoomsScreenVideo` (CRT ads) raycasts from `Camera.main` with triggers on and reads `Keyboard.current.eKey` itself (`Media/FrontRoomsScreenVideo.cs:124-143`). It ignores the game phase, the prompt, the touch layer and `FrontRoomsInput`. In a close-up the rendered camera sits in front of the target, so E would toggle a CRT's power while the game is "paused". It must move onto the shared path.
9. **The kit sidecar can carry inspect data with no schema break.** `kitlib.py` copies `self.meta` straight into the JSON (`Tools/Blender/frontrooms_kit/kitlib.py:797-825`), and `JsonUtility` ignores unknown keys (sidecars already carry `signAtlas`, `numberAtlas`, `face`, `noCollider`, none of which `FrontRoomsKitLibrary.Info` declares). Add `inspect` to `Info` (`FrontRoomsKitLibrary.cs:40-104`) and attach the target in `Spawn` (`:196-208`). Then module props (`FrontRoomsMapWorld.cs:2038`), office kits and mounts all get it without a map change.

---

## 1. The interaction path today (aim ray, prompt, E)

### 1.1 Per frame, in order

| Step | Where | What happens |
|---|---|---|
| Input read | `FrontRooms3DGame.cs:2126` (`Update`), `:947` (`UpdateMapPlay`) | One `FrontRoomsInput.FrameSnapshot` per frame, cached on `Time.frameCount` (`Input/FrontRoomsInput.cs:121-142`) |
| Phase routing | `FrontRooms3DGame.cs:2127-2131` | Title + Start → run; Paused + O → settings; Esc toggles Playing ↔ Paused |
| Gameplay gate | `:2153-2167` | `UpdateMapPlay(dt)` only when `phase == Playing && mapPlay` |
| Look | `:953-962` | Mouse added to yaw/pitch unless `rig.LookLocked` or `glassShot.LookLocked` |
| Shot input | `:967-973` | `rig.ClampLook`; move zeroed if `rig.MoveLocked`; S → `rig.Consume(ShotInput.Back)` |
| Glass shot | `:974-982` | `glassShot.Tick(dt, threat, moveIntent, lookDegrees)` |
| Move | `:1006-1029` | CharacterController; sprint/stamina; climb (`:1165-1211`) |
| **Aim** | `:1067` → `UpdateAim` `:1259-1347` | see 1.2 |
| Relay | `:1074-1081` | `relay.Tick(dt, feet, BaseEye, forward)`; a Relay that sees or chases cancels shots (`rig.CancelForRelay`) |
| Relay rig | `:1089` → `:1356-1396` | turn, motion state, timer steps |
| Picture | `:1092` | `rig.Tick(dt)` places the render camera last |
| HUD | `:2168` → `UpdateHud` `:2170-2219` | prompt text `:2193`, hold bar `:2194-2198`, mobile prompt `:2199` |

### 1.2 `UpdateAim` (`FrontRooms3DGame.cs:1259-1347`)

- **Eye:** `rig.BaseEye` (`:1266`). Gameplay never reads the shot camera (rig rule, `FrontRoomsCameraRig.cs:60-75`).
- **Ray:** `Physics.Raycast(eye, forward, Reach = 2.4 m, ~0, QueryTriggerInteraction.Ignore)` (`:1268`; `Reach` at `:184`).
- **Prompt:** `prompt = map.Describe(hit.collider, out aimedHold)` (`:1270`).
  - Doors: "E  ·  OPEN DOOR", "E  ·  SHUT DOOR", "LOCKED  ·  NEEDS THIS ZONE'S KEY" (`FrontRoomsMapWorld.cs:2198-2204`); nothing while broken or being broken.
  - Windows: "HOLD E  ·  BREAK GLASS", `holdToUse = true` (`:2205-2209`). The game trims it to 1.2 m (`FrontRooms3DGame.cs:1272`, `GlassBreak.Reach`) and swaps in "TAP E  ·  BREAK GLASS" in tap mode (`:1274`).
  - Anything else: `null` (`FrontRoomsMapWorld.cs:2210`). **Props, signs, the clock and the placard return null.**
- **Aim change:** leaving a target releases the glass hold and the shot (`:1278-1286`).
- **E ownership:** `EDown()` (`:1214-1220`, autopilot can inject). During a shot, E belongs to the shot: `rig.Consume(ShotInput.Use)` (`:1289-1291`).
- **Doors:** a press calls `map.Use(aimed)` (`:1297-1301`) → `FrontRoomsMapWorld.Use` (`:2213-2229`): rattle if locked, else unlock, open or shut.
- **Glass:** fresh press → `BeginGlass` (`:1235-1256`) → `map.BeginGlassHold` (`FrontRoomsMapWorld.cs:2604`), the glass shot and a 0.55 m step-in; held → `map.Hold` (`:2633-2685`); let go → `ReleaseHold` (`:2688-2695`); tap mode → `PauseHold` (`:2702-2707`).
- **Keys** have no E at all: `CollectKeys` vacuums any key within 0.9 m every frame (`FrontRoomsMapWorld.cs:3127-3143`).

### 1.3 Other entry points that use the same objects

| Path | Where | Note |
|---|---|---|
| Touch "tap the door" | `FrontRooms3DGame.Mobile.cs:168-177` | Ray from the rendered camera through the tapped viewport point; same `Describe`/`Use`; ignores holds |
| Level Designer walker | `FrontRoomsMap/FrontRoomsMapWalker.cs:88-150` | Own Update, own ray (`:140`), own legacy `Input.GetKeyDown(KeyCode.E)`; uses the same rig |
| CRT screen ads | `Media/FrontRoomsScreenVideo.cs:99-143` | Own ray from `Camera.main` (`:133-134`, **triggers on**), `Keyboard.current.eKey` (`:141`), toggles power. No phase check, no prompt, no touch. Owner unclear (an earlier chat; 平面视觉 has the intake, `GRAPHIC_VISUAL_TASKS.md:54-61`) |

### 1.4 Every physics query in the game

| File:line | Query | Triggers |
|---|---|---|
| `FrontRooms3DGame.cs:1268` | aim ray | Ignore |
| `FrontRooms3DGame.Mobile.cs:172` | touch tap | Ignore |
| `FrontRoomsShots/FrontRoomsCameraRig.cs:376` | camera world clamp (0.1 m sphere) | Ignore |
| `FrontRoomsMap/FrontRoomsMapHunter.cs:1020, 1049` | Relay capsule probes | Ignore |
| `FrontRoomsMap/FrontRoomsMapHunter.cs:1279` | Relay sight ray | Ignore |
| `Audio/FrontRoomsRelaySound.cs:85` | Relay occlusion | Ignore |
| `FrontRoomsMap/FrontRoomsMapWalker.cs:140` | designer aim | Ignore |
| `Media/FrontRoomsScreenVideo.cs:133` | CRT focus | **Collide** |

**Consequence:** a trigger collider is invisible to everything except the CRT ray, and the zone post volumes are trigger boxes too (`FrontRoomsPostStack.cs:52-55`). A solid collider would block movement, the Relay's sight and the camera clamp. User layers 6–31 are all free (`ProjectSettings/TagManager.asset`), but adding one is a project-settings change.

---

## 2. Pause today

### 2.1 Entry points

| Trigger | Where | Effect |
|---|---|---|
| Esc (desktop) | `FrontRooms3DGame.cs:2131` | Playing ↔ Paused. Esc never cancels a shot (`FrontRoomsCameraRig.cs:13`, audit §3.0) |
| Lost focus | `:2107-2113` | Playing → Paused (not under autopilot) |
| App paused (mobile) | `:2115-2121` | Playing → Paused |
| Touch pause button | `Input/FrontRoomsTouchControls.cs:673-678` (hit test), `:836-838` (`framePauseDown = true`) | becomes `PauseDown` in the virtual snapshot (`:263-273`), handled by the same `:2131` |
| Android Back | `Input/FrontRoomsMobileBackBridge.cs:27` → `FrontRooms3DGame.Mobile.cs:127-134` | one step back: restart confirm → settings → pause ↔ play |
| O (settings) | `FrontRooms3DGame.cs:2128-2130`, `:1724-1744` | only from Paused |
| R (restart) | `:2136-2148` | **any phase except Playing and Title** (reloads the scene; `Awake` resets `timeScale` to 1 at `:347`) |

### 2.2 `SetPhase` (`FrontRooms3DGame.cs:2037-2106`)

- Raises `Paused(bool)` on every change into or out of Paused (`:2042`); `OnDestroy` raises `Paused(false)` (`:2318`).
- Closes settings outside Paused (`:2045-2049`).
- **Overlay:** `overlay.SetActive(!playing || keepMobileTitleFade)` (`:2054`); off-white wash at α 0.98 on desktop, clear on handheld (`:2055-2057`).
- Panels: room, threat, key `SetActive(playing)` (`:2070-2073`); crosshair `enabled = playing` (`:2054`).
- **Cursor:** locked only while playing (`:2075`).
- Pause copy (`:2076-2091`; `PauseText` `:1716-1722`); caught card (`:2094-2104`).
- Mouse settle frames after a resume (`:2093`).
- Touch menu state (`:2105` → `Mobile.cs:179-198`).

### 2.3 What today's pause freezes, and what it does not

`timeScale` stays 1. Only code inside the phase gate stops.

| Frozen (inside `UpdateMapPlay` or phase-gated) | Still running |
|---|---|
| Player move/look, aim and prompt (`:941-1067`) | **Map streaming and dressing** (`FrontRoomsMapWorld.cs:837-841`) |
| Relay brain `relay.Tick` (`:1078`) | **Lamps:** `TickFixtures(Time.deltaTime)` (`:842`, `:1652-1676`), lamp overrides age (`:3064-3084`), `FixtureChanged` fires (`:3109`) |
| Relay rig animation (`:1089`) | **Doors:** `TickDoors(Time.deltaTime)` (`:843`, `:2717-2739`): swings finish, a key's unlock delay elapses and opens the door (`:2719-2729`), `DoorMoved` fires (`:2414, 2422`) → `OnDoorMoved` → `relay.Noise` (`FrontRooms3DGame.cs:907-917`) → `HuntToward` (`FrontRoomsMapHunter.cs:366-392`). **A noise reaches the Relay during the pause** |
| Camera rig shots, shakes (`:1092`) | Key spin (`FrontRoomsMapWorld.cs:3135`) |
| Glass shot (`:974-982`) | **All FMOD sound** (no `Paused` subscriber, §9) |
| Start-room stream `roomStream.Tick` (`:854`) and title stream (`:447`) | Door Foley, player steps, Relay steps: `Time.deltaTime` components (§9) |
| Tier clock, stamina, captions, rattle jolts (`:2157-2165`) | Screen-ad video and the CRT E toggle (§1.3) |
| | Glass RT, zone reflection fades, touch UI (unscaled) |

---

## 3. What `Time.timeScale = 0` would do, system by system

Settings checked: `TimeManager.asset` (fixed step 0.02, timeScale 1), `DynamicsManager.asset:21-22` (`m_AutoSimulation: 1`, `m_AutoSyncTransforms: 0`), `ProjectSettings.asset:969` (`activeInputHandler: 2`, both), no Input System settings asset (so the default dynamic-update mode). Searched `Assets/Scripts`, `Assets/Shaders`, `Assets/Resources`: **no** Rigidbody, `WaitForSeconds`, `StartCoroutine`, `Invoke`, Animator, ParticleSystem, or shader `_Time`.

| System | Clock today | File:line | Under timeScale 0 | Must it run during the close-up? |
|---|---|---|---|---|
| Game `Update` dt | `Mathf.Min(Time.deltaTime, .1f)` | `FrontRooms3DGame.cs:2149` | 0 | The close-up must use `Time.unscaledDeltaTime` |
| Camera rig | owner's dt | `FrontRoomsCameraRig.cs:276-367`; ticked at `FrontRooms3DGame.cs:1092` only | not ticked outside Playing | **Yes** (the transition). Tick with unscaled dt |
| HUD fade | `Time.unscaledDeltaTime` | `FrontRooms3DGame.cs:2174` | runs | yes |
| Relay brain | game dt | `FrontRoomsMapHunter.cs:220-…` via `:1078` | frozen (phase gate) | no |
| Relay rig | game dt | `FrontRooms3DGame.cs:1386`; `FrontRoomsRelayRig.cs:121-128` (edit mode only) | frozen | no |
| Lamps (map) | `Time.deltaTime` | `FrontRoomsMapWorld.cs:842, 1652-1715` | **hold their current level** | no (Red: pause). Note: a lamp caught mid-stutter stays dark |
| Lamp overrides | same dt | `:3064-3084` | hold | no |
| Doors (map) | `Time.deltaTime` | `:843, 2717-2851` | **frozen**: no swing, no unlock timer, no `DoorMoved`, no noise leak | no |
| Streaming / dressing | frame budget | `:837-841, 867-896` | keeps building and dropping chunks | yes (harmless; nothing moves) |
| Revisit shift timer | `Time.time` | `:885, 903` | frozen | no |
| Key pickup | distance | `:3127-3143` | runs; player does not move | n/a |
| Title / start-room stream | game dt | `FrontRoomsRoomStream.cs:429-451` | frozen (phase gate) | no |
| Glass shot | game dt | `FrontRoomsGlassShot.cs:178-200` | frozen | no (refuse inspect during a glass shot) |
| Physics | FixedUpdate | `DynamicsManager.asset:21` | no steps (no Rigidbodies exist) | no |
| Physics queries | current colliders | — | still work. New chunk colliders register on creation; moved colliders do not sync (auto-sync off), but nothing moves | yes (the inspect precheck) |
| Input | Input System dynamic update; cache on `Time.frameCount` | `FrontRoomsInput.cs:121-142` | works | **yes** |
| Touch layer | `FrontRoomsTouchMotion.Now/Delta` = unscaled | `Input/FrontRoomsTouchMotion.cs:38-60`; view doc `FrontRoomsTouchControlsView.cs:20` | runs | yes |
| Zone reflection fades | unscaled (or capture dt) | `Rendering/FrontRoomsZoneReflectionDriver.cs:13` | runs | yes |
| Glass RT | `Time.frameCount`, `realtimeSinceStartup` | `Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:882-884`, accumulation `:1186-1189` (cap 32, `:44-45`) | runs; with lamps frozen the scene is still, so reflections **converge to 32 samples** | yes (a better close-up) |
| Sound director | `Time.unscaledTime/DeltaTime` | `Audio/FrontRoomsSoundDirector.cs:146, 161-163` | runs: tension slide, fixture voices, beds | sound chat decides (§9) |
| Player steps, Relay steps/occlusion, door Foley | `Time.deltaTime`, early return on 0 | `FrontRoomsPlayerFootsteps.cs:41-42`, `FrontRoomsRelaySound.cs:50-51`, `FrontRoomsDoorSound.cs:103-104` | stop updating; an FMOD event already started plays out | no |
| FMOD playback | FMOD's own clock | — | **keeps playing** | sound chat decides |
| Screen ads | `VideoPlayer`, `timeUpdateMode` not set | `Media/FrontRoomsScreenVideo.cs:262-275` | UNVERIFIED (depends on Unity's default time mode) | Red: probably freeze |
| Placard glow (spec) | `Time.deltaTime` | `placard/10_spec.md` §4.9 | holds its glow | yes, holding is right |
| Film grain | URP internal | profile `Resources/Rendering/FrontRoomsPost.asset` | UNVERIFIED whether the grain pattern still moves | look-dev check |
| Capture harnesses | `Time.captureDeltaTime` | `FrontRooms3DGame.Baseline.cs:331-348`, `FrontRoomsTouchPlaytestDriver.cs:86` | game dt = capture dt × 0 | harnesses must not inspect |

**Recommendation:** freeze with **both** a new `Phase.Inspect` (stops gameplay the same way as Esc) **and** `Time.timeScale = 0` from the E press until the camera is back on `BaseEye`. The phase gate alone leaves the lamp, door, noise and Foley leaks of §2.3; timeScale alone would still run `UpdateMapPlay` with dt 0 and read input.

Whether the Esc pause should also set `timeScale = 0` is a separate decision for Red and the map chat (it would fix the same leaks there).

---

## 4. Camera rig API (map chat) and the glass shot's hooks

### 4.1 `FrontRoomsCameraRig` (`Assets/Scripts/FrontRoomsShots/FrontRoomsCameraRig.cs`)

| Member | Line | Note for a close-up |
|---|---|---|
| `enum ShotKind { KeyPickup, Unlock, Open, Rattle, Glass, Climb, DoorBreak, Caught }` | 6 | Append `Inspect`. Listeners: sound (contract) and post |
| `ShotCancel { None, PlayerInput, Relay }` | 9-17 | `PlayerInput` = S or a second E after 0.2 s, never Esc |
| `ShotSpec` | 26-44 | `target` is a `Func<Pose>` asked every frame; `fovDelta` / `fov`; `lockMove`, `lockLook`, `lookCone`; `cancel`; `duration` |
| `ShotHandle` | 47-58 | `weight` and `age` are **internal**. The post needs a public `Weight` |
| `static event ShotStarted, ShotEnded (ShotKind, Vector3 focus)` | 79 | raised in `BeginShot` (`:167`) and when a shot's weight reaches 0 (`:295`) |
| `Attach(body, camera, eyeHeight)` | 104-114 | done once per run at `FrontRooms3DGame.cs:654` |
| `BaseFov` | 117 | 76 (scene camera, `FrontRooms3D.unity:313`) |
| `BaseEye` | 130 | the only pose gameplay reads |
| `InShot`, `MoveLocked`, `LookLocked`, `HudFade` | 133-145 | `HudFade` fades room, threat, crosshair, key HUD (`FrontRooms3DGame.cs:2257-2267`) but never the hint card or captions |
| `BeginShot(spec)` | 161-169 | clamps `fovDelta` to ±15° |
| `EndShot`, `CancelShot`, `CancelForRelay` | 172-197 | |
| `Consume(ShotInput)` | 226-241 | E and S are swallowed during a shot |
| `ClampLook` | 244-254 | look cone |
| `ResetLayers` | 257-273 | run start |
| `Tick(dt)` | 276-367 | weights (`:284-307`), pose pull (`:314-326`), offsets, FOV pushes, shake, world clamp (`:364`) |
| `ClampToWorld(from, to)` | 370-386 | 0.1 m spherecast from `BaseEye`, ignoring the body |

**Limits that matter for a close-up:**

1. **Tick ownership.** "Pausing the owner pauses the shots" (`:71-72`). The game ticks the rig only at `FrontRooms3DGame.cs:1092`. During `Phase.Inspect` the game must call `rig.Tick(Time.unscaledDeltaTime)` itself.
2. **Camera motion scales the pose.** `pull = w * motion` (`:321`), FOV × motion (`:325`), no pull at all when motion is 0 (`:318`). At 50 % the camera stops halfway and the framing is wrong; at Off nothing moves. The audit rule "at Off every takeover plays in place" (`10_audit_report.md` §3.0) fits a door shot, not a close-up whose job is the view. **Request:** a `ShotSpec` flag (e.g. `fullPose`) that ignores the motion scale for the pose and FOV, and at motion Off sets `blendIn = blendOut = 0` (a cut). Shake and offsets stay scaled.
3. **FOV clamp ±15°** (`FrontRoomsShotTimings.cs:13`, applied at rig `:163` and `:349`). The audit fixed it as a comfort rule. The close-up keeps it and frames by distance (§11).
4. **World clamp.** The camera never passes a collider between `BaseEye` and the target (`:364`). If a column or door leaf is in the way, the close-up stops short. The inspect must precheck `rig.ClampToWorld(eye, pose)` and refuse (no prompt) when it is blocked.
5. **Look.** `UpdateMapPlay` does not run while inspecting, so yaw and pitch do not change. A pan inside the close-up has to be an offset inside the target pose, not base look.

### 4.2 `FrontRoomsGlassShot` hooks (`FrontRoomsShots/FrontRoomsGlassShot.cs`)

- `static event Action<int, float> Beat` (`:33`): beats 1–3 with intensity (motion × strength). For the visual chat's CA pulse.
- `static float Weight` (`:40`): shot weight × motion × strength, set in `Tick` (`:181`), zeroed by `Reset` (`:164-170`). For the brace vignette.
- **Runtime subscribers: none.** Only the autopilot (`FrontRooms3DGame.cs:2836, 2865`) and editor tests (`Editor/FrontRoomsMap/FrontRoomsGlassShotTests.cs:134, 144`). The glass post (`GlassBreak.Vignette .08`, `CaPulsePeak .16`, `FrontRoomsShotTimings.cs:107`) is not built.
- **Pattern to copy for inspect:** a static `Weight` (0..1) the post reads each frame. The inspect weight must come from unscaled time.
- **Interaction rule:** an inspect must not start while `glassShot.Active || glassShot.Blending` (`:53-57`), or must `Reset()` it first.

### 4.3 Timings table (`FrontRoomsShots/FrontRoomsShotTimings.cs`)

- Shared rules `:13-17`: `MaxFovDeltaDeg 15`, `MinFovChangeSeconds .4`, `WorldClampRadius .1`, `HudFadeSeconds .15`.
- The Unlock shot (`:32-57`) is the closest template: travel `0.30 + 0.12 × distance`, clamped 0.35–0.55 s (`:42`), shot volume blend 0.40 s (`:43`, "mild far blur, vignette +0.1, exposure +0.15"), optional camera light 0.6 (`:56`).
- **Add** `public static class Inspect { … }` here (map-owned file, shared by picture, post and sound).

---

## 5. Post stack (visual chat)

| Item | Where | State |
|---|---|---|
| Global volume | `Rendering/FrontRoomsPostStack.cs:15-31` | priority 0, profile `Resources/Rendering/FrontRoomsPost` |
| Zone volumes | `:41-62` | trigger box, priority 1, blend 2.5 m (Office) |
| Camera setup | `:64-75` | HDR, MSAA, post on, no AA, dithering; **`FrontRoomsGlassRT.OptIn(camera)` (`:74`)**, the G14 RT opt-in (macOS only, `Rendering/GlassRT/FrontRoomsGlassRT.cs:95-100`) |
| Profile contents | `Resources/Rendering/FrontRoomsPost.asset` | LensDistortion −0.04, Bloom 0.55 (threshold 1.05), SMH, Tonemapping ACES, WhiteBalance (9, −7), ColorAdjustments (exposure +0.15, contrast −6, saturation −8), **Vignette 0.26** (smoothness 0.45), FilmGrain 0.22, ChromaticAberration 0.06, LiftGammaGain. **No Depth of Field** |
| Profile authoring | `Editor/Rendering/FrontRoomsRenderSetup.cs:185` (`EnsurePost`), `:157` (`EnsureOfficePost`), `:240` (`Get<T>`) | the place to author `FrontRoomsPost_Inspect` |
| Depth texture | `Settings/FrontRooms_URP.asset:22` | already on (`m_RequireDepthTexture: 1`), MSAA 4 (`:28`) |
| Shot volumes | — | **none**; no `PushShot`, no Beat/Weight consumer |

**For the close-up:**
- Use the **same camera** (the rig moves it). It keeps post, MSAA, the G14 RT opt-in, the touch frost backdrop (`FrontRooms3DGame.Mobile.cs:93`) and the FMOD listener. A second camera would need `ConfigureCamera` again and would split the listener.
- A shot volume `Post / shot Inspect`: global, priority 2, weight driven on unscaled time from the shot weight.
- The volume weight is evaluated per rendered frame by URP, so it animates at timeScale 0.
- Era rule (audit §3.0): a 1990 camcorder has deep depth of field; the audit asked for a mild far blur, not shallow bokeh. The close-up look stage decides the DOF mode; this survey only notes the profile has none.

---

## 6. Input system (touch session owns `Assets/Scripts/Input/*`)

- One code-built action map, no asset and no `PlayerInput` (`Input/FrontRoomsInput.cs:222-281`): Move (WASD, arrows), Look (mouse delta × 2.1, right stick), Sprint (Shift), **Use (E only, no gamepad button)**, Pause (Esc), Start (Space, Enter), Settings (O), Restart (R), **ShotBack (S)**.
- `FrameSnapshot` (`:15-53`) holds those ten values. Three sources: physical (`:283-299`), virtual from touch (`:156-172`), editor injection (`:174-199`).
- Legacy input still used: settings keys (`FrontRooms3DGame.cs:1692-1713`, `Input.GetKeyDown`), the designer walker, and the CRT's `Keyboard.current` (§1.3).
- **For v1 no new action is needed:** E opens and closes, S closes (the shot rule), Esc opens the pause card over the close-up. A zoom or pan would need a new action and snapshot field (touch-session change).

---

## 7. Mobile touch (touch session)

| Item | Where | Close-up impact |
|---|---|---|
| `MenuState { Title, Playing, Paused, Settings, Caught }` | `Input/FrontRoomsTouchControls.cs:53` | needs `Inspect` |
| `UseKind { Hidden, Open, Shut, Take, Locked, Hold, Tap }` | `:55` | needs a READ/LOOK kind (glyph) |
| Finger routing by state | `:668-720` (`Begin`) | Inspect: USE = close; pause button stays; no stick, no sprint |
| Button actions | `:832-860` (`Activate`) | |
| Virtual snapshot each frame | `:247-273` | unchanged |
| Game → touch state map | `FrontRooms3DGame.Mobile.cs:187-190` | **falls through to `Caught`** for any new phase |
| Use prompt | `Mobile.cs:210-235` | visible only if `aimed != null` (`:213`): a registry target has no collider, so the map chat must add an `inspectAimed` check; kind chosen by substring (`:215-220`) |
| Prompt text on touch | `Mobile.cs:35-41` (`DisplayPrompt`) | strips "E  ·  " automatically if the inspect prompt uses that prefix |
| Back | `Mobile.cs:127-134` | add: Inspect → close |
| Tap-to-use | `Mobile.cs:168-177` | could open an inspect by tapping the target |
| Frost backdrop | `FrontRoomsTouchControlsView.cs:105-…, 678-…` (`BuildFrost`), `:701` (`Washed`), `:709` (`CaptureFrost`), `:953` | Inspect must not be "washed" (no frost over the close-up). Pause from inside a close-up would frost the close-up frame, which is fine |
| Clock | `FrontRoomsTouchMotion.cs:38-60` | unscaled already |
| Haptics | `Input/FrontRoomsMobileInteractionEvents.cs:34-107` | optional `InspectOpened/Closed` |

---

## 8. HUD and typography (平面视觉 decides; map chat builds)

- The HUD is one Screen Space Overlay canvas built in code, 1920 × 1080 reference (874 × 402 on handheld) (`FrontRooms3DGame.cs:1817-1951`).
- **`UiFont` picks the face from the GameObject name** (`:1482-1487`): "Room meta", "Distance", "Aim" → IBM Plex Mono; "Threat", "Key", "Menu text" → Bayon; anything else → Source Serif 4. The prompt is named "Key prompt" (`:1871`), so it is **Bayon**. Any new text must be named on purpose or set its font explicitly.
- `Text(...)` helper `:1488-1496`; `Panel` `:1497-1502`; **`TypographyGroup`** `:1805-1812` (RectTransform only, no card: "information that belongs to the world").
- Prompt: under the crosshair, 20 px desktop / 17 px handheld (`:1871-1873`); hold bar `:1874-1876`; stamina `:1877-1881`; all fade with the crosshair group.
- Pause card copy: `PauseText` (`:1716-1722`) lists the controls. A READ line would be new copy.
- Type scale of record: `Documentation/UI_SYSTEM.md:7-11` (prompt = UI label, Bayon 20/20; screen title Bayon 88/80; HUD meta Plex Mono 13/16). `UI_SYSTEM.md:33-34` once mapped "hold E reads/breaks" in 3D: a READ verb was planned before.
- Prompt strings today live in map-owned code (`FrontRoomsMapWorld.cs:2202-2208`, `FrontRooms3DGame.cs:1274`). An inspect verb string goes through 平面视觉 (and narrative for wording).

---

## 9. Sound: what AUDIO_CONTRACT allows, and what the director does today

**Rules** (`Documentation/AUDIO_CONTRACT.md`):
1. No new AudioSources or AudioClips. Raise an event; the sound chat adds the FMOD event.
2. **Do not set `AudioListener.volume` or `AudioListener.pause`.** The director mutes legacy audio itself (`FrontRoomsSoundDirector.cs:152`).
3. Legacy Unity sound is dead (`AudioManager m_DisableAudio: 1`); gate any leftover on `!FrontRoomsFmod.Ready`.
4. Listened-to hooks already in the contract: `FrontRooms3DGame.Paused(bool)` and `FrontRoomsCameraRig.ShotStarted / ShotEnded(ShotKind, Vector3)`. Renaming or removing them needs the sound chat.

**What the code does:**
- The director subscribes to `MapRunStarted`, `MapRunEnded`, `PlayerClimbed`, `sceneLoaded` (`FrontRoomsSoundDirector.cs:96-102`) and the map/Relay events (`:426-447`). **It does not subscribe to `Paused`, `ShotStarted` or `ShotEnded`.** So the Esc pause quiets nothing today: room tone, fixture hum, breath, heartbeat, the Relay presence drone and any window-stress loop keep playing.
- It runs on unscaled time (`:146, 161-163`), so its mix keeps sliding at timeScale 0.
- Buses exist: `bus:/`, `bus:/AMB`, `bus:/SFX`, `bus:/Music`, `bus:/Subjective` (`Audio/FrontRoomsSoundIds.cs:55-59`). The only bus control today is the caught cut, `stopAllEvents` (`FrontRoomsSoundDirector.cs:586-588`). No snapshots exist.
- The audit proposed a `Snapshot/Closeup` (room tone −3 dB) for the key shot (`interaction_audit/10_audit_report.md` §3.2).

**What the sound chat would do (their code, their call):** on `ShotStarted(Inspect)` (or a new `Inspecting(true)`), pause `bus:/SFX` and the Relay/Subjective voices (`Bus.setPaused`), keep or duck the room tone, optionally a close-up Foley; resume on the end. Also wire the existing `Paused`.

---

## 10. Kit sidecars, and where per-kit inspect data can live

### 10.1 Today's schema

- Loader: `Office/FrontRoomsKitLibrary.cs` (visual). `Info` (`:40-104`): `pile`, `name`, `boundsMin/Max`, `supports`, `colliders`, `anchors`, `tags`, `slots`, `triangles`, `trianglesLod1`, `frontAxis`, `footprintCentre/Size`, `placement`, `service`, `minCeiling`, `lodDistances`. `JsonUtility` (`:170`).
- `Spawn` (`:191-208`): instantiate, `ApplyMaterials` (`:216-240`, shadows on above 0.3 m), `AddColliders` only when `colliders` is true and the sidecar has boxes (`:242-252`).
- Writer: `Tools/Blender/frontrooms_kit/kitlib.py`. `self.meta` (`:148`), `no_collider()` (`:494-496`), `anchor()` (`:498-499`), `tag()` (`:501-502`), `pile()` (`:507-531`). The sidecar is `dict(self.meta)` plus Unity conversion of supports, colliders, anchors (`:797-825`). **Any key put in `self.meta` reaches the JSON.**
- Sidecars already carry keys `Info` does not declare (counted over all 113): `noCollider` 73, `lodRatios` 59, `lodBudget` 39, `motion` 23, `numberAtlas` 12, `signAtlas` 1, `face` 2, `hungPose` 3, and more. `JsonUtility` skips them.
- **Anchors for a readable face already exist** on wall pieces: `face` and `face_dir` (= face + 0.10 m along its normal), e.g. `Kit_DoorSign.json`, `interact_exit_sign.py:91-93`.
- `JsonUtility` creates a missing `[Serializable]` class field as an empty object, not null. Test a field inside it, the way `IsPilePiece` tests `pile.cls` (`:103`).

### 10.2 Who spawns kits (all go through `Spawn`)

| Caller | Where | Owner |
|---|---|---|
| Module props (any kit a room module lists) | `FrontRoomsMapWorld.cs:2038`, `colliders = !p.noCollider` | map |
| Office stations (desk, CRT, phone, paper…) | `Office/FrontRoomsOfficeKit.cs:487-688` | visual |
| Furniture piles | `Office/FrontRoomsFurniturePile.cs:133` | visual |
| Window frames | `Office/FrontRoomsInteractableKit.Window.cs:112` | visual |
| Placard (spec) | `FrontRoomsPlacardMount`, `placard/10_spec.md` §4.6 (clone only) | visual |
| CRT screen ad | `FrontRoomsScreenVideo.Attach` at `Office/FrontRoomsOfficeKit.cs:675` | unclear |

### 10.3 Where inspect data should live (recommendation)

| Layer | Holds | Owner |
|---|---|---|
| **Kit sidecar `inspect` block** | per-asset framing: verb kind, face anchor names, readable size (m), margin, min/max distance, reach to open, the aim box (part-local), DOF on/off | visual (`kitlib.py` + asset scripts) |
| **`FrontRoomsInspectable` component** | per instance: the world box, the bound sidecar data, runtime extras (the placard's glow instance, a sheet id), enabled flag. Registers in a static list on enable, leaves on disable (chunks drop) | visual (added by `KitLibrary.Spawn` when the sidecar has `inspect`, or by a mount) |
| **Per-placement override** | turn inspect off for one placement | map (`ModuleProp`, `FrontRoomsRoomModuleData.cs:40-43`), only if needed |
| **Fallback catalogue in code** | kits whose FBX/JSON will not be re-exported soon (CRT monitor) | visual |

Sketch of the sidecar block (names to agree; all positions Unity part-local, written through `to_unity` like colliders):

```json
"inspect": {
  "kind": "read",
  "face": "face", "faceDir": "face_dir",
  "size": [0.4668, 0.3138],
  "margin": 0.08,
  "distance": [0.31, 0.36],
  "reach": 1.6,
  "box": { "centre": [0, 0, 0.007], "size": [0.4668, 0.3138, 0.014] },
  "dof": true
}
```

**Aim test without physics:** the game already has the blocking hit from its ray (`FrontRooms3DGame.cs:1268`). `FrontRoomsInspect.Find(ray, maxDistance = hit.distance or Reach)` tests the ray against the registered boxes (oriented, a few dozen per run). A wall-hung box sits in front of the wall, so it is found before the wall. No collider, no layer, no change to the Relay's or the rig's queries, and `noCollider` stays true on the placard (spec D10).

**Conflict:** targets on a door leaf (door sign, number plate) share the leaf's collider. A ray-box test would put the inspect in front of "E · OPEN DOOR" over that patch of the leaf. That needs a rule from the map chat and Red (a separate key, or open wins and the sign reads only through another verb). See `01_inventory.md` §0 item 5.

---

## 11. Close-up numbers for the placard (Q16)

Inputs: frame outside 466.8 × 313.8 mm, sheet 432 × 279 mm (`placard/10_spec.md` §2, `evac_placard.py` docstring); print 2592 × 1674 px at 6 px/mm; scene camera FOV 76° vertical, near 0.06 m (`FrontRooms3D.unity:311-313`; fallback `FrontRooms3DGame.cs:278`); rig floor 61° (76 − 15). 16:9.

| Quantity | Value |
|---|---|
| tan(half FOV) at 61° | 0.589 (vertical), 1.047 (horizontal at 16:9) |
| tan(half FOV) at 76° | 0.781 (vertical), 1.389 (horizontal) |
| Whole frame + 8 % margin, at 61° | **0.31 m** from the face (height-bound; width alone needs 0.26 m) |
| Same framing as Red's 0.6 m reference render | **0.36 m** at 61° (the frame fills about 62 % of the width) |
| Screen density at 0.31 m | 3.0 px/mm at 1080p, 3.9 at 1440p, 5.9 at 2160p |
| Texture density | 6 px/mm: above screen density up to 4K; above the audit's ≥ 1.6 px/mm close-up rule (`10_audit_report.md` §6.4) |
| Smallest readable line | at 1080p a line needs a cap height of about 3 mm (≈ 9 px). 平面视觉 should check the legend's smallest line ("WALLCOVERING SHOWN AT 1/25 SIZE") against that |
| Hero LOD | LOD0 holds at 0.3 m (spec §2.3, "about 2.3 px/mm at 76°") |
| World clamp | at 0.31 m the 0.1 m sphere ends 0.21 m from the face: clear of the 13.5 mm frame (no collider) and the wall |
| Travel | Unlock rule `0.30 + 0.12 × d`, clamped 0.35–0.55 s: from 1.5 m away ≈ 0.44 s |

**Light note:** with lamps frozen, a placard whose lamp was mid-stutter stays dark for the whole close-up. That is honest, and it is also the moment the glow legend reads (narrative: it lights only when its lamp is off). The optional camera light (`Unlock.ShotLightIntensity .6`) would wash out the glow, so it should stay off for the placard.

---

## 12. Proposed shape (one paragraph)

The game (map chat) gets a fourth target kind beside doors and glass. `UpdateAim` asks the visual chat's `FrontRoomsInspect.Find` after its own ray. On E it calls `BeginInspect`: it releases any hold, refuses when the Relay sees or chases (or another shot runs), sets `Phase.Inspect` and `Time.timeScale = 0`, and begins a rig shot `ShotKind.Inspect` whose target pose comes from `FrontRoomsInspect.ClosePose`. While inspecting, the game ticks the rig with unscaled dt and reads only E, S, Esc (and Back on touch). E or S ends the shot. When its weight reaches 0, the game returns to Playing and timeScale 1. The post volume, the target registry and the framing are visual; the sound pause is the sound chat's, on `ShotStarted/ShotEnded(Inspect)`; the touch state and glyph are the touch session's; the words are 平面视觉's.

---

## 13. Hook points and what each owner changes

### 13.1 Map chat (关卡设计): CONTRACT REQUESTS, sketched against today's lines

| # | File:line | Change |
|---|---|---|
| M1 | `FrontRooms3DGame.cs:31` | Append `Inspect` to `Phase` (append only: `FrontRoomsTouchPlaytestDriver.cs:481` reads the phase by name) |
| M2 | `FrontRooms3DGame.cs:39-53` | `public static event Action<bool> Inspecting;` (true at the press, false when control returns) |
| M3 | `FrontRooms3DGame.cs:1268-1277` | After the ray: `if (prompt == null && FrontRoomsInspect.Find(ray, hitAny ? hit.distance : Reach, out var t) && CanInspect()) { inspectAimed = t; prompt = t.Prompt; }`; clear `inspectAimed` at `:1261-1264` |
| M4 | `FrontRooms3DGame.cs:1292` (before `if (aimed == null)`) | `if (inspectAimed != null) { holdProgress = 0f; if (pressed) BeginInspect(inspectAimed); return; }` |
| M5 | new `BeginInspect` / `UpdateInspect` / `EndInspect` | release hold and glass (`:1157-1162`), `pullClear = null`, refuse if `relay.SeesPlayer \|\| State is Chase or BreakDoor` or `rig.InShot` or `climbTime >= 0`; `rig.BeginShot(new ShotSpec { kind = ShotKind.Inspect, target = () => FrontRoomsInspect.ClosePose(t, rig), fovDelta = FrontRoomsInspect.FovDelta(t), focus = t.Focus, blendIn = …, blendOut = …, lockMove = true, lockLook = true, cancel = ShotCancel.PlayerInput, fullPose = true })`; `SetPhase(Phase.Inspect)`; `Inspecting?.Invoke(true)`; `Event("inspect", t.Name)` |
| M6 | `FrontRooms3DGame.cs:2127-2131` | Inspect routing: E or S → `rig.EndShot`; Esc → remember `pausedFrom = Inspect`, `SetPhase(Paused)`; resume returns to `pausedFrom` |
| M7 | `FrontRooms3DGame.cs:2153-2167` | `if (phase == Phase.Inspect) { rig.Tick(Time.unscaledDeltaTime); if (inspectShot.Ended) EndInspect(); }` |
| M8 | `FrontRooms3DGame.cs:2037-2106` (`SetPhase`) | `Time.timeScale = p == Phase.Inspect ? 0f : 1f` (plus Paused if Red wants it); no overlay in Inspect (`:2054`); keep the cursor locked (`:2075`); no pause copy (`:2076-2091`); settle frames on return (`:2093`) |
| M9 | `FrontRooms3DGame.cs:2136` | R only in `Paused` or `Caught`, never in `Inspect` |
| M10 | `FrontRooms3DGame.cs:2107-2121` | focus loss in Inspect: stay (already frozen) or go to Paused with `pausedFrom` |
| M11 | `FrontRooms3DGame.cs:2170-2219` (`UpdateHud`) | Inspect: hide prompt, hold bar, hint card, captions (or let `rig.HudFade` fade them; 平面视觉 picks) |
| M12 | `FrontRooms3DGame.cs:2303-2321` (`OnDestroy`) | `Time.timeScale = 1f`; `Inspecting?.Invoke(false)` if inspecting |
| M13 | `FrontRooms3DGame.Mobile.cs:187-190` | `phase == Phase.Inspect ? MenuState.Inspect` before the `Caught` fall-through (after the touch session adds the value) |
| M14 | `FrontRooms3DGame.Mobile.cs:213-220` | visible also when `inspectAimed != null`; kind → `UseKind.Read` |
| M15 | `FrontRooms3DGame.Mobile.cs:127-134` | Back in Inspect → close |
| M16 | `FrontRoomsShots/FrontRoomsCameraRig.cs:6` | append `ShotKind.Inspect` |
| M17 | `FrontRoomsCameraRig.cs:26-44, 314-326` | `ShotSpec.fullPose`: pose and FOV not scaled by Camera motion; at motion 0 the blend is a cut |
| M18 | `FrontRoomsCameraRig.cs:47-58` | `public float Weight => weight;` on `ShotHandle` (for the post) |
| M19 | `FrontRoomsShots/FrontRoomsShotTimings.cs` | `static class Inspect`: reach, travel rule, blend out, the 0.2 s cancel delay, post blend |
| M20 | autopilot (`FrontRooms3DGame.cs:2427-…`) | never open an inspect (or close it at once) |
| M21 | optional: `FrontRoomsMapWalker.cs:88-150` | same inspect in the Level Designer walker |
| M22 | decision | door-leaf targets (sign, number plate) vs "E · OPEN DOOR" (§10.3) |

### 13.2 Visual chat (us)

| # | File | Change |
|---|---|---|
| V1 | new `Rendering/FrontRoomsInspect.cs` | registry; `Find(ray, max, out target)` (ray vs oriented boxes); `ClosePose(target, rig)` (face + normal × d, d solved from size, margin and FOV, §11); `FovDelta`; travel time; reach check; `ClampToWorld` precheck |
| V2 | new `Rendering/FrontRoomsInspectable.cs` | per-instance component; registers on enable; holds world box and data |
| V3 | `Office/FrontRoomsKitLibrary.cs:40-104, 196-208` | `InspectDef` class + `public InspectDef inspect;` in `Info`; in `Spawn`, attach `FrontRoomsInspectable` when `inspect.kind` is set |
| V4 | `Tools/Blender/frontrooms_kit/kitlib.py:494-505` | `def inspect(self, kind, face="face", face_dir="face_dir", size=…, …)` writing `self.meta["inspect"]`; convert positions at `:797-825` |
| V5 | `Tools/Blender/frontrooms_kit/assets/evac_placard.py` (+ door sign, plate, exit sign, clock, key board…) | call `kit.inspect(...)`; add `face`/`face_dir` anchors where missing |
| V6 | `Rendering/FrontRoomsPostStack.cs` + `Editor/Rendering/FrontRoomsRenderSetup.cs:157-240` | `EnsureShotVolume("Inspect")` (priority 2) and the `FrontRoomsPost_Inspect` profile (DOF mode per look-dev, vignette and exposure deltas); weight from `ShotHandle.Weight`, unscaled. Share the driver with the unwired glass Beat/Weight hooks |
| V7 | placard mount (Q16 B3, clone) | ensure the placard registers as an inspect target; no shot light; LOD0 |
| V8 | `Editor/Rendering/FrontRoomsKitLookdev.cs` | a close-up capture at the game's numbers (61°, 0.31 / 0.36 m) for every inspect kit |
| V9 | WebGL tier | same mechanic; reduced look only (no DOF or Gaussian, no lens), gated to WebGL |

### 13.3 Sound chat (声音设计)

- Subscribe to `ShotStarted/ShotEnded(ShotKind.Inspect)` or `FrontRooms3DGame.Inspecting(bool)`; pause SFX and the Relay and Subjective voices, keep or duck room tone; optional close-up Foley. Wire the existing `Paused(bool)` too.
- Update `AUDIO_CONTRACT.md`: add `ShotKind.Inspect`, `Inspecting`, and "ShotKind is append-only" (the contract says that only for `HunterState`).
- Note: their director keeps running on unscaled time (`FrontRoomsSoundDirector.cs:146, 162`); their `Time.deltaTime` components stop.

### 13.4 Touch session (`Assets/Scripts/Input/*`)

- `MenuState.Inspect` (`FrontRoomsTouchControls.cs:53`) with routing in `Begin` (`:668-720`): USE closes; pause stays; no stick, no sprint.
- `UseKind.Read` (`:55`) and its glyph (`FrontRoomsTouchSprites`).
- View: Inspect is not `Washed` (`FrontRoomsTouchControlsView.cs:701`); USE shows a back glyph (`TickUse :546`).
- Optional: `InspectOpened/Closed` haptics (`FrontRoomsMobileInteractionEvents.cs`); a pan or zoom action in `FrontRoomsInput` if Red wants it.

### 13.5 平面视觉

- The verb string(s): e.g. "E  ·  READ" for print, another for objects; keep the "E  ·  " prefix so touch strips it (`Mobile.cs:35-41`). The prompt renders in Bayon (`:1485`, "Key prompt").
- Whether the close-up shows any hint ("E  ·  BACK") or nothing (world-first, `UI_SYSTEM.md:3`), its face, size and place.
- The pause card line (`FrontRooms3DGame.cs:1716-1722`) and the touch legend.
- The legend's smallest line against §11's 3 mm cap height at 1080p.

### 13.6 Unclear owner

- `Media/FrontRoomsScreenVideo.cs:124-143`: move the CRT power toggle onto the shared aim path (or make it an inspect target with power as a second verb). Until then it must at least check the game phase.

---

## 14. Risks found in the code

1. **R restarts mid-close-up** with a new phase (`FrontRooms3DGame.cs:2136`).
2. **Touch shows the Caught card** for a new phase (`Mobile.cs:190`).
3. **Desktop pause wash covers the close-up** (`:2054-2057`); **cursor unlocks** (`:2075`).
4. **No close-up motion unless the rig is ticked with unscaled dt** (`:1092`, `:2149`).
5. **Camera motion 50 % or Off breaks the framing** (`FrontRoomsCameraRig.cs:318-325`).
6. **FOV clamp ±15°** forces 0.26–0.36 m distances (§11).
7. **Render-only targets + trigger-blind queries** (§1.4, §10.3).
8. **CRT E path** fires during pause and inspect (`ScreenVideo.cs:141`).
9. **Noise leak during today's Esc pause** (door swing → `relay.Noise`, §2.3); timeScale 0 fixes it for the close-up.
10. **No FMOD pause anywhere** (§9).
11. **Window stress loop** keeps playing if a pause starts mid-hold (`ReleaseHold` is not called on pause); `BeginInspect` must release first.
12. **`UiFont` by name** (`:1484-1486`): a new text named carelessly gets the wrong face.
13. **Door-leaf targets** collide with "E · OPEN DOOR" (§10.3).
14. **Autopilot E** could open a close-up (`:1214-1228`).
15. **Pause-scumming:** a frozen world lets a player stop a Hunt to think. Refusing inspect while the Relay sees or chases covers the worst case; Red decides the rest.
16. UNVERIFIED: VideoPlayer and URP film grain behaviour at timeScale 0.
17. The touch view file is moving (edited 18:46); re-read its lines before any patch.

## 15. Questions for Red

1. Freeze everything (lamps, doors, sound) during the close-up? (Recommended: yes, timeScale 0.) Should the Esc pause freeze the same way?
2. Block the close-up while the Relay sees or chases you? (Recommended: yes.)
3. Reach to open: 1.6 m? The camera then travels to 0.31 m in about 0.4 s.
4. Pan or zoom inside the close-up, or one fixed framing per object?
5. Door sign and number plate: how does the player read them without opening the door?

## 16. Files read (mtimes)

`FrontRooms3DGame.cs` (Oct 7 16:21), `FrontRooms3DGame.Mobile.cs` (Oct 7 17:28), `FrontRooms3DGame.Baseline.cs`, `FrontRoomsMap/FrontRoomsMapWorld.cs` (Oct 4 13:56), `FrontRoomsMap/FrontRoomsMapHunter.cs`, `FrontRoomsMap/FrontRoomsMapWalker.cs`, `FrontRoomsMap/FrontRoomsModulePropTag.cs`, `FrontRoomsMap/FrontRoomsRelayDirectorPolicy.cs` (header), `FrontRoomsShots/*` (Oct 3), `FrontRoomsRoomStream.cs`, `FrontRoomsRelayRig.cs`, `FrontRoomsSettings.cs`, `FrontRoomsCaptions.cs`, `Input/FrontRoomsInput.cs` (Oct 4), `Input/FrontRoomsTouchControls.cs` (Oct 7 17:28), `Input/FrontRoomsTouchControlsView.cs` (Oct 7 18:46), `Input/FrontRoomsTouchMotion.cs`, `Input/FrontRoomsHandheld.cs`, `Audio/FrontRoomsSoundDirector.cs` (Oct 3), `Audio/FrontRoomsSoundIds.cs`, `Audio/FrontRoomsPlayerFootsteps.cs`, `Audio/FrontRoomsRelaySound.cs`, `Audio/FrontRoomsDoorSound.cs`, `Rendering/FrontRoomsPostStack.cs`, `Rendering/FrontRoomsLook.cs`, `Rendering/FrontRoomsGlassPane.cs`, `Rendering/FrontRoomsZoneReflectionDriver.cs`, `Rendering/GlassRT/FrontRoomsGlassRT.cs`, `Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` (time and accumulation), `Office/FrontRoomsKitLibrary.cs`, `Office/FrontRoomsInteractableKit.Window.cs` (header), `Media/FrontRoomsScreenVideo.cs`; `Resources/Rendering/FrontRoomsPost.asset`; `Settings/FrontRooms_URP.asset`; `ProjectSettings/{TimeManager, DynamicsManager, TagManager, ProjectSettings}.asset`; `Scenes/FrontRooms3D.unity` (camera); all 113 `Resources/Props/Models/*.json`; `Tools/Blender/frontrooms_kit/kitlib.py`, `assets/evac_placard.py`, `assets/interact_exit_sign.py`; docs `AUDIO_CONTRACT.md`, `UI_SYSTEM.md`, `TOUCH_CONTROLS.md`, `RELAY_PURSUIT_REDESIGN.md` (pause, placard rows), `research/placard/10_spec.md`, `20_build.md`, `research/interaction_audit/10_audit_report.md` §3.0, §3.2, §6.4, `research/inspect/01_inventory.md` (§0).
