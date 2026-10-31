# iPhone Duo · unfolding mid-run (cover → inner display)

Research, catalogue and diagnosis for Red, 2026-10-24, by the 交互设计 line (a side session started from `local_0fdc206f`). **Docs only: no game code, project setting or build was changed.** Red picks an approach in §4.

- Every claim has a source id in brackets; the ids are rows in `SOURCES.md`. `L` rows are our own logs and code. The research agents' full notes (about 300 sources) stay outside the repo in `~/FrontRoomsInteractionWork/duo/fold_transition/research_*.md`; log excerpts and crash reports are in `…/evidence/`.
- Nothing was downloaded. Media is linked in §5, with the candidates that need Red's OK.
- Related: `../../../../IPHONE_DUO_ADAPTATION.md` (the Duo layout work landed 05:06), `../../../../TOUCH_CONTROLS.md` §3.1.

**Status, 2026-10-30.** The research below stands. These parts have moved on since it was written:

- **The feel.** Red's 10-29 audit answer ("全部按推荐") replaced the PAUSED card, which no longer exists: unfolding during play opens the settings page, which has been the pause since 10-24. It is built (R6, `FrontRoomsTouchControls.ApplyUnfoldPause`, `TOUCH_CONTROLS.md` §3.1). The option 2 plan in §4 stays as history.
- **Step 0.** 2 unfolds and 2 folds were measured with full screen and did not crash (§2.5). No unfold during unpaused play has been captured yet. Red, 10-30: "可以每一种检查只做一遍", so the device check that is left is one unfold during play, not 3 or 5.
- **Launch logo on the inner display held upright.** Red skipped the cold-launch capture on 10-24 at 07:50. That layout was checked in UIKit only (`TOUCH_CONTROLS.md` §3.2).

## 0. In short

1. **What Red saw at 05:23:54 was a crash of iOS's system shell (SpringBoard), 0.12 s into the unfold.** The inner screen stayed black for about 9 s, then the home screen came back, with the run paused in the background [L2][L4]. SpringBoard died in its code for "limited resizing" apps. Our app was one because the iOS export said landscape only **and** `UIRequiresFullScreen = NO`. App Store Connect rejects the same setting at upload (06:09 today) [L10]. **Fixed since 06:34:** main sets `UIRequiresFullScreen = YES` (Red's call; `FrontRoomsMobileBuild.cs` 8df529fe), as do TestFlight builds 15 and 16 [L10].
2. **Without the crash, iOS covers the inner screen in black until the game has answered the move,** then fades over a snapshot of the outer screen. Black lasted 0.27 s on the title screen, 1.67 s mid-run and 6.0 s during start-up (simulator) [L3]. Everything past ~0.3 s is our frame time. **The run keeps going in the dark**: nothing pauses the Relay [L9].
3. **Every platform hides the swap itself and expects the app to resize in place.** Android keeps the panel dark until the app's first new frame (≤ ~1 s) and then reveals that live frame [C1][C4][C11]. iOS runs the black-cloak-then-snapshot sequence; reviewers see a frosted, see-through glass effect [L3][E45][G30]. Apps that can't resize get system bars or a restart [A10][G19]. **No shipped game shows its own pause or loading card on unfold, and nobody publishes timings** (§3, §3.3).
4. **Decided by Red (2026-10-24, in this chat): option 2.** Red's words: "Use option 2 instead: unfolding shows the PAUSED card. Step 0 measurement as planned." I had recommended option 1 (hold the world); see §4 for both. The full-screen setting is in main, and step 0 measures the unfold with it.
5. **What full screen changes:** iOS still resizes the app in place, with no relaunch. It makes a discrete move to a screen of exactly the new size [E11][E17]. So the expected cost of an unfold is the ~0.3 s black floor plus our redraw, not the ~13 s cold start, which applies only when the game is relaunched [L11]. Measured so far: 0.39–1.34 s of black (§2.5).

## 1. What iOS 27.1 does when the Duo unfolds

### 1.1 Apple's documented model

- **An unfold is a resize of the same scene, not a relaunch.** The process stays alive; the app keeps its scene and state, and sees size-class and trait changes. Apps with `UIRequiresFullScreen` adapt in place too, rather than being sent to the background [E11] (checked by us 2026-10-24).
- The app's view **moves to the inner display** [E8]. `UIScreen.main` keeps describing the outer display, while the scene's own `screen` describes the inner one [E12][E57]. Apple: use the most local geometry (view, then window or scene) [E12].
- No lifecycle callback is documented for a fold; apps should re-read their size in layout callbacks or `windowScene(_:didUpdateEffectiveGeometry:)` [E2][E14][E11].
- **`UIRequiresFullScreen` on iOS 27 means discrete resizing:** the scene moves to a `UIScreen` of exactly the new size. The key is deprecated, Apple recommends removing it "as soon as possible", and a future release will ignore it [E17][E14][E11] (TN3192 checked by us; revised 2026-10-09).
- Orientation on the inner display: Apple's own answers disagree. One says supported orientations are not honoured there; others say they are, with exceptions in some hinge states. The consistent advice is to lay out by size [E5][E11].
- Hinge APIs (`UIHingeInteraction`, SwiftUI `onHingeChange`) report closed / partly open / open and an angle. Their update rate is system policy and must not be relied on [E7][E11].
- iOS 27 adds `UIWindowScene.displayLink(…)`, which retargets itself when the scene changes display; `UIScreen.displayLink` is deprecated [E27][E28].
- **Apple says nothing about what is shown during the swap or how long the app has to draw** [E2][E3][E4][E5][E11][E30]. For games, the HIG asks for play in every pose, a filled screen and changing aspect rather than bars [E3].

### 1.2 What people see (press; Apple publishes nothing)

- Hands-on reports describe a blurred, stretched, "see-through" transition with depth-of-field and gradient effects [E45][E46]. One reviewer saw a Liquid Glass opening animation with the left half frosted until the phone is flat [G30].
- 9to5Mac calls it a gorgeous software transition [E47]. MKBHD reportedly saw glitches; this comes through an aggregator [E52].
- Third-party rebuilds model a hinge-driven frosted glass that sharpens when flat [E53][E55].
- On demo units, Zenless Zone Zero was folded **closed** mid-session: the glass animation played, then the game resumed [G13].

### 1.3 What our simulator logs show (the mechanism)

The QA simulator's SpringBoard log shows two transition styles [L3][L6][L7]:

- **`.continuousSnapshot` is used for apps, including FrontRooms, every time:**
  1. **Start, hinge ~5°.** The arriving panel is covered by a black **cloak** (alpha 1, above everything). The departing panel's content gets a perspective "fold" mesh.
  2. **Capture, +50–540 ms.** The departing screen is snapshotted.
  3. **Swap, +55–1,020 ms.** The app's scene is handed to the inner display: same process, same `UIWindowScene`, new screen and bounds. Unity sees no lifecycle event.
  4. **Wait.** The cloak stays until SpringBoard's "move content" step completes, i.e. until the app has processed the move.
  5. **Reveal.** The cloak fades out over ~0.45 s with the snapshot still installed, presented with the perspective effect.
  6. **End, 1.1–3.4 s.** Overlays are dropped and the live app shows.
- **`.continuousLive`** is used for SpringBoard's own UI (home screen). There is no snapshot, and the cloak lifts at the first hinge update, 0.06–0.24 s. No third-party app got this style in our logs, and Apple's Duo documentation offers no opt-in [E2][E4][E11].

Simulator caveats: screen capture is blank in some hinge poses [E34][E35]; the simulated GPU is far slower than a device's; and today's SpringBoard had already crash-looped once (§2.1).

## 2. Why FrontRooms goes black (diagnosis, read-only)

Sources are our own, all read-only [L1–L9]:

- the game's console logs `FrontRoomsBuildWork/iphone-duo-20261024/Logs/simulator-runtime-1024-f.log` (Red's run) and `…-diag2.log`;
- the QA simulator's unified log (`xcrun simctl spawn 3B6AEED9… log show`), for SpringBoard and the game;
- SpringBoard crash reports;
- the Unity trampoline exported with the build;
- our plugin `Assets/Plugins/iOS/FrontRoomsHandheldNative.mm` (c3af2873) and `Assets/Scripts/Input/FrontRoomsHandheld.cs` (d480d6a9). Both are identical in main and in the Duo copy.

Nothing was tapped, rotated or launched on the simulator.

### 2.1 What Red saw (05:23:54): the system shell crashed

`simulator-runtime-1024-f.log` never shows the inner display. Its last `[HandheldNative]` line (05:24:03.079) is still the outer display, followed by `sceneWillResignActive` → `sceneDidEnterBackground` [L1]. The simulator's own log explains why [L2][L3]:

| Time (PDT) | Who | What |
|---|---|---|
| 05:23:41–42 | Red | turns the closed phone; the game follows (interface 4 → 3) |
| 05:23:4x | game | a run starts (`START · in place in stream room 109`) |
| 05:23:54.432 | SpringBoard | hinge at 5°: unfold transition starts (`.continuousSnapshot`); the inner panel is cloaked in black |
| 05:23:54.519 | SpringBoard | cover snapshot captured; the game's scene is handed to the inner display (+0.10 s) |
| 05:23:54.553 | SpringBoard | **crash** (EXC_BREAKPOINT, SIGTRAP), +0.12 s |
| 05:23:54.568 | launchd_sim | respawns SpringBoard |
| 05:23:59.994 | SpringBoard | the new instance's first log line (+5.6 s) |
| 05:24:03.0–03.1 | SpringBoard / game | displays brought up without animation. The game's scene is reconnected **in the background**; Unity pauses it, and our `OnApplicationFocus(false)` would show PAUSED on return |
| 05:24:06–08 | Red | folds and unfolds again (home screen, `.continuousLive`, no black) |
| 05:24:27.6 | launchd_sim | the simulator's whole userspace restarts (SIGTERM to every service); the game process exits |
| 05:24:59.0 → 05:25:04.7 | game | cold relaunch on the inner display: process start → first layout 3.7 s → active 5.7 s |

So the "long black" was ~8.7 s with nothing on the inner display (05:23:54.4 → 05:24:03.1), then the home screen, not the game.

**Crash signature** (`SpringBoard-2026-10-24-052359.ips`, top of the crashing thread) [L4]:

```
FrontBoardServices  -[FBSDisplayMode _initWithWidth:height:preferredScale:scaleOverride:refreshRate:gamut:hdr:rotation:virtual:validityCheck:]
FrontBoardServices  -[FBSDisplayMode _copyWithOverrideSize:scale:refreshRate:]
SpringBoard         +[SBApplication(Classic) restrictedDisplayConfigurationForResizableAppWithDisplayConfiguration:size:]
SpringBoard         +[SBApplication(Classic) restrictedDisplayConfigurationForLimitedResizingApp:size:hingeEdge:isOrientationLocked:]
SpringBoard         -[SBDeviceApplicationSceneHandle _modifyApplicationSceneAndSceneSettings:fromRequestContext:entity:]
…                   -[SBApplicationSceneUpdateTransaction initWithApplicationSceneEntity:transitionRequest:delegate:]
```

- **Reading.** While moving our scene to the inner display, SpringBoard computes a *restricted* display configuration for a "limited resizing" app with an orientation lock, and asserts on the display mode it builds.
- **Why we are on that path.** The installed `Info.plist` has `UIRequiresFullScreen = NO` and `UISupportedInterfaceOrientations` = landscape left and right only [L9]. Main's uncommitted `Assets/Editor/FrontRoomsMobileBuild.cs:287` sets `requiresFullScreen = false` [L10].
- **Not caused by our running code.** No game code runs inside SpringBoard. Our plugin only reads view metrics and adds one hidden, non-interactive corner probe view [L9].
- **It does not happen every time.** 1 of the 8 unfolds today with the game on screen crashed; a later unfold in the same landscape pose (05:26:43, mid-run) did not [L3].
- **The simulator was already unhealthy.** The SpringBoard that crashed was the 8th spawn after a crash loop at 04:08:51–04:09:26. In that loop SpringBoard aborted 7 times on a display configuration with 0 × 0 bounds ("We expect a valid bounds for configuration"). That loop is not app-specific [L5].
- **Hardware unknown.** SpringBoard is the same code on a device, but the simulator's inner display mode is 2007 × 2853 px while Apple's panel spec is 1878 × 2670 px [L9]. A display-mode validity check can behave differently there.
- **This setting cannot ship anyway.** App Store Connect rejected the iOS 0.1.0 (16) upload with it on 2026-10-24 06:09: "Invalid bundle … you need to include all four orientations to support iPad multitasking" (ID X3TLVPUK5A4JDBYO5S2S4EXJDA) [L10]. Build 15 shipped `UIRequiresFullScreen = YES`, landscape only, and passed. Build 16 shipped YES too. **Since 06:34 main sets YES** (Red's call; `FrontRoomsMobileBuild.cs` 8df529fe). The Duo work copy (`FrontRoomsBuildWork/iphone-duo-20261024/Project`) still says NO until it is synced [L10].

So the crash is Apple's bug, and the trigger (our "limited resizing" declaration) is ours.

### 2.2 The normal path: black until the game answers

Cover → inner with the game on screen, QA simulator, iOS 27.1. "Scene moved" is SpringBoard's host change; all times are relative to the transition start [L3]:

| Start (PDT) | Game state | Scene moved | **Black (cloak) until** | Transition end | Note |
|---|---|---|---|---|---|
| 02:18:34.9 | earlier build | +0.75 s | **+0.93 s** | +1.55 s | |
| 02:27:24.9 | earlier build | +0.14 s | **+0.35 s** | +1.13 s | reveal cut short (hinge already open) |
| 03:13:58.6 | earlier build | +0.08 s | **+0.34 s** | +1.12 s | reveal cut short |
| 03:15:55.7 | earlier build | +0.12 s | **+0.39 s** | +1.21 s | reveal cut short |
| 03:40:46.6 | **still starting up** | +0.18 s | **+6.00 s** | +6.65 s | cloak held until the game's first layout (52.698) |
| 03:43:20.1 | title screen | +0.06 s | **+0.27 s** | +1.43 s | game's first frame at the new size +0.11 s |
| 05:23:54.4 | playing | +0.10 s | **never** | — | SpringBoard crashed (2.1) |
| 05:26:43.7 | playing, landscape | +1.02 s | **+1.67 s** | +3.43 s | came up **upright** 669 × 951, turned landscape at +3.27 s |

Folds (inner → cover) showed 0.46–0.84 s of black and lasted 1.09–2.01 s in all (7 cases) [L3].

So, without the crash, the black has two parts:

- **The OS floor: ~0.3 s of black,** then a ~0.45 s fade over the snapshot, ~1.1–1.5 s in all. Only Apple can shorten this.
- **Ours: everything above the floor.** This is how long the game takes to process the scene move and present a frame at the new size.
  - Title screen: +0.06 s.
  - Mid-run (05:26:43): the black lasted 1.4 s longer than the floor [L7].
    - The capture itself came late (+0.54 s) and the scene was only moved at +1.02 s (44.756).
    - The game saw the move 0.22 s later (44.971) and drew the new size 0.38 s after the move (45.133).
    - The cloak lifted at +1.67 s.
    - How much of the late capture is the game's (a busy main thread) and how much is the OS's is not known.
  - **During start-up: 6 s** [L3].
- The simulator's GPU is far slower than a device's, so mid-run numbers on hardware should be smaller (not measured).
- **Why start-up is so slow** (measured by the corners + launch session on the spare simulator) [L11]:
  - Unity loads the scene, runs `Awake` (3.7 s) and draws its first frame (~4.7 s of Metal shader compilation on the simulator) inside `didFinishLaunching`.
  - The app becomes active ~13 s after the icon tap, behind a 1 × 1 black launch image.
  - That session is fixing the cold launch. It matters here twice: an unfold during start-up waits for all of it, and any recovery that relaunches the game costs the same black.

### 2.3 What Unity does when the view moves to the other display

From the exported trampoline (Unity 6000.3.10f1) and the project settings [L8][L9]:

1. **No restart, no new scene.** UIKit resizes Unity's view from 678 × 466 to 951 × 669 pt; the scale stays @3. Unity runs its loop from a plain `CADisplayLink` (`UnityAppController+Rendering.mm:30`), so nothing happens until the next tick.
2. **Next tick (`UnityView recreateRenderingSurfaceIfNeeded`).** The tick does the following, at a cost of about one frame:
   - `CAMetalLayer.drawableSize` becomes 2853 × 2007;
   - `UnityReportResizeView` updates `Screen.width/height`, and the safe area is re-reported;
   - `DisplayManager recreateSurface:` rebuilds the system surface and Unity's backbuffer targets;
   - the frame then renders normally.
3. **URP targets.** `FrontRooms_URP.asset` has HDR on (32-bit colour buffer), MSAA 4× and render scale 1.
   - The camera colour, depth and post-processing RTHandles grow from 2034 × 1398 to 2853 × 2007 px, 2.0× the pixels. They are re-allocated once, on the first frame at the new size; RTHandles grow and never shrink [E73].
   - Formats and sample counts do not change, so no new Metal pipeline states or shader variants are expected; shader warm-up is not a factor.
   - HDR display output is off (`useHDRDisplay 0`), so the swapchain format does not change.
4. **While resigned active, Unity draws nothing.** On `applicationWillResignActive`, Unity turns focus off and calls `UnityPause(1)`. `UNITY_SNAPSHOT_VIEW_ON_APPLICATION_PAUSE` is 0, so there is no extra frame and no snapshot view. This only matters when the OS backgrounds the scene (the crash case); a normal fold does not resign the app.
5. **Main-screen bookkeeping.** The fps cap is read from `UIScreen.mainScreen.maximumFramesPerSecond`, and `UIScreen.mainScreen` stays the outer display on the Duo [E12]. Both simulated panels run at 60 Hz and FrontRooms targets 60, so this did not matter here. The game kept rendering normally on the inner display in every log.
6. **Our code on the move.**
   - `FrontRoomsHandheld` reads the view metrics once a frame (cheap). Touches are cancelled on any geometry change (`FrontRoomsTouchControls.RefreshLayout`).
   - The give-way layout animates (0.28 s) or snaps. `AspectFov` keeps 16:9's horizontal field.
   - The two landscape aspects are almost equal (outer 1.455, inner 1.422), so the picture barely re-frames.
   - **Nothing pauses the run.** A normal fold does not resign the app, so `OnApplicationFocus` and `OnApplicationPause` never fire and the Relay keeps moving through the black.
7. **Upright detour.** On the inner display iOS switched our scene to all orientations ("orientation mode: dynamic → alwaysAll"). The landscape-only game first appeared upright (669 × 951) and turned landscape about 2 s later (05:26:45 → 05:26:47) [L7].

### 2.4 Ours versus the OS

| Part of the gap | Length (simulator) | Who |
|---|---|---|
| Unfold floor: black cloak, then a fade over the snapshot | ~0.3 s black, ~1.1–1.5 s in all | OS (iOS 27.1 SpringBoard, `.continuousSnapshot`) |
| Waiting for the game to answer the move and draw the new size | +0.06 s (title). Mid-run: 0.38 s from the move to the new frame, 0.64 s to the cloak lift, 1.4 s over the floor in all (slow simulator GPU; part may be the OS's late capture). +6 s during start-up | **Mostly ours**: main-thread load and frame time; the first frame at 2853 × 2007 with HDR + MSAA 4× |
| Upright first, then a turn to landscape | ~2 s, one extra resize | OS policy (the inner display ignores the landscape-only plist) plus **our** landscape-only declaration |
| SpringBoard crash: ~9 s black, then the home screen, run paused in the background | until the player relaunches (~13 s cold start) | OS bug, reached through **our** "limited resizing" declaration |
| Relay keeps hunting during the black | the whole gap | **Ours**: no pause or freeze on a display move |

### 2.5 Step 0: the unfold with full screen, measured 2026-10-24

Test app "FR fold S1": the Duo build of that morning with only `UIRequiresFullScreen = YES` changed in its Info.plist, on the QA simulator, with the inner display recorded. Log and video agree to within 0.03 s [L12].

| Event | Start (PDT) | Game state | Black until (log / video) | Then | Fills the screen at |
|---|---|---|---|---|---|
| Unfold A | 06:52:03.6 | title; the cover display had just rotated | **+1.34 s / +1.36 s** | small sideways picture, ~0.7 s | ~+2.2 s |
| Unfold B | 06:53:12.1 | already paused | **+0.39 s / +0.37 s** | small sideways picture, ~1.9 s | ~+2.47 s |

- **No crash** in these 4 transitions (2 folds, 2 unfolds). The folds were black until +0.43 s and +0.47 s.
- The game's first frame at the new size came 0.07–0.12 s after SpringBoard moved the scene. SpringBoard lifted the cloak 0.15–0.28 s after the move.
- UIKit sent no deactivation during the folds, so a normal fold does not pause the run by itself. The game has to do it (R6 now does).
- **Full screen removes the upright render, not the detour.** The game first appears letterboxed and sideways, then rotates to fill the screen.
- **Missing:** an unfold during unpaused play. The later recording attempt ended when the QA simulator went down at 07:03:28 (a lock, then a keyboard disconnect), and its render server crashed on the screen-capture queue at 07:03:33 while the recording was attached [L13]. The SpringBoard log gives the same times, so the check that is left needs no recording.

## 3. How others handle the unfold (catalogue)

No platform, OEM or game publishes a measured unfold gap. The only numbers are code limits in Android's source, one press figure ("a few seconds" of blank for apps that reload on Samsung's TriFold [G60]) and our own logs (§2.2). Two figures seen in search (0.5–2 s, ~300 ms) had no traceable source and were rejected [G99][G102].

### 3.1 The approaches at a glance

| # | Approach | What the player sees in the gap | How long | Who does the work | Examples | For a first-person horror game |
|---|---|---|---|---|---|---|
| T1 | **OS dark mask, then the app's new frame is revealed** | Inner panel dark until the app has drawn at the new size; then a black scrim opens from the fold line while the app's real new frame grows from 94 % to 100 %; touch blocked until 80 % | Dark ≤ ~1 s (code limit), reveal follows the hinge (≤ 1 s canned) | OS (Android WM Shell + SystemUI); the app's first frame is on the critical path | Android 12L+ / Pixel [C1][C4][C5][C7][C10][C11] | + the swap is hidden and the first image is live. − the run keeps going under the mask unless the game stops itself |
| T2 | **OS black cloak, then a snapshot of the old screen, then live** | Black until the app has processed the move; a ~0.45 s fade over the cover snapshot with a perspective effect (on hardware: a frosted "glass" opening); then the live app | Black 0.27–1.67 s (6 s at start-up); 1.1–3.4 s in all (our simulator) | OS (iOS 27.1 SpringBoard); the cloak waits for the app | iPhone Duo, every app transition we logged [L3][E45][G30] | + system look, the same for every Duo app. − for a moment the player sees the past (the cover snapshot). − the black lasts as long as our frame takes |
| T3 | **Live content follows the hinge** | No black, no snapshot | 0.06–0.24 s until the cloak lifts | OS + live rendering | iOS SpringBoard's own UI (`.continuousLive`) [L3] | Ideal, but no app got it in our logs and no opt-in was found |
| T4 | **In-place resize (continuity)** | The same session at the new size once the OS mask lifts | The app's resize time (one frame for Unity, §2.3) | App or engine; the OS masks | Supercell on Z Fold5 [G46]; Honor of Kings on Mate X [G53]; PUBG Mobile after a 2020 fix [G48]; ZZZ folded closed on Duo demo units [G13]; UnGodly on Duo [G12]; YouTube, Netflix and Google Maps launch demos [G67][G44][G69] | + the run and the tension carry on; Apple's and Google's recommended path [E11][A3][A6]. − needs a camera FOV policy and UI re-layout. − any slow first frame stretches the mask |
| T5 | **Recreate / relaunch, then restore** | Mask, then blank or a splash until the app has rebuilt; state only if it was saved | Seconds ("a few seconds" of blank on Samsung TriFold [G60]) | OS recreates (Android default); app restores | SmartNews [G66]; Unity games with extra activities [G41]; a One UI 8 regression [S13] | − breaks immersion. − risks the run. − iOS 27 does not do this to resizing apps [E11] |
| T6 | **Compatibility presentation: old size or aspect, with bars** | The game continues at once inside black bars; nothing is re-laid out | No switch gap, but the big screen is wasted | OS (Android size-compat mode; old-SDK apps on Duo) | Genshin, ZZZ (retail units), Delta Force on Duo [G19]; Sword of Convallaria [G29]; Galaxy Fold apps [G64]; Android size-compat [A9][A10] | + no black, no FOV jump. − Apple asks games to fill every pose and avoid bars [E3]. − old-SDK apps stay iPhone-sized, centred [E2][E5] |
| T7 | **OS restart affordance** | The old-size app with a restart button, or a "Restart for a better view?" dialog warning that progress may be lost | Until the player taps | OS | Android size-compat [A10][C16]; Samsung restart icon [S1][S5] | − asks the player to restart a run; never acceptable mid-hunt |
| T8 | **Engine holds the old render size; Core Animation scales it** | The old frame stretched or aspect-fit until the new size is ready | Until the engine switches | App or engine | Apple's Metal guidance for resizing windows [E21][E22] | On the Duo the cloak hides this phase anyway. Useful only as a cheap first frame that makes the cloak lift sooner |
| T9 | **Fold-direction gate (inner → cover only)** | Lock-style "swipe up to continue" screen on the cover | "A few seconds" (Pixel [A7]); ~3 s (OnePlus, snippet [P4]) | OS setting: Always / Swipe up / Never | Pixel [A7][A8], Samsung One UI 7+ [S12][S18], OnePlus [P3]; Motorola per app [M1] | Only for folding; no platform gates unfolding [A7][S11][M4] |
| T10 | **The game's own pause card, loading card or "continue?" prompt** | — | — | Game | **No shipped game found** [G13][G19][G46][G48][G49] (see §3.3) | The open space FrontRooms can use (§4) |
| T11 | **Camera FOV policy across aspects** | After the switch: no "zoom" or crop | n/a | Game | Samsung's horizontal-FOV guide for Unity [S9]; PUBG wider and Honor of Kings narrower on Mate Xs [G54]; CODM centre crop [G55] | FrontRooms already keeps 16:9's horizontal field (`AspectFov`) |
| T12 | **Early warning from the hinge** | The game dims or freezes itself before the swap | Undocumented: the hinge event order is not published | Game, through a native plugin | `UIHingeInteraction` / `onHingeChange` [E7][E11]; Android `FoldingFeature` [A15] | Gives a head start. Not reliable as the only signal: Apple calls the update rate system policy |

### 3.2 Platforms

**Apple, iPhone Duo (iOS 27 / 27.1).**
- An unfold resizes the same scene. The process stays alive, the state is kept, and the view moves to the inner display [E11][E8].
- Apps built with the iOS 27.1 SDK fill both displays edge to edge. Older-SDK apps run in a compatibility region: iOS 26 SDK apps are iPhone-sized and centred, iOS 27.0 SDK apps leave a status-bar strip [E2][E5].
- `UIRequiresFullScreen` no longer opts an app out. It now means discrete resizing to a `UIScreen` of exactly the new size. It is deprecated and will be ignored in a future release [E17][E14][E16].
- Apple documents neither the visuals nor a time budget (§1). No API was found that lets an app opt out of the animation or customise it [E2][E4][E11].

**Android framework / Pixel Fold.**
- An unfold is a configuration change. By default Android destroys and recreates the activity, unless the app declares the size changes in `configChanges` and handles `onConfigurationChanged` [A3][A11]. Google's large-screen quality bar requires state to survive fold and unfold [A6].
- **The AOSP unfold sequence:**
  1. The cover panel reports off (wait ≤ 500 ms) [C12].
  2. A Shell display-switch transition is requested [C9].
  3. The app redraws.
  4. The inner screen stays off until windows are drawn (≤ 1000 ms) and, since Android 15, until the transition has started (≤ 1000 ms) [C10][C11][C21].
  5. The reveal (T1) plays:
     - a black scrim opens from the centre line [C4][C5];
     - the app's frame scales from 0.94 to 1.0, with a crop and rounded corners, over a solid background [C1][C3] (checked by us);
     - launcher icons slide out from the centre [C19];
     - touches are blocked until 80 % [C4].
- **Timing.** Progress follows the hinge angle. If the hinge stops or is already open, a canned animation finishes within 1 s [C7][C20]. The safety net is 5 s [C2]. Google measures the gap internally (display-switch latency, Perfetto) but publishes no figure [C14][C15][C23].
- **Apps that cannot resize** keep their old configuration, scaled and letterboxed, with a restart control [A9][A10][C16]. A restart is a cold start with a splash screen [A12].
- **Games are special.** They are exempt from Android 16/17 forced resizing via `appCategory="game"`. "Display compatibility mode" also avoids restarts when a game moves between displays, while still delivering `onConfigurationChanged` [A1][A10][A18].
- **Settings** cover only folding (T9) [A7][A8]. Jetpack WindowManager reports the hinge as a `FoldingFeature` (state, `isSeparating`, occlusion, bounds; no angle) [A15].

**Samsung One UI (Galaxy Z Fold / Flip / TriFold).**
- Unfolding continues the cover app on the main screen automatically [S11].
- Samsung defines App Continuity as the app's job (`configChanges` or saved state). It asks that the app appear on the main screen where the user left off, filling it [S1][S2][S4].
- Non-resizable apps keep their size, with a small restart icon [S1][S5].
- Folding is governed by "Continue apps on cover screen": per-app toggles until One UI 6.1.1, then Always / Swipe to continue / Never [S11][S12][S18].
- After Android 16 / One UI 8 some Folds "refresh" apps on fold and unfold, a regression Samsung has confirmed [S13].
- **For games:**
  - the activity is recreated on each switch unless the change is handled; Unity and Unreal coped in Samsung's tests;
  - UE4 needs `r.EnableNativeResizeEvent=1` [S5];
  - keep a horizontal FOV, or the scene appears to "zoom" [S9].
- No Samsung statement or number on the unfold visuals was found.

**Huawei / HarmonyOS (Mate X series).**
- The foldable UX standard makes open/close continuity mandatory: page, scroll, input and playback must survive [H1] (snippet).
- It asks for continuous motion rather than a hard cut, which it says the system provides [H1].
- Apps learn the fold state from `foldStatusChange` and the active screen from `foldDisplayModeChange` [H2]. The 2019 Mate X guide asked apps to resize without restarting [H3].

**Honor, OPPO / OnePlus, vivo.**
- The OEM guides repeat Android's advice and describe black bars or compatibility mode for apps that don't follow it [P1][V1].
- Honor offers per-app display ratio and layout fixes for the inner screen [O1].
- OnePlus Open popularised the fold-direction swipe-up gate [P3][A22].
- No unfold animation description or number was found.

**Motorola Razr.** Opening the phone continues the cover app on the main display automatically. The per-app settings (Manual / Auto / Don't transition) apply only to closing [M1][M2][M4] (partly snippets).

**Microsoft Surface Duo.** Spanning is a user drag. The app is told it is bigger and re-lays out; apps had to declare the size changes to be told reliably [D1][D2][D5]. No transition visuals or timing are documented.

### 3.3 Games

- **No shipped game was found with its own transition** (pause card, loading card, or a "continue on the big screen?" prompt) on any platform. Documented behaviour is T4 (live resize), T6 (OS bars) or T5/T7 (reload or restart) [G13][G19][G46][G48][G49][G41].
- **iPhone Duo today, two days after launch.**
  - Most big games run letterboxed: Genshin Impact, Zenless Zone Zero (retail units), Delta Force [G19]; Sword of Convallaria [G29]. Unoptimised games fill about three quarters of the screen [G31].
  - Asphalt Legends shipped "support" and then "optimized for iPhone Duo" builds in launch week [G25][G26].
  - UnGodly's studio claims continuous play closed or open [G12].
  - On demo units, ZZZ was folded closed mid-session: the system animation played, then the game resumed [G13] (footage [G14], not watched).
  - No review describes a game during the **opening** itself.
- **Android foldables.**
  - Google marketed Supercell's mid-attack unfold on the Z Fold5 [G46].
  - A 2019 video report shows Honor of Kings continuing through an unfold on the Mate X [G53].
  - PUBG Mobile fixed "resolution display errors" when switching Samsung screens in 0.18.0 [G48].
  - Warzone Mobile fixed a broken display after fold → unfold in 2024 [G49].
  - Unity games were cut top and bottom, or restarted, when non-Unity activities lacked `configChanges` [G41].
  - Users report games stretched until a restart [G86][G89] (user reports).
- **Failures are wrong images, not long black screens:** stretched, cropped or wrong resolution [G48][G49][G41][S9].

### 3.4 Video, maps and reading

- **Video is the classic continuity demo.**
  - Playback carried over without a break on the Galaxy Fold (YouTube) [G67], in Google's first foldable statement [G44] and in the Pixel Fold keynote [G79].
  - On iPhone Duo, Apple shows Clips scrolled on the outer display, then opening to keep watching inside [G2]. AVKit resizes video across the displays itself [E11].
  - Not every app is ready: Disney+ shows wide side bars on the Duo [G31], and a live stream on the Mate Xs cut out briefly [G54].
- **Maps.** Google Maps was the Galaxy Fold Unpacked 2019 continuity demo [G69][G52]. On iPhone Duo it is not optimised yet [G33][G34].
- **Reading.** A comic app on the Mate Xs jumped back to the chapter start [G54], and SmartNews restarted on every switch [G66]. Google warns that apps can lose UI state going from the outer to the inner display [G45].

### 3.5 Engines

**Unity on iOS.**
- Unity's view resizes on its own, and `Screen.width/height` update live [E62].
- Unity plans no fold event and points to native plugins [E62].
- DPI is reported wrongly (489) on both panels, and Unity's findings are simulator-only [E62].
- The trampoline rebuilds the drawable and backbuffers on the next display-link tick [E63][L8], and URP's RTHandles reallocate when the size grows [E73].
- Unity 6000.3.10f1 uses a plain `CADisplayLink` [L8]. iOS 27's scene display link would retarget itself across displays [E27], and a Metal display link toggle exists from 6000.3.15f1 [E70].
- No 6000.3 patch through 27f1 mentions iOS 27 or the Duo [E70].

**Unity on Android.**
- Keep Resizeable Activity on (the default) and declare the size changes in `configChanges`. Otherwise compatibility mode letterboxes the game and may prompt a restart [E77][A4].
- Known foldable issues:
  - a landscape-only app on Pixel Fold doesn't fill the screen after unfolding until it reloads. Won't Fix; the workaround is to allow all orientations, then restrict autorotation at runtime [E82];
  - black screen in multi-window, fixed in 6000.0.7f1 [E83].

**Unreal.**
- On Android the native resize event is off by default (`Android.EnableNativeResizeEvent=1`); without it the image stretches between displays [E90][S5].
- UE4's 2.1 maximum aspect leaves bars on cover screens [S5].
- Nothing from Epic on iOS 27 or the Duo [E92][E93].

**Godot.** Nothing documented [E94].

## 4. Decision for Red: how should an unfold feel?

### Background (why this is asked now)

- **The crash.** At 05:23 Red unfolded the Duo mid-run and got ~9 s of black, then the home screen. iOS's system shell crashed during the unfold (§2.1). The crash is Apple's bug, but our app reached it through the "limited resizing" path: landscape only with `UIRequiresFullScreen = NO`. It happened in 1 of 8 unfolds today, and App Store Connect rejects that setting anyway. Main has used `UIRequiresFullScreen = YES` since 06:34.
- **The black.** Even without the crash, iOS keeps the inner screen black until the game answers the move (§2.2): ~0.3 s at best, 1.7 s mid-run on the simulator, 6 s during start-up. During that black the run keeps going, so the Relay can move while the player sees nothing.
- **The upright detour.** On the inner display iOS may first show the game upright and only later turn it landscape (§2.3, point 7).
- **What we can choose.** The OS part is the same in every option below: ~0.3 s of black, then iOS's own fade over a snapshot. We cannot draw over it. What we choose is what the **game** does around it.

### Step 0, in every option: an unfold never loses the run

This has to happen first, or none of the options can be judged. **The game has to take one of two shippable settings:**

| | **S1 · Full screen (`UIRequiresFullScreen = YES`), landscape only** | **S2 · Fully resizable: all four orientations, key removed** |
|---|---|---|
| On the inner display | Stays landscape. When the phone is held upright, iOS letterboxes it (Unity's iOS 27.1 tests [E62]), so no upright detour. Every size change is a discrete move to an exactly-sized screen [E17] | The game must draw upright (669 × 951) and in side-by-side windows itself |
| Unfold | Same app, same scene, resized [E11]. **Not measured yet** | Same. Not measured yet |
| Ships today | Yes: build 15, and build 16 is uploading with it [L10] | Needs a portrait design (HUD, touch, menus) first |
| Future | Apple deprecated the key and will ignore it in a future iOS; the game would then behave like S2 [E17][E11] | Apple's recommended end state [E17] |
| Cost | One flag in `FrontRoomsMobileBuild.cs`, which belongs to the build line | Days of portrait work across HUD, touch and menus |

**Decided: S1.** Red's call; it has been in main since 06:34 (`FrontRoomsMobileBuild.cs` 8df529fe) and in TestFlight 16 [L10]. S2's portrait work stays planned for later, before Apple drops the key.

With S1, an unfold is still an in-place resize: a discrete move to an exactly-sized screen, not a relaunch [E11][E17]. The remaining black should be the OS floor plus our redraw (§2.2), not a cold start. Then:

| What | How | Cost |
|---|---|---|
| Re-measure the unfold with S1 | A simulator build with `UIRequiresFullScreen = YES`. The Duo work copy still says NO. Change only its `Assets/Editor/FrontRoomsMobileBuild.cs`, never a wholesale sync from main: the corners + launch session has unlanded work in that copy. Do it after that session's batch runs (until ~07:10), and tell it first. Red unfolds 5× mid-run on a freshly booted QA simulator; Device Hub's fold cannot be scripted. Record the inner display while Red does it, and read the SpringBoard log as in §2.2. Any build needs Red's OK | ~30 min build + 10 min of Red folding |
| If SpringBoard still crashes | Feedback Assistant with `SpringBoard-2026-10-24-052359.ips` and the new report (Red's Apple ID) | 10 min, Red |
| If the shell dies anyway | The run already pauses when the app is sent to the background. Add: keep the run on that pause, so reopening the app shows PAUSED, not the title | Small (our files + one 关卡设计 hook) |
| Answer the move fast | Detect "the display changed" natively (screen bounds or identity, already logged by our plugin) and do nothing heavy that frame. Keep the first frame at 2853 × 2007 cheap | Small |

### The options (what the player feels)

| | **1 · Hold the world, keep playing** | **2 · Unfold pauses the game** | **3 · The screen wakes up (diegetic)** |
|---|---|---|---|
| The moment of unfolding | iOS's own unfold: ~0.3 s black, then its fade over the last outer frame | Same | Same |
| First second on the big screen | The run is already there. The world was frozen while the screen was dark. The image comes up from dark over ~0.4 s, like eyes adjusting, then time runs again. No menu | The PAUSED card on a frosted freeze of the room. One tap on RESUME | The picture powers on like a 1990 CRT (a bright line opens, the phosphor glow settles, ~0.6 s), then time runs again |
| Fairness | The Relay cannot move while the player cannot see | Same, and the player chooses when to go on | Same as 1 |
| Folding back to the outer screen | Same treatment | Pauses again | Same treatment |
| If the transition goes wrong (shell crash, app sent to the background) | Falls back to option 2's PAUSED card | PAUSED card | Falls back to the PAUSED card |
| Feels like | The phone just got bigger, and the horror never lets go | A safe, deliberate break that interrupts every unfold | A signature moment that fits the 1990 office fiction; may wear thin if the player folds often |
| Precedent | What continuity apps and games do (T4); no game masks the dark the way we would | No shipped game does this (T10) | None found |
| Cost | ≈ ½ day + Red's fold test:<br>• a native "display moved" signal, optionally a hinge head start (T12)<br>• a freeze window (our files, plus one hook in `FrontRooms3DGame.cs` through 关卡设计)<br>• an eye-adjust ramp on exposure (游戏视觉's path) | ≈ 1–2 h + Red's fold test. The existing pause on a new trigger. The frost capture adds one extra render right at the move: measure it | ≈ 1–2 days:<br>• option 1's freeze<br>• a new CRT power-on effect (游戏视觉 / 动画和动态图形)<br>• a photosensitivity check (COMMON rule 12) |
| Blocks | Nothing else | Nothing else | Waits for 游戏视觉's queue |

### Decided: option 2, the PAUSED card on unfold

Red, 2026-10-24, in this chat: "Use option 2 instead: unfolding shows the PAUSED card. Step 0 measurement as planned."

**Replaced on 2026-10-29.** Red's audit answer made the unfold open the settings page, and R6 built that. The plan below is the 10-24 plan, kept as history.

**Plan for option 2 (10-24).** Not built in this form.

1. **Detect a display move, not a rotation.**
   - The view's point size changes as an unordered pair (678 × 466 ↔ 951 × 669), or the scene's screen changes. A plain rotation only swaps the two numbers.
   - Our plugin already reads both every frame [L9].
   - A Split View resize on the inner display counts as a move too. It is the player turning their attention away, so a pause fits.
2. **On a move during a run or a close-up**, the game goes to `Phase.Paused`. The existing PAUSED card and frost appear on the new display. Folding back pauses too.
3. **Keep the first frame at the new size cheap,** because the iOS cloak waits for it (§2.2).
   - The frost capture is one extra half-resolution camera render plus blits.
   - Take it a frame after the move, or from the last frame before the move, so it doesn't lengthen the black. Measure both ways.
4. **Files.**
   - The trigger goes in the handheld block of `FrontRooms3DGame.Mobile.cs` (ours) and in `Input/FrontRoomsTouchControls.cs`, which already detects geometry changes.
   - The corners + launch session has unlanded edits in `FrontRoomsHandheld.cs`, `FrontRoomsTouchLayout.cs`, `FrontRoomsTouchControlsView.cs`, `FrontRoomsTouchPlaytestDriver.cs`, `FrontRooms3DGame.cs` and the adaptive tests. Option 2 stays out of those files until that session has landed; the playtest check (a forced Duo outer → inner profile switch ends in PAUSED) waits for it.
5. **Verify.** Red folds 5× again with the option 2 build, and we read the same SpringBoard and game log lines. Option 2 is done when every unfold ends on the PAUSED card, the run is intact, and the black is no longer than step 0's.

### My recommendation was option 1, with option 2 as its safety net

- It matches what the platforms ask for: keep the state across a fold, with no restart [E11][A6]. It also matches what players get from apps and games that support continuity (§3.3, §3.4).
- It removes the unfairness: the Relay cannot move while the screen is dark.
- It keeps the horror running, with no menu between the player and the room.
- The PAUSED card still appears whenever iOS sends the app to the background (already true), so the crash case keeps the run.

### What stays blocked until Red answers

- **Answered 06:34: the setting is S1** (main 8df529fe).
- **Answered (Red, this chat): the feel is option 2**, the PAUSED card on unfold.
- **Answered 10-29 (Red, 总控 B, "全部按推荐"): an unfold during play opens the settings page.** Built (R6). This replaces the line above.
- **Step 0** ran in part on 10-24 (§2.5). What is left is one unfold during play on a fresh simulator build of main (Red, 10-30).
- **The device check** needs Red's hand: `simctl` has no fold command, and Device Hub's fold cannot be scripted.
- **Not blocked:** the half-fold (division region) give-way. It is a separate open item in `IPHONE_DUO_ADAPTATION.md` and does not depend on this choice.

### Weak points of the plan

- Every number here comes from the simulator. Its GPU is slower and its SpringBoard was unhealthy today; a device may show a shorter black and may never crash.
- We cannot draw over iOS's black cloak or its snapshot. The first ~0.3 s and the fade are Apple's in every option.
- Freezing time hides a gap but does not shorten it; only a fast first frame does.
- S1 is borrowed time: Apple will stop honouring the key.

## 5. Media (links only; nothing downloaded)

Red's research rule asks for original footage first. Nothing was downloaded for this page: downloads need Red's OK, and third-party media stays outside the repo (COMMON rule 7). "Not watched" means the research agents read the page or its metadata but did not view the footage. Timestamps marked "inferred" come from live-blog clock times.

### 5.1 Original footage and official material

| What it shows | Link | Where in it | Status | Row |
|---|---|---|---|---|
| Apple's iPhone Duo launch: the system unfold and app continuity | https://www.youtube.com/watch?v=39BalPDuTo0 | Duo segment ≈ 1:00–1:15 (inferred) | not watched | G8, G9 |
| Apple tech talks for iPhone Duo (prepare, layouts, multiple displays, camera, bars, design) | https://developer.apple.com/videos/play/tech-talks/111461/ (and 111462–111466) | — | pages read; no transition visuals in any | E5–E10 |
| WWDC26 "Modernize your UIKit app": `UIRequiresFullScreen` becomes discrete resizing | https://developer.apple.com/videos/play/wwdc2026/278/ | ≈ 5:46 | page read | E14 |
| Dave2D, iPhone Duo hands-on: ZZZ folded closed mid-session, system glass animation, game resumes | https://www.youtube.com/watch?v=CEgynoERrqc | no timestamp given | not watched | G13, G14 |
| GeekCulture, iPhone Duo gaming test: Genshin, ZZZ, Delta Force letterboxed | https://www.youtube.com/watch?v=mG11rm123PE | — | not watched | G19, G20 |
| Samsung Galaxy Unpacked 2019, official replay: the first App Continuity demo (Google Maps, Netflix) | https://www.youtube.com/watch?v=ISjiJQGJ72o | not found (~2.5 h replay; the Fold opened the show) | not watched | G69, G72 |
| HotHardware, Galaxy Fold live demo at Unpacked 2019 | https://www.youtube.com/watch?v=QeF01uxlCzc | — | not watched | G74 |
| Google I/O 2023 keynote: video on the closed Pixel Fold carries over on opening | https://www.youtube.com/watch?v=cNfINi5CNbY | ≈ 1:57:00 (inferred) | not watched | G78, G79 |
| Samsung support explainer "App Continuity Galaxy Z Fold3 5G" | https://www.youtube.com/watch?v=DqSixZdJXME | — | not watched | G96 |
| Android's unfold animation, as source code | AOSP `FullscreenUnfoldTaskAnimator.java`, `UnfoldLightRevealOverlayAnimation.kt`, `LightRevealScrim.kt` | — | read (C1 checked by us) | C1, C4, C5 |
| Third-party rebuilds of Apple's fold animation (frosted glass driven by the hinge) | Empirical Health article; `marcoazeem/duo-open` | — | read | E53, E55 |

### 5.2 Our own evidence (FrontRooms, QA simulator, 2026-10-24)

Outside the repo, in `~/FrontRoomsInteractionWork/duo/fold_transition/evidence/` (rows L1–L7):

- the SpringBoard timelines of the crashing unfold (05:23:54), a clean fold/unfold pair (03:43:17 and 03:43:20) and the mid-run upright unfold (05:26:43);
- every fold transition today;
- the game's display events;
- both SpringBoard crash reports, and the script that summarises them.

### 5.3 Candidates that need Red's OK

| Candidate | What | Size | Why |
|---|---|---|---|
| **Our own recording** of the inner display while Red unfolds mid-run (`xcrun simctl io 3B6AEED9… recordVideo --display=primary-1`), during step 0 | An MP4 we make ourselves (not a download) | ~5–20 MB per 20 s | The best evidence of what our players see. Needs Red's hands on Device Hub, at a time when Red is not playing. Simulator capture can drop frames in some poses [E35] |
| Stills of the unfold moment from the Apple Event and Dave2D videos, after watching to check the timestamps | 2–4 JPEG frames, kept outside the repo | ~200–400 KB each | Shows Apple's own unfold next to ours |

## 6. Open questions and next steps

- **The one unfold left** (status at the top). On hardware, when an iPhone Duo is available: one unfold, filmed at 120 fps.
- **In-game timing:** log "geometry changed" → "first present at the new size" inside the game. Simulator screen capture is not reliable for this [E34][E35].
- **Display-link timing after a move:** log `CADisplayLink` timestamps before and after an unfold. The iOS 27 scene display link and Unity's Metal display link toggle are fallbacks [E27][E70].
- **Hinge order:** whether `UIHingeInteraction` reports before the display swap (undocumented) [E7][E11].
- **Folding back** (inner → cover) gets the same treatment: 0.46–0.84 s of black measured [L3].
- **Android build** (when it matters): check Resizeable Activity, `configChanges` and `appCategory = game` in the exported manifest. Apply the UUM-99058 workaround if it stays landscape-only [E77][E82][A1].
