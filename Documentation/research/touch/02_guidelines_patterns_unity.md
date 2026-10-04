# Touch controls 02 — Platform guidelines, ergonomics, FPS patterns, accessibility, Unity facts

Research for FrontRooms iOS/Android (phone + tablet) touch controls.
Compiled 2026-10-03. Engine context: Unity 6000.3.10f1, URP 17.3, Input System 1.18.0, `activeInputHandler = Both` (gameplay reads the legacy Input Manager).

**How to read this file**
- Every claim carries an inline source URL. "[P]" = primary source (platform owner, original paper, the developer of the game). "[S]" = secondary (press, player guide, third-party mirror) — use with care.
- **UNVERIFIED** = I could not confirm it against a primary source, or it is my own derived number. Derived numbers show their math.
- "cu" = canvas units in FrontRooms' current HUD canvas (CanvasScaler *Scale With Screen Size*, 1920×1080 reference, `matchWidthOrHeight = 0.5`, set in `Assets/Scripts/FrontRooms3DGame.cs` line 1644).
- Nothing was downloaded. Figures worth putting on a slide are listed under **Media candidates** with the URL and what they show.

---

## 0. The strongest findings (read this if nothing else)

1. **Apple now has a first-party design brief for exactly this game.** The HIG *Game controls* page (updated 9 June 2025) and the WWDC26 session 358 *Make your game great with touch* say: a movement stick that appears wherever the left thumb lands, camera control by dragging on the right half of the screen instead of a second stick, sprint folded into how far you push the stick, contextual buttons that show only when usable, a press state that is visible even under the finger, frequent controls at least 44×44 pt and menus at least 28×28 pt ([HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls), [WWDC26-358](https://developer.apple.com/videos/play/wwdc2026/358/)) [P].
2. **Accessibility minimums are much larger than platform minimums.** Xbox XAG 107 suggests default touch targets of **15 mm on phones and 24 mm on tablets** ([XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)). By comparison, Apple's 44 pt is 7.3 mm on an iPhone 17 Pro and Android's 48 dp is about 7.5 mm on a Pixel 8. Hoober's field data agrees: about 7 mm is enough at screen centre, but corners need about 12 mm ([UXmatters 2017](https://www.uxmatters.com/mt/archives/2017/03/design-for-fingers-touch-and-people-part-1.php)) [P].
3. **FrontRooms' 72 cu HUD margin is narrower than the iPhone Dynamic Island inset in landscape.** On iPhone 16/17 Pro the left and right insets are 62 pt. At the current CanvasScaler settings that is about 151 cu, against a 72 cu margin (math in §7). On iPad 11" the same canvas is only **1727 cu wide**, so a 1920-wide 12-column grid does not exist on iPad.
4. **Two project settings conflict with Android.** Unity 6.3 documents Active Input Handling **"Both … isn't supported on Android"**, and the project is set to Both ([Unity Android Player settings](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html)). Separately, the project uses the GameActivity entry point. Unity says GameActivity libraries older than 4.4.x cause ANRs, and to get 4.4.0 you need **6000.3.13f1 or later**. The project is on 6000.3.10f1 ([Unity GameActivity requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/android-application-entries-game-activity-requirements.html)) [P].
5. **Google Play deadlines are already live.** Since 31 Aug 2026, new apps and updates must **target API 36 (Android 16)**; an extension to 1 Nov 2026 is available ([Play target SDK](https://developer.android.com/google/play/requirements/target-sdk)). At API 36: edge-to-edge cannot be opted out of, `onBackPressed` is no longer called, and `KEYCODE_BACK` is no longer dispatched ([Android 16 behaviour changes](https://developer.android.com/about/versions/16/behavior-changes-16)) [P].
6. **The Android gesture-exclusion cap is 200 dp of vertical extent, and it does not apply while the nav bar is stickily hidden.** You can never exclude the home gesture ([View reference](https://developer.android.com/reference/android/view/View#setSystemGestureExclusionRects(java.util.List%3Candroid.graphics.Rect%3E)), [gesture nav](https://developer.android.com/develop/ui/views/touch-and-input/gestures/gesturenav)) [P].
7. **iPad has no haptics.** Apple: "Some devices don't support haptic feedback, including iPad" ([Core Haptics](https://developer.apple.com/documentation/corehaptics/preparing-your-app-to-play-haptics)). Feedback on tablets must come from audio and visuals [P].
8. **The closest reference game uses this scheme.** Alien: Isolation mobile (Feral) offers trackpad-style look as an option, aim assist that slows the reticule near targets, a "Rapid Tap Assist" that turns mashing into a hold, and a full layout editor (drag, pinch to resize, per-input opacity, five custom presets, left- or right-hand reset) ([Feral FAQ](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)) [P].
9. **The Unity Device Simulator cannot simulate multitouch or the gyroscope.** "It can only simulate one finger touch" ([Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/device-simulator-introduction.html)). Testing the two-thumb scheme requires real devices [P].
10. **Unity's mobile default frame rate is 30 fps.** With vSyncCount 0 and targetFrameRate −1, iOS and Android render at a fixed 30 fps ([Application.targetFrameRate](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html)). Google's Level Up guidelines require at least 60 fps by default on reference devices ([Level Up](https://developer.android.com/games/guidelines)) [P].

---

## 1. Apple (iOS / iPadOS)

### 1.1 HIG — Game controls → Touch controls (updated 9 Jun 2025) [P]
Source: https://developer.apple.com/design/human-interface-guidelines/game-controls (text read from the HIG JSON feed for that page).

- **Only show virtual controls when they pay for themselves.** Virtual controls suit games that "offer a large number of actions or require players to control movement". The page also says to "look for opportunities to reduce the amount of virtual controls … by associating actions with in-game gestures instead", for example "tap objects to select them instead of adding a virtual selection button". For FrontRooms this argues for tapping the door itself.
- **Placement.** Respect device boundaries and safe areas. Do not overlap the Home indicator or the Dynamic Island. "Place frequently used buttons near a player's thumb, avoiding the circular regions where players expect movement and camera input to happen. Place secondary controls, like menus, at the top of the screen."
- **Size.** "frequently used controls are a minimum size of 44x44 pt, and less important controls, such as menus, are a minimum size of 28x28 pt".
- **Press states.** "Always include visible and tactile press states." Add a visual effect such as a glow "that they can see even when their finger is covering the control", and combine it with sound and haptics.
- **Symbols.** Use artwork for the action itself and avoid controller names such as "A, X, or R1".
- **Show and hide.** Hide controls when an action isn't available. For example, "consider hiding movement controls until a player touches the screen".
- **Combine functions.** Use double tap and touch-and-hold for variations of an action. "For multiple actions, such as walking or sprinting, consider combining the actions into a single control."
- **Movement and camera.** Movement goes on the left and camera on the right, using "as large of an input area as possible". "For movement control, opt to show a virtual thumbstick wherever the player lands their thumb instead of a static thumbstick position. For camera control, opt to use direct touch to pan the camera instead of a virtual thumbstick."
- **Platform default input.** Always provide a fallback to touch, even if you support game controllers.

### 1.2 HIG — Designing for games [P]
Source: https://developer.apple.com/design/human-interface-guidelines/designing-for-games
- iOS/iPadOS button size: **default 44×44 pt, minimum 28×28 pt**. Text: **default 17 pt, minimum 11 pt**.
- Layouts must work at "16:10, 19.5:9, and 4:3" and use the platform safe areas.
- "Help players personalize their experience": type size, control mapping, motion intensity, sound balance. Apple offers Unity plug-ins for accessibility personalisation (`UnityPlugins`).

### 1.3 Touch Controller framework (new, iOS/iPadOS 26.0+) [P]
Sources: https://developer.apple.com/documentation/touchcontroller · https://developer.apple.com/documentation/touchcontroller/tcthumbstick · https://developer.apple.com/documentation/touchcontroller/tctouchpad · https://developer.apple.com/documentation/touchcontroller/tccontrollayout · https://developer.apple.com/documentation/touchcontroller/tctouchcontroller

- Availability: iOS 26.0, iPadOS 26.0, Mac Catalyst 26.0 and visionOS. "Integrate onscreen touch controls into your Metal-based games." Controls appear to the game as a `GCController`, so code written for game controllers works unchanged.
- Control types: `TCButton`, `TCDirectionPad`, `TCSwitch`, `TCThumbstick`, `TCThrottle`, `TCTouchpad`. `TCTouchpad` "reports absolute coordinates or delta movements"; set `reportsRelativeValues` for deltas.
- `TCThumbstick` has `hidesWhenNotPressed`, `highlightDuration`, `stickSize` (in points) and `colliderShape`. WWDC26-358 uses `colliderShape = leftSide/rightSide` so that a stick or touchpad owns half the screen.
- Layout: `TCControlLayout` has `anchor`, `anchorCoordinateSystem`, `offset`, `size` (points) and `zIndex`. WWDC26-358 describes "nine anchor points". There is also `automaticallyLayoutControls(for:)`.
- You call `handleTouchBegan/Moved/Ended(at:index:)` with a per-touch index, and you draw the controls with `render(using:)` on a Metal render command encoder.
- **Unity relevance.** Apple's official Unity plug-ins (Core, Accessibility, BackgroundAssets, CoreHaptics, GameController, GameKit, PHASE, SpatialController, StoreKit) include **no Touch Controller plug-in** ([apple/unityplugins](https://github.com/apple/unityplugins)). Using it from Unity would mean a custom native plug-in that injects into Unity's Metal encoder: **UNVERIFIED feasibility, not recommended.** Copy its *patterns* in Unity UI instead.
- Older option: `GCVirtualController` (iOS 15) gives a default on-screen controller UI ([Apple article](https://developer.apple.com/documentation/gamecontroller/adding-virtual-controls-to-games-that-support-game-controllers-in-ios); [WWDC21-10081](https://developer.apple.com/videos/play/wwdc2021/10081/)).

### 1.4 WWDC and Apple design sessions on touch for games [P]
| Session | Key points (timestamps are transcript cue times) |
|---|---|
| **WWDC26-358 "Make your game great with touch"** https://developer.apple.com/videos/play/wwdc2026/358/ | 5:16 nine anchors; group controls per anchor so sizes stay physically constant across devices. 5:50–6:53 safe areas; add `safeAreaInsets` to offsets. 7:25 keep the centre clear; frequent actions by the thumbs, menus at the top. 10:05 a 1:1 port of a pad "makes the screen cluttered". 12:17 remove unavailable actions: sticks hidden until touched, the pick-up button only when an item is near, QTE button only during a QTE. 13:19 the pick-up button appears **next to the item**. 14:52 full screen for movement and camera; "single left thumbstick without an extra button" for sprint. 15:24 "players can't physically feel where their finger is … expand the input area as much as possible". 16:30 "A small tilt means the character moves at a normal pace. If it's a big tilt, the character will sprint." 17:00–18:02 right-half `TCTouchpad` with relative values: "no latency or drift … no over-rotation". 20:35 hold a button and drag to aim, with the drag read from raw touch deltas in `touchesMoved`. 21:07 press states. 21:38 a glowing halo on the stick ring while sprinting. |
| **WWDC24-10085 "Design advanced games for Apple platforms"** https://developer.apple.com/videos/play/wwdc2024/10085/ | 7:57 safe areas, Home indicator, Dynamic Island. 8:59 safe areas are "guides … not meant to be the actual margins"; render full bleed. 11:36 44×44 pt default, 28 pt for less critical controls. 16:49–18:22 hidden left stick, broad input area, sprint built into the stick, direct-touch camera "more akin to … a mouse". 19:23–19:54 placement and two-thumb concurrency (swap which hand owns which control so aim and fire happen together). 21:58–22:28 press states drawn outside the control bounds; "subtle haptics on touch down and touch up". |
| **Meet with Apple 243 "Design great interfaces for handheld games"** (Dylan Edwards, 15 Dec 2025) https://developer.apple.com/videos/play/meet-with-apple/243/ | Restates the WWDC24 guidance and adds: 2:15 "break up your interface into smaller components … anchor those elements to their respective edges"; 3:50 safe areas include "gesture regions"; 7:58 use scroll views in settings rather than shrinking text; 13:14 "as I drag my finger further, the character can run faster". Mentions Apple's Unity plug-ins for controller glyphs. |
| WWDC25-209 "Level up your games" https://developer.apple.com/videos/play/wwdc2025/209/ | ~13:33 introduces the Touch Controller framework ("the vast majority of players won't have a controller available"). |
| Haptics: WWDC19-520 "Introducing Core Haptics" https://developer.apple.com/videos/play/wwdc2019/520/ · WWDC19-223 "Expanding the Sensory Experience with Core Haptics" https://developer.apple.com/videos/play/wwdc2019/223/ · WWDC21-10278 "Practice audio haptic design" https://developer.apple.com/videos/play/wwdc2021/10278/ | 10278 at ~3:13: three principles, "causality, harmony, and utility". |

### 1.5 Home indicator and system-gesture deferral [P]
- `preferredScreenEdgesDeferringSystemGestures` (iOS 11+) lets the app's gestures take precedence on the edges you return. "Whenever possible, you should allow the system gestures to take precedence. However, immersive apps can use this property." If the edges change, call `setNeedsUpdateOfScreenEdgesDeferringSystemGestures()` ([doc](https://developer.apple.com/documentation/uikit/uiviewcontroller/preferredscreenedgesdeferringsystemgestures)).
- `prefersHomeIndicatorAutoHidden`: returning true "is no guarantee that the indicator will be hidden" ([doc](https://developer.apple.com/documentation/uikit/uiviewcontroller/prefershomeindicatorautohidden)).
- HIG *Going full screen*: "Consider deferring system gestures to prevent accidental exits in a full-screen app or game … If supporting this results in unexpected exits, you can enable two swipes rather than one to exit." On iPad, games may ask the system to ignore an initial swipe up so the Dock isn't revealed by accident ([HIG](https://developer.apple.com/design/human-interface-guidelines/going-full-screen)).
- HIG *Gestures*: avoid custom gestures that conflict with system gestures; "within games … developers can work around this area by deferring the system gesture". Games may recognise multiple gestures at once, such as a joystick plus fire buttons ([HIG](https://developer.apple.com/design/human-interface-guidelines/gestures)).
- **Unity mapping.** `PlayerSettings.iOS.deferSystemGesturesMode` ("the system ignores the first swipe … HIG don't recommend turning on this setting") and `PlayerSettings.iOS.hideHomeButton`. **If hideHomeButton is enabled, deferSystemGesturesMode has no effect** ([Unity defer](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings.iOS-deferSystemGesturesMode.html), [Unity hideHomeButton](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings.iOS-hideHomeButton.html)). Both can also be set at runtime through `iOS.Device.deferSystemGesturesMode` and `iOS.Device.hideHomeButton`. Project today: `deferSystemGesturesMode: 0`, `hideHomeButton: 0` (`ProjectSettings/ProjectSettings.asset`).

### 1.6 Safe areas, Dynamic Island, landscape [P/S]
- A safe area is "the area within a window that isn't covered … by a hardware feature … Respecting the safe area is essential to make sure … the Dynamic Island don't obstruct content and controls" ([HIG Layout](https://developer.apple.com/design/human-interface-guidelines/layout)). Even landscape-only games must resize well.
- The per-device size tables no longer appear in the HIG Layout JSON (its change log says 9 Sep 2026 "Updated guidance"), so they appear to have been removed (**UNVERIFIED** inference). Point sizes below are derived from Apple's pixel specs ÷ scale factor; insets come from Use Your Loaf [S].
  - iPhone 17 / 17 Pro: 2622×1206 px at 460 ppi ([Apple iPhone 17 specs](https://www.apple.com/iphone-17/specs/)) → **874×402 pt @3x**. Landscape insets **L/R 62, top 20, bottom 20** on iOS 26 ([Use Your Loaf iPhone 17](https://useyourloaf.com/blog/iphone-17-screen-sizes/)) [S].
  - iPhone 16 Pro on iOS 18: landscape insets top 0, bottom 21, L/R 62 ([Use Your Loaf iPhone 16](https://useyourloaf.com/blog/iphone-16-screen-sizes/)) [S]. The top inset changed between OS versions, so **read `Screen.safeArea` at runtime and never hard-code it.**
  - iPhone 17 Pro Max 440×956 pt; iPhone Air 420×912 pt with landscape insets L/R 68 and bottom 29 ([Use Your Loaf](https://useyourloaf.com/blog/iphone-17-screen-sizes/)) [S].
  - iPhone 18 Pro (shipping Oct 2026): 2622×1206 px at 460 ppi; iPhone 18 Pro Max 2868×1320 px ([Apple](https://www.apple.com/iphone-18-pro/specs/)). Assuming @3x this gives 874×402 and 956×440 pt (**UNVERIFIED scale**). Its Dynamic Island is reportedly smaller ([Engadget, S](https://engadget.com/2253951/apples-iphone-18-pro-has-an-a20-pro-chip-and-smaller-dynamic-island/)), so insets may differ (**UNVERIFIED**).
  - **New form factor: iPhone Duo (foldable).** Inner screen 7.6", 1878×2670 px at 430 ppi; outer screen 5.4", 1398×2034 px at 460 ppi; both have a Dynamic Island ([Apple](https://www.apple.com/iphone-duo/specs/)). The inner screen in landscape is about 1.42:1, close to an iPad, so the phone layout cannot assume 19.5:9. Point sizes are **UNVERIFIED** (about 890×626 pt if @3x).
  - iPad 11" (A16): 2360×1640 px at 264 ppi → **1180×820 pt @2x** ([Apple](https://www.apple.com/ipad-11/specs/)). iPad Pro 11": 2420×1668 px → 1210×834 pt. iPad Pro 13": 2752×2064 px → 1376×1032 pt ([Apple](https://www.apple.com/ipad-pro/specs/)). iPad landscape insets (about top 24 / bottom 20 pt) are **UNVERIFIED**.
- **iPadOS windowing.** `UIRequiresFullScreen` is deprecated from iPadOS 26. From iOS/iPadOS 27 SDK builds it "no longer opts your app out of resizing", and the scene is resized discretely ([TN3192](https://developer.apple.com/documentation/technotes/tn3192-migrating-your-app-from-the-deprecated-uirequiresfullscreen-key)). Unity 6.3 "support[s] all multitasking modes" ([Unity iOS requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/ios-requirements-and-compatibility.html)). The project currently sets `uIRequiresFullScreen: 1`, so the **touch layout must re-layout whenever the screen size changes.**

### 1.7 Haptics on Apple devices [P]
- HIG *Playing haptics*: use haptics consistently with a clear cause and effect; let them complement visual and audio feedback; don't overuse them; prefer short haptics for discrete events; "Make haptics optional"; and haptics "might impact other user experiences", such as the camera, gyro and microphone ([HIG](https://developer.apple.com/design/human-interface-guidelines/playing-haptics)).
- Custom haptics are built from **transient** events (taps) and **continuous** events (sustained vibration), each with intensity and sharpness. The HIG's own game examples: a strong landing versus a subtle "approach of footsteps or a looming danger".
- **No haptics on iPad** (also not on iPod touch or Vision Pro). Check `supportsHaptics` and fall back to audio or visuals ([Core Haptics](https://developer.apple.com/documentation/corehaptics/preparing-your-app-to-play-haptics)).
- UIKit's simple generators are `UIImpactFeedbackGenerator`, `UISelectionFeedbackGenerator` and `UINotificationFeedbackGenerator` ([doc](https://developer.apple.com/documentation/uikit/uifeedbackgenerator)). For Unity, use Apple's **Apple.CoreHaptics** plug-in ([apple/unityplugins](https://github.com/apple/unityplugins)).

---

## 2. Android

### 2.1 Touch targets [P]
- "each interactive UI element have a focusable area … of at least 48dp×48dp. Larger is even better" ([Android accessibility](https://developer.android.com/guide/topics/ui/accessibility/apps)).
- "A touch target of 48x48dp results in a physical size of about 9mm, regardless of screen size … separated by 8dp of space or more" ([Google Accessibility Help](https://support.google.com/accessibility/android/answer/7101858)). The nominal arithmetic is 48 × 25.4/160 = **7.62 mm**, and on a Pixel 8 it is **7.54 mm** (§7). Google's "about 9 mm" is therefore generous. **Design to millimetres measured on the device.**

### 2.2 Edge-to-edge and display cutouts [P]
- Edge-to-edge is enforced once you target SDK 35 on Android 15 ([edge-to-edge](https://developer.android.com/develop/ui/views/layout/edge-to-edge)). Targeting API 36 removes the opt-out: `windowOptOutEdgeToEdgeEnforcement` "is deprecated and disabled" ([Android 16](https://developer.android.com/about/versions/16/behavior-changes-16)).
- Cutout modes are `DEFAULT`, `SHORT_EDGES`, `NEVER` and `ALWAYS`. On SDK 35+ non-floating windows, DEFAULT, SHORT_EDGES and NEVER are all interpreted as ALWAYS. The cutout is retrieved with `WindowInsetsCompat.getDisplayCutout()` ([display cutout](https://developer.android.com/develop/ui/views/layout/display-cutout)).
- Inset types: **system bar insets** are for tappable views that must not be covered. **System gesture insets** are "areas … where system gestures take priority": "a bottom inset for the home gesture, and a left and right inset for the back gestures". Use them to pad swipeable regions, "swiping in games" included ([edge-to-edge](https://developer.android.com/develop/ui/views/layout/edge-to-edge)). Android 10 added `getMandatorySystemGestureInsets()` ([gesture nav](https://developer.android.com/develop/ui/views/touch-and-input/gestures/gesturenav)).
- Unity's *Render outside safe area* has **no effect on Android 15+** because the app already uses the whole screen ([Unity Android Player settings](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html)). Project: `androidRenderOutsideSafeArea: 1`.

### 2.3 Gesture navigation and system-gesture exclusion [P]
- Back is an inward swipe from the left or right edge, and apps can opt out of it in specific regions. **Home and quick-switch at the bottom cannot be opted out of** ([gesture nav](https://developer.android.com/develop/ui/views/touch-and-input/gestures/gesturenav)).
- Games: use `Window.setSystemGestureExclusionRects()`, and "Games must make sure to only exclude these areas when necessary, such as during gameplay" (same page).
- **The limit, quoted:** "the system will put a limit of 200dp on the vertical extent of the exclusions it takes into account. The limit does not apply while the navigation bar is stickily hidden" ([View.setSystemGestureExclusionRects](https://developer.android.com/reference/android/view/View#setSystemGestureExclusionRects(java.util.List%3Candroid.graphics.Rect%3E))). The 200 dp is per vertical edge.

### 2.4 Immersive mode [P]
- Hide the system bars with `WindowInsetsControllerCompat.hide(systemBars())`. For games, use `BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE`: the bars return transiently on an edge swipe and auto-hide. "Immersive mode helps users avoid accidental exits during a game" ([immersive](https://developer.android.com/develop/ui/views/layout/immersive)).
- A game that needs swipes near the home area can use immersive mode, which "disables the system gestures while the user is interacting with the game, but lets the user re-enable the system gestures by swiping from the bottom" ([gesture nav](https://developer.android.com/develop/ui/views/touch-and-input/gestures/gesturenav)).
- In desktop windowing the caption bar stays visible "even for games in immersive mode" ([immersive](https://developer.android.com/develop/ui/views/layout/immersive)).
- Unity equivalents: *Start in fullscreen* and *Hide Navigation Bar* ([Unity Android Player settings](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html)). Project: `androidStartInFullscreen: 1`. Whether Unity uses the sticky or transient behaviour internally is **UNVERIFIED**.

### 2.5 Back gesture and predictive back [P]
- Apps targeting API 36 on Android 16: the predictive-back animations are on by default, "`onBackPressed` is not called and `KeyEvent.KEYCODE_BACK` is not dispatched anymore". Apps can temporarily opt out with `android:enableOnBackInvokedCallback="false"` ([Android 16](https://developer.android.com/about/versions/16/behavior-changes-16), [predictive back](https://developer.android.com/guide/navigation/custom-back/predictive-back-gesture)).
- Unity: *Predictive Back Gesture Support* / `PlayerSettings.Android.predictiveBackSupport` — "Enable to use Android's OnBackInvokedCallback for handling back events on Android 13 and above" ([Unity API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings.Android-predictiveBackSupport.html)). Project: `androidPredictiveBackSupport: 0`. How Unity surfaces the back event to scripts in each mode (for example as Escape) is **UNVERIFIED — test on an Android 16 device.**
- Games are exempt from Android 16's large-screen orientation, resize and aspect-ratio overrides if `android:appCategory="game"`. Unity's *App Category* defaults to Game for new projects "to ensure that your application is exempted from Android 16 behavior changes" ([Android 16](https://developer.android.com/about/versions/16/behavior-changes-16); [Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html)). Project: `AndroidIsGame: 1`, `useAndroidAppCategory: 1`.

### 2.6 Google Play requirements (as of Oct 2026) [P]
- **Target API:** "New apps and app updates must target Android 16 (API level 36) or higher" from 31 Aug 2026, with an extension available to 1 Nov 2026. Existing apps must target API 35+ to stay visible to new users on newer OS versions ([Play target SDK](https://developer.android.com/google/play/requirements/target-sdk)).
- **64-bit:** required. "IL2CPP is required for 64-bit support. The Mono backend does not support 64-bit ARM" ([Play 64-bit](https://developer.android.com/google/play/requirements/64-bit)). Project: Android `scriptingBackend: 1` (IL2CPP) and `AndroidTargetArchitectures: 2` (ARM64).
- **16 KB pages:** apps targeting API 35+ must support 16 KB pages on 64-bit devices. "Starting February 1, 2027, if your app updates don't support 16 KB … you won't be able to release these updates" ([page sizes](https://developer.android.com/guide/practices/page-sizes)). Unity 6 supports this ([Android Unity guide](https://developer.android.com/games/engines/unity/unity-on-android); [Unity Android requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/android-requirements-and-compatibility.html)).
- **Play Games "Level Up" guidelines** (opt-in program, not a store requirement) ([guidelines](https://developer.android.com/games/guidelines)):
  - LU-LS-GAA: use insets so controls don't overlap system bars or cutouts.
  - LU-LS-GAB: handle rotation, fold, split-screen and window resizing "without … touch mapping offsets".
  - LU-LS-GAC: landscape at 4:3, 16:10 and 21:9 without letterboxing; landscape-only games must "cleanly letterbox" in portrait.
  - LU-PR-GAA: ≥60 fps by default on reference devices (average ≥55, P90 ≥50, P99 ≥30). Tablets and foldables are exempt.
  - LU-IC-GAA: "fully playable with a controller, keyboard, and mouse".
  - LU-FF-GAA: phones, foldables and tablets by 30 Sep 2026.

### 2.7 Window size classes [P]
Width: compact <600 dp, medium 600–840, expanded 840–1200, large 1200–1600, extra-large ≥1600. Height: compact <480 dp covers "99.78% of phones in landscape" ([Android](https://developer.android.com/develop/ui/compose/layouts/adaptive/use-window-size-classes)). A typical 20:9 phone in landscape is about **915×412 dp**; for example the Pixel 8 is 1080×2400 px at density 2.625 → 411×914 dp ([YesViz, S](https://yesviz.com/devices/google-pixel-8/)).

### 2.8 Haptics and device variability [P]
- Principles: "clear" haptics for discrete events; "rich" haptics "are supported by fewer devices, so … have a fallback". **"Given the choice of buzzy haptics or no haptics for touch feedback, choose no haptics."** Avoid legacy `createOneShot` and `Vibrator.vibrate(long)` because they feel buzzy ([principles](https://developer.android.com/develop/ui/views/haptics/haptics-principles)).
- Order of preference: (1) `HapticFeedbackConstants` via `View.performHapticFeedback`, which needs no VIBRATE permission and respects the user's touch-feedback setting ([haptic feedback](https://developer.android.com/develop/ui/views/haptics/haptic-feedback)); (2) `VibrationEffect` predefined effects and `Composition` primitives (CLICK, TICK, LOW_TICK, THUD, SPIN, QUICK_RISE, SLOW_RISE, QUICK_FALL; Android 11+).
- Device variability: if a composition contains **one unsupported primitive, the whole vibration fails**. Check `arePrimitivesSupported()`; `hasAmplitudeControl()` gates smooth waveforms. Android 16's `VibrationEffect.Builder` adds automatic fallback; `WaveformEnvelopeBuilder` does not ([custom haptics](https://developer.android.com/develop/ui/views/haptics/custom-haptic-effects)).
- Tuning numbers from the same page: gaps of **≥50 ms** between primitives; intensity steps of **≥1.4×**; scales **0.5 / 0.7 / 1.0** for low, medium and high.

---

## 3. Ergonomics

### 3.1 How people hold devices [P]
- Hoober observed 1,333 people in public, 780 of them touching the screen: **49 % one-handed, 36 % cradled, 15 % two-handed**. Of the two-handed users, **90 % held the phone in portrait and only 10 % in landscape** ([UXmatters 2013](https://www.uxmatters.com/mt/archives/2013/02/how-do-users-really-hold-mobile-devices.php)). Landscape two-thumb play is therefore a posture the game asks for, not a default. Teach it on the title screen.
- Tablets: "Two-thirds of tablet usage" is "on a stand or resting on a table or in their lap". Phones are held about 30 cm from the eyes ([UXmatters 2017 Pt 2](https://www.uxmatters.com/mt/archives/2017/05/design-for-fingers-touch-and-people-part-2.php)). On iPad, players may not be gripping the edges at all, so offer a layout preset with controls higher and further in (**derived**).

### 3.2 Touch accuracy by screen region [P]
- Targets at screen centre can be as small as **7 mm**; corners need about **12 mm**. "All of my suggested target sizes contain only 95% of all observed taps" ([UXmatters 2017 Pt 1](https://www.uxmatters.com/mt/archives/2017/03/design-for-fingers-touch-and-people-part-1.php)).
- Centre spacing can be 7 mm on centre, top and bottom edges 10–12 mm. "Both the left and right edges of a screen are equally hard to reach." Tablet data was described as insufficient ([UXmatters 2013-11](https://www.uxmatters.com/mt/archives/2013/11/design-for-fingers-and-thumbs-instead-of-touch.php)).
- Book version: Hoober & Berkman, *Touch Design for Mobile Interfaces* (Smashing, 2021). A secondary summary cites 11 mm at the top and 12 mm at the bottom ([Smart Interface Design Patterns, S](https://smart-interface-design-patterns.com/articles/accessible-tap-target-sizes/), **UNVERIFIED**, not read).
- **Implication.** Thumb-zone buttons in the lower corners sit exactly where Hoober measured the *lowest* accuracy. That is one more reason to make them large (12–15 mm), not 44 pt.

### 3.3 Game-specific studies [P]
- **Huang & Huang 2017**, *Thumb touch control range and usability factors of virtual keys for smartphone games*, J. Multimodal User Interfaces ([doi](https://doi.org/10.1007/s12193-017-0248-9)). Two-thumb landscape with a fighting game; ten dimensions reduced to four factors: key feedback degree, key usability, relative key position, relative key size. Abstract via secondary snippet; specific reach numbers **UNVERIFIED** (paywalled).
- **Teather, Roth & MacKenzie 2017**, *Tilt-Touch synergy: Input control for "dual-analog" style mobile games*, Entertainment Computing ([doi](https://doi.org/10.1016/j.entcom.2017.04.005), [PDF](https://www.csit.carleton.ca/~rteather/pdfs/ec2017.pdf)). "touch-based controls offered the best performance, tilt-based movement control was comparable … tilt-based orientation control significantly altered, and in certain cases impaired, participant navigation." The related-work section summarises Zaman et al.: virtual controls on an iPhone → "significantly slower and died more frequently" than physical controls on a Nintendo DS.
- **Zaman, Natapov & Teather 2010**, *Touchscreens vs. traditional controllers in handheld gaming*, FuturePlay ([doi](https://doi.org/10.1145/1920778.1920804)).
- **Baldauf et al. 2015**, *Investigating On-Screen Gamepad Designs for Smartphone-Controlled Video Games*, ACM TOMM ([doi](https://doi.org/10.1145/2808202)). Directional pad and joystick "encourage drifting and unintended operations". The **floating joystick "can reduce the glances at the device"**. Tilt was not precise or quick enough.
- **Bergstrom-Lehtovirta & Oulasvirta 2014**, *Modeling the functional area of the thumb on mobile touchscreen surfaces*, CHI ([doi](https://doi.org/10.1145/2556288.2557354)). Mostly one-handed reach modelling; its applicability to landscape two-thumb play is **UNVERIFIED**.
- **Hynninen 2012**, *First-person shooter controls on touchscreen devices: A heuristic evaluation of three games on the iPod touch*, M.Sc. thesis, Univ. of Tampere (cited in Teather 2017). Not read: **UNVERIFIED** content.
- **Andrade 2024**, *Designing mobile game input unreachability: risks when placing items out of the functional area*, AHFE ([open access](https://openaccess.cms-conferences.org/publications/book/978-1-964867-13-7/article/978-1-964867-13-7_13)). Warns that placing items out of reach is used deliberately for ads and risks thumb strain.

### 3.4 Phone vs tablet
| | Phone (landscape) | Tablet (landscape) | Source |
|---|---|---|---|
| Typical size | 874×402 pt (iPhone 17 Pro, 145×67 mm); 914×411 dp (Pixel 8, 144×65 mm) | 1180×820 pt (iPad 11", 227×158 mm); 1376×1032 pt (iPad Pro 13", 265×199 mm) | Apple specs above; §7 math |
| Aspect | 19.5:9 to 20:9 (2.17–2.22); the iPhone Duo inner screen is about 1.42 | 1.33–1.44 | — |
| Default touch-target suggestion | **15 mm** | **24 mm** | [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107) |
| Grip | Two thumbs at the short edges | Often propped on a stand, table or lap (two-thirds of use) | [Hoober 2017](https://www.uxmatters.com/mt/archives/2017/05/design-for-fingers-touch-and-people-part-2.php) |
| Haptics | Yes (iPhone; Android varies) | **None on iPad** | [Core Haptics](https://developer.apple.com/documentation/corehaptics/preparing-your-app-to-play-haptics) |
| FrontRooms canvas at match 0.5 | ~2123×977 cu | ~1727×1200 cu (11"), 1663×1247 (13") | §7 |

---

## 4. Touch-FPS design patterns

### 4.1 Movement stick: fixed, floating or dynamic
- **Apple:** show the stick "wherever the player lands their thumb" ([HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls)). WWDC26-358 makes the whole left half the collider (`colliderShape = leftSide`) and hides the stick when it isn't touched (`hidesWhenNotPressed`) [P].
- **Epic (Fortnite/UEFN guidance):** "Joysticks, when used, are dynamic: they appear where the thumb lands, not in a fixed corner." Also: "No more than 5–6 active controls should be visible at once." Primary actions go in "the thumb zone (the bottom corners)" ([Epic](https://dev.epicgames.com/documentation/fortnite/designing-for-mobile-in-fortnite)) [P].
- **Research:** the floating joystick reduced glances at the device ([Baldauf 2015](https://doi.org/10.1145/2808202)) [P].
- **Accessibility:** a "fully dynamic" stick that appears wherever a touch lands is listed as a way to let players rearrange interfaces ([GAG — rearrange](https://gameaccessibilityguidelines.com/allow-interfaces-to-be-rearranged/)). Pik-Pok's horror game **Into the Dead** offered four layouts (no UI, stick on the left, stick on the right, buttons on both sides), and **each was used by roughly 25 % of players** (same page) [P].
- **Counter-pattern:** competitive players often prefer a *locked* stick for muscle memory, for example the Brawl Stars "locked joystick" option ([player guide, S](https://ar-pay.com/blog/en/articles/brawl-stars/)). Offer *Fixed* as a setting.
- **Unity mapping:** `OnScreenStick.behaviour` [P] ([API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/api/UnityEngine.InputSystem.OnScreen.OnScreenStick.Behaviour.html)):
  - `RelativePositionWithStaticOrigin`: fixed origin; the stick starts centred and moves relative to the press (classic fixed stick).
  - `ExactPositionWithStaticOrigin`: fixed origin, but "may begin from an actuated position" (a tap off-centre is an immediate input).
  - `ExactPositionWithDynamicOrigin`: "center of origin is determined by the initial press position". Only presses inside `dynamicOriginRange` (a radius around the component) count; others are ignored. This is the floating stick.

### 4.2 Dead zones and response curve
- Josh Sutphin, *Doing Thumbstick Dead Zones Right* (Game Developer, Apr 2013) [P] ([link](https://www.gamedeveloper.com/disciplines/doing-thumbstick-dead-zones-right)):
  - Axial dead zones snap to the cardinal directions.
  - Radial dead zones (`if (mag < dz) v = 0`) are fine for most games.
  - **Scaled radial** is recommended: `v = v.normalized * ((mag - dz) / (1 - dz))`, so there is no "kick" at the threshold.
  - Physical-stick values: about **0.25** for OUYA pads, about **0.1** for Xbox 360 pads.
- A virtual stick has no spring wear, but it has finger-contact jitter and needs a resting-thumb tolerance. **Proposal (UNVERIFIED, tune on device):** scaled radial with dz = **0.08–0.12 of stick radius**, about 1–1.5 mm on a 12–15 mm radius, plus an optional outer saturation at 0.95.
- Feral exposes the dead zone as a player setting in Alien: Isolation mobile, alongside sensitivity per camera method ([Feral interview, S/P](https://www.gfinityesports.com/alien-isolation-mobile/interview/)).
- Unity's `movementRange` is the **edge length in screen pixels** of the box the stick moves in: "A movement range of 50 … means … 25 pixels up, down, left, and right" ([Unity OnScreen](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/OnScreen.html)). **It must be scaled by DPI**, or the stick throw changes physically from device to device.

### 4.3 Look-drag (camera) sensitivity and acceleration
- Apple: use direct touch to pan the camera, "like a mouse input". `TCTouchpad` with `reportsRelativeValues` "moves exactly as far as their finger moves … no over-rotation" ([WWDC26-358, ~17:00](https://developer.apple.com/videos/play/wwdc2026/358/)) [P].
- Feral offers **Aim Input**: "Trackpad: Simulates a trackpad or mouse pointer. Players aim by dragging anywhere on the right-hand side of the screen" versus "Joystick: … an on-screen joystick" ([Feral FAQ](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)) [P].
- HCI basis for gain: Casiez et al. 2008 found that **low control-display gain hurt performance**, high gain increased overshoot, and gain mattered little "until human limits of speed and accuracy are approached". Their usable-gain floor is `CDmin = Dmax / ORmax`, where OR is the operating range of the limb ([doi](https://doi.org/10.1080/07370020802278163)) [P].
  - Applied to touch (**derived, UNVERIFIED**): a 180° turn should fit in one comfortable thumb swipe of about 40–50 mm inside the right half (about 60 mm usable on an iPhone 17 Pro after the 10 mm island inset). That gives a default of **≈3.5–4.5°/mm**, with a slider of ×0.25–×4.
- Acceleration: GyroWiki argues that acceleration should be optional and transparent, with "slow/fast sensitivities with … real-world velocity thresholds" ([GyroWiki](http://gyrowiki.jibbsmart.com/blog:good-gyro-controls-part-1:the-gyro-is-a-mouse)) [P]. **Recommendation (UNVERIFIED): the default is linear 1:1 deltas (touchpad), with acceleration as an opt-in.**
- Accessibility floor: separate horizontal and vertical look sensitivity and the option to disable automatic camera movement ([XAG 117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)). Sensitivity must be adjustable "by at least 50% of the default" ([XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)) [P].

### 4.4 Gyro aiming
- GyroWiki (Jibb Smart, 2019) [P] ([link](http://gyrowiki.jibbsmart.com/blog:good-gyro-controls-part-1:the-gyro-is-a-mouse)):
  - The gyro "is a mouse": map real rotation 1:1 (turn the device 37.5°, the camera turns 37.5° at sensitivity 1).
  - Let sensitivity go high.
  - "disable the gyro while a button is held" so the player can re-centre the device ("ratcheting").
  - A small **tightening threshold** scales tiny inputs toward zero.
  - **Tiered smoothing** applies only to small movements.
  - GCAP22 talk: [link](http://gyrowiki.jibbsmart.com/blog:mouse-like-precision-no-aim-assist-gcap22-presentation).
- **"Gyro on touch"** (gyro active only while a thumb rests on the look area or a stick) is the Steam Deck pattern of activating gyro on right-stick capacitive touch ([GamingOnLinux, S](https://www.gamingonlinux.com/2023/09/valve-overhauled-steam-deck-as-mouse-gyro-option-in-new-beta/)). The mobile equivalent is gyro active only while the right thumb is down. It ratchets naturally: lift the thumb, re-centre the device. **No primary mobile-game source was found for this exact pattern: UNVERIFIED as an industry convention.**
- Mobile precedents: CoD Mobile gyroscope option, where "all aiming and look controls will be defined by how you move your phone" ([Activision blog](https://blog.activision.com/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls)) [P]. PUBG Mobile "Scope On" vs "Always On" ([Android Central, S](https://www.androidcentral.com/how-use-gyroscope-controls-aiming-pubg-mobile)).
- Cautions:
  - *Tilt* (position-mapped) orientation impaired navigation in Teather 2017. Gyro *rate* aiming is a different mapping, but treat it as opt-in [P].
  - XAG 107 lists "Gyroscopic controls such as tilting or shaking" among input types that need alternatives.
  - Google's Level Up treats gyro-aiming games as a special input class (LU-IC-EAD exemption) ([Level Up](https://developer.android.com/games/guidelines)).
  - For a horror game, gyro look also moves the camera when the player shifts in their seat, so it must be **off by default** (**derived**).
- Unity: sensors are **disabled by default**; call `InputSystem.EnableDevice(Gyroscope.current)` and set `samplingFrequency` ([Unity Sensors](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Sensors.html)). The Device Simulator does not simulate gyroscope rotation ([Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/device-simulator-introduction.html)). Unity Remote does stream gyro and attitude data (Sensors page).

### 4.5 Sprint: stick threshold, lock, auto
- Apple: build sprint into the stick. "A small tilt means … normal pace. If it's a big tilt, the character will sprint." Add a **halo on the stick ring while sprinting** ([WWDC26-358](https://developer.apple.com/videos/play/wwdc2026/358/), [WWDC24-10085](https://developer.apple.com/videos/play/wwdc2024/10085/)) [P].
- CoD Mobile: **"Joystick Sprint"**, "sprint forward by keeping the on-screen Joystick in the forward position", and **"Auto-Run"** ([Activision](https://blog.activision.com/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls)) [P]. Its "Always sprint" option is also used as an accessibility example in [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107).
- PUBG Mobile: pushing the stick far up shows a sprint spot, and sliding onto it **locks** sprint ([PocketGamer, S](https://www.pocketgamer.com/pubg-mobile/pubg-mobile-cheats-and-tips-everything-you-need-to-adjust-to-mobile-pubg/), **UNVERIFIED**, not read directly).
- Accessibility: provide toggle or auto alternatives to holds; "Can u thank the person who decided to put auto sprint in Titanfall?" ([GAG — holds](https://gameaccessibilityguidelines.com/avoid-provide-alternatives-to-requiring-buttons-to-be-held-down/)); XAG 107 "Toggles and 'auto' holds" [P].
- Feral adds "Virtual Controller Vibration: … certain touch inputs, for example sprinting, will cause vibrations" ([Feral FAQ](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)) [P].

### 4.6 Contextual interact buttons and tapping the world directly
- HIG: show and hide controls with context, and prefer tapping in-world objects over a selection button ([HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls)). WWDC26: the pick-up button appears **next to the item**, and is removed when it doesn't apply ([358, 12:17–13:49](https://developer.apple.com/videos/play/wwdc2026/358/)) [P].
- Use glyphs for the action ("a hand … to pick up an object"), not "A/X" ([HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls)) [P].
- Diablo Immortal's "auto pick up" removes the pick-up control entirely ([XAG 107 example](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)) [P].
- Fortnite mobile lets players choose how to fire: "Auto fire, tapping anywhere on the screen, or tapping on a dedicated fire button" (same XAG page) [P].

### 4.7 Hold-to-progress (FrontRooms break glass)
- XAG 107: "Avoid introducing mechanics where a key or button should be held down for an extended period". Its scoping question flags holds of "2-3 seconds or more". Acceptable alternatives to complex gestures include a "long press (less than 3 seconds)". Example: The Long Dark's "accessible interactions … converts all press and hold actions into press actions" ([XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)) [P].
- Gears 5 offers "press & hold" or "quick taps" for tap challenges (XAG 107). Alien: Isolation's "Rapid Tap Assist" changes rapid tapping to tap-and-hold ([Feral](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)) [P].
- CoD Mobile's ADS trigger can be set to "tap and hold", "tap", "hybrid" or "double tap" ([XAG 107 example](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)) [P].
- HIG: use touch-and-hold for *variations* of an action, and draw a press state that is visible beyond the finger ([HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls)) [P].
- No primary source specifies a progress-ring design. Drawing the ring around the button *outside* the thumb's footprint follows the HIG's "glow outside the bounds" logic (**derived**).

### 4.8 Opacity and fade when idle
- HIG figure pair: the thumbstick is "less visible while the character is at rest" and "more visible while … moving" ([HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls)). TouchController exposes `hidesWhenNotPressed` and `highlightDuration` ([TCThumbstick](https://developer.apple.com/documentation/touchcontroller/tcthumbstick)) [P].
- Layout editors expose per-control opacity: CoD Mobile ("change the size and opacity of everything on-screen", [Activision](https://blog.activision.com/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls)) and Alien: Isolation ("adjust the opacity of the selected input icon", [Feral](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)) [P].
- GAG: option to hide non-interactive elements ([GAG](https://gameaccessibilityguidelines.com/provide-an-option-to-turn-off-hide-all-non-interactive-elements/)) [P].

### 4.9 Multitouch: finger ownership, and a look finger that starts on a button
- Apple's hold-and-drag: with button B held, "capture the raw touch delta while button B is held. This has to happen in touchesMoved because it's tracked independently from the button's pressed state" ([WWDC26-358, ~20:35](https://developer.apple.com/videos/play/wwdc2026/358/)) [P]. The rule is that **a finger that lands on a button may also drive look, and keeps ownership by touch ID until it lifts.**
- TouchController passes a per-touch `index` to `handleTouchBegan/Moved/Ended` ([TCTouchController](https://developer.apple.com/documentation/touchcontroller/tctouchcontroller)) [P].
- Unity specifics [P]:
  - `InputSystemUIInputModule` tracks pointers separately, "required to allow control of multiple On-Screen controls at the same time with different fingers" ([Input System changelog](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/changelog/CHANGELOG.html)).
  - `Touchscreen` supports a maximum of **10** simultaneous fingers (changelog, 1.4.0).
  - Each touch has `touchId`, `startPosition`, `delta`, `phase`, `tapCount`. `EnhancedTouch.Touch.activeTouches` and `activeFingers` give garbage-free per-finger tracking ([Unity Touch](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Touch.html)).
  - Don't poll `Touchscreen` in Update: "your app will miss changes in touch state" (same page).
  - Input queued by the UI from on-screen controls lags **one frame** behind direct input (changelog, 1.1.0-pre).
- Pattern (**derived**): a look region implemented with `<Touchscreen>/touch*/delta` will *also* see the stick finger's movement. Filter by `touchId` ownership, and claim ownership at touch-began by region.

### 4.10 HUD layout editors and presets
- **Alien: Isolation (Feral)** [P]:
  - Five custom control schemes.
  - Tap an input to select it (highlighted green), drag to move, **pinch to resize**, fine-tune position and size, opacity, delete, add.
  - Save to a preset; "reset … to the left- or right-hand default".
  - Editable from the main or pause menu ([Feral FAQ](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)).
- **CoD Mobile:** a mock HUD where you "drag, drop, and change the size and opacity of everything" ([Activision](https://blog.activision.com/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls)) [P].
- **Fortnite:** three preset HUD layouts ("Old School", "Builder Pro", "Combat Pro") ([XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)). UEFN can override touch layouts ([Epic](https://dev.epicgames.com/documentation/en-us/fortnite/developer-customizable-touchscreen-controls-in-fortnite)) [P].
- **XAG 107:** let players "adjust the size, spacing, and positioning of all touch targets", "Provide a range of pre-set … layout configurations", and "Ensure the customization process itself is fully accessible" [P].

### 4.11 Aim assist and interaction magnetism (for a door at about 2 m)
- Alien: Isolation **Aim Assist**: "Auto-Aim: Reticule locks onto nearby targets automatically", "On: Reticule slows down when nearing or over targets", "Off" ([Feral FAQ](https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs)) [P].
- HCI foundations [P]:
  - **Semantic pointing** lowers control-display gain over targets, so they behave as if larger in motor space ([Blanch, Guiard & Beaudouin-Lafon, CHI 2004](https://doi.org/10.1145/985692.985758)). This is the "slow-down" / sticky crosshair.
  - **Bubble cursor** selects the nearest target ([Grossman & Balakrishnan, CHI 2005](https://doi.org/10.1145/1054972.1055012)). This corresponds to a "best target in a cone" interaction query.
- GAG lists assist modes such as auto-aim as an accessibility option (General, Intermediate) ([GAG](https://gameaccessibilityguidelines.com/include-assist-modes-such-as-auto-aim-and-assisted-steering/)) [P].

### 4.12 Cancel and error recovery
- XAG 107 [P]:
  - Activate on **touch-end**, and let a press be cancelled by sliding off.
  - It allows down-event activation "if it's essential" (shooting, piano).
  - Diablo Immortal: "Slide Out of Range / Slide to Cancel Icon".
  - Surround each target with **inactive space** ([XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)).
- Wild Rift: tap-cast versus drag-aim prioritisation was reworked per champion, and there is an "action cancel" zone ([Riot dev blog — Irelia](https://wildrift.leagueoflegends.com/en-gb/news/dev/dev-irelia-dances-her-way-to-wild-rift)) [P]; cancel-method details come from a player guide ([GINX, S](https://www.ginx.tv/en/wild-rift-best-controller-and-ui-settings)).

### 4.13 GDC and industry talks (for the bibliography)
- Zach Gage, *Controls You Can Feel: Putting Tactility Back Into Touch Controls*, GDC 2012 Smartphone & Tablet Summit ([GDC Vault](https://gdcvault.com/play/1015663/Controls-You-Can-Feel-Putting)).
- Antti Summala (Supercell), *Designing Two Tasty Cores Three Times Over: The Case of 'Brawl Stars'*, GDC 2019. The team "tried to decide between two control models but ended up with a third" ([GDC Vault](https://gdcvault.com/play/1025751/)).
- *Controlling Carnage: Effective Side-Scroller Controls on Mobile* ([GDC Vault](https://gdcvault.com/play/1021883/Controlling-Carnage-Effective-Side-Scroller)); speaker and year **UNVERIFIED**.
- Feral Interactive on Alien: Isolation mobile (Dec 2021): "lots of slow and cautious movements interspersed with very rapid actions" ([interview](https://www.gfinityesports.com/alien-isolation-mobile/interview/)). This is the closest tone match to FrontRooms.

---

## 5. Accessibility

### 5.1 Game Accessibility Guidelines (gameaccessibilityguidelines.com) — touch-relevant items [P]
| Item | Level | Touch note |
|---|---|---|
| [Ensure interactive elements / virtual controls are large and well spaced](https://gameaccessibilityguidelines.com/ensure-interactive-elements-virtual-controls-are-large-and-well-spaced-particularly-on-small-or-touch-screens/) | Motor **Basic**, Vision Basic | "2.4cm … ideal … 0.96cm is the recommendation for phones"; treat it as a minimum; close spacing requires larger targets |
| [Allow controls to be remapped / reconfigured](https://gameaccessibilityguidelines.com/allow-controls-to-be-remapped-reconfigured/) | Motor Basic | Includes action remapping |
| [Include an option to adjust the sensitivity of controls](https://gameaccessibilityguidelines.com/include-an-option-to-adjust-the-sensitivity-of-controls/) | Motor Basic | "mouse, touch, tilt, analogue stick" |
| [Include toggle/slider for any haptics](https://gameaccessibilityguidelines.com/include-toggle-slider-for-any-haptics/) | Motor Basic, Cognitive Intermediate | Separate controls per haptic *type* (proximity cue vs atmospherics) |
| [Avoid / provide alternatives to requiring buttons to be held down](https://gameaccessibilityguidelines.com/avoid-provide-alternatives-to-requiring-buttons-to-be-held-down/) | Motor Intermediate | Toggle or auto sprint |
| [Allow interfaces to be rearranged](https://gameaccessibilityguidelines.com/allow-interfaces-to-be-rearranged/) | Motor Intermediate | Dynamic stick counts; Into the Dead 25 % × 4 layouts |
| [Allow interfaces to be resized](https://gameaccessibilityguidelines.com/allow-interfaces-to-be-resized/) | Motor/Vision Intermediate | — |
| [Avoid repeated inputs (button-mashing/QTEs)](https://gameaccessibilityguidelines.com/avoid-repeated-inputs-button-mashing-quick-time-events/) | Motor Intermediate | Offer hold instead of mash |
| [Ensure multiple simultaneous actions (click/drag or swipe) are not required](https://gameaccessibilityguidelines.com/ensure-that-multiple-simultaneous-actions-eg-click-drag-or-swipe-are-not-required-and-included-only-as-a-supplementary-alternative-input-method/) | Motor Intermediate | Drag-look needs an alternative (e.g. snap-turn buttons) |
| [Ensure all key actions can be done with digital controls](https://gameaccessibilityguidelines.com/ensure-that-all-key-actions-can-be-carried-out-by-digital-controls-pad-keys-presses-with-more-complex-input-eg-analogue-speech-gesture-not-required-and-included-only-as-supplementary-al/) | Motor Intermediate | — |
| [Make accuracy-requiring elements stationary](https://gameaccessibilityguidelines.com/make-interactive-elements-that-require-accuracy-eg-cursor-touch-controlled-menu-options-stationary/) | Motor Intermediate | No HUD sway on buttons |
| [Support more than one input device](https://gameaccessibilityguidelines.com/support-more-than-one-input-device/) | Motor Intermediate | Controller on phone/tablet |
| [Include assist modes such as auto-aim](https://gameaccessibilityguidelines.com/include-assist-modes-such-as-auto-aim-and-assisted-steering/) | General Intermediate | Interaction magnetism |
| [Provide very simple control schemes compatible with switch / eye tracking](https://gameaccessibilityguidelines.com/provide-very-simple-control-schemes-that-are-compatible-with-assistive-technology-devices-such-as-switch-or-eye-tracking/) | Motor Advanced | — |
| [Allow play in both landscape and portrait](https://gameaccessibilityguidelines.com/allow-play-in-both-landscape-and-portrait/) | listed | Conflicts with landscape-only; note as stretch |

### 5.2 Xbox Accessibility Guidelines [P]
- **XAG 107 Input**, with a dedicated "Guidelines for mobile input" section ([link](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107)):
  - Support non-touch input and the platform's own accessibility features (Switch Control, Voice Control).
  - Support portrait and landscape.
  - Adjustable size, spacing and position; presets.
  - **Minimum default sizes: phones 15 mm (59 px @100 DPI, 118 @200, 236 @400); tablets 24 mm (94 / 189 / 378 px)**.
  - Space targets apart, with inactive space around each; allow cancel or undo.
  - Adjustable swipe sensitivity; simplified schemes.
  - Alternatives for prolonged holds, drags, rapid taps, multi-finger input and gyro.
  - Activate on touch-end.
  - The customisation UI itself must be accessible.
  - General: sensitivity range at least ±50 %; no mandatory simultaneous presses; single-stick play possible.
- **XAG 110 Haptic feedback**: on/off and strength, and never haptics as the sole channel ([link](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/110)).
- **XAG 117 Visual distractions and motion settings**: camera shake, head-bob and blur off; adjustable FOV; separate horizontal/vertical sensitivity ([link](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117)).
- XAG 118 covers photosensitivity ([link](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/118)).
- XAG 115 covers error correction and destructive actions ([link](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/115)), which is relevant to R = restart.
- (XAG 108 is *game difficulty options*, not input: [link](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/108).)

### 5.3 Apple accessibility for games [P]
- **AssistiveTouch**: "use multi-finger gestures … or replace pressing buttons with just a tap". You can record custom gestures, and if multi-finger gestures are impossible, "record individual movements, and they group together" ([Apple Support](https://support.apple.com/en-us/111794)). **Implication:** a fixed, predictable layout helps recorded gestures; a stick that moves every time works against them. Keep a "Fixed stick" option.
- **Switch Control**: "the same switch or sound actions you use to navigate … can be turned into a game controller" ([apple.com/accessibility/mobility](https://www.apple.com/accessibility/mobility/)). Apple has a WWDC22 coding challenge on Switch Control in games ([doc](https://developer.apple.com/documentation/accessibility/wwdc22_challenge_learn_switch_control_through_gaming)). **Implication:** support `GCController` / Input System gamepad fully so switch-as-controller works.
- **Apple.Accessibility Unity plug-in**: `AccessibilityNode` and traits for VoiceOver; `AccessibilitySettings.IsReduceMotionEnabled`; Dynamic Type multiplier; `onIsSwitchControlRunningChanged` ([doc](https://github.com/apple/unityplugins/blob/main/plug-ins/Apple.Accessibility/Apple.Accessibility_Unity/Assets/Apple.Accessibility/Documentation~/Apple.Accessibility.md)). Use Reduce Motion to set the default for FrontRooms' *camera motion %*.

### 5.4 Android accessibility services [P/UNVERIFIED]
- Android accessibility guidance: 48 dp targets and content descriptions ([Android](https://developer.android.com/guide/topics/ui/accessibility/apps)).
- Google's Level Up requirement that games be *fully* playable with controller and with keyboard/mouse (LU-IC-GAA) means a touch-free path exists by design ([Level Up](https://developer.android.com/games/guidelines)).
- How TalkBack and Switch Access behave with a Unity GameActivity surface for gameplay touch was not researched: **UNVERIFIED**. Expect TalkBack to intercept touches unless the game handles explore-by-touch.

---

## 6. Unity facts (6000.3 LTS + Input System 1.18)

### 6.1 Platform minimums and build requirements [P]
- **iOS:** Unity 6.3 "supports iOS 15 and above"; Xcode 16+ recommended; Metal only ([Unity iOS](https://docs.unity3d.com/6000.3/Documentation/Manual/ios-requirements-and-compatibility.html)). The system requirements page lists iOS/iPadOS 15+ and an A8 SoC+ ([Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html)). Project: `iOSTargetOSVersionString: 15.0`.
- **Android:** "Unity supports Android 7.1 (API level 25) and above … You can now target Android API levels 35 and 36" ([Unity Android](https://docs.unity3d.com/6000.3/Documentation/Manual/android-requirements-and-compatibility.html)). The default development toolchain is **SDK API 35**, NDK r27c and OpenJDK 17 ([Unity system requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html)), so **API 36 must be installed** to meet Play's API 36 rule. Project: `AndroidMinSdkVersion: 25`, `AndroidTargetSdkVersion: 0` ("highest installed").
- "Unity doesn't support Android emulators. … use Unity Remote" or the Device Simulator for appearance only (same page).
- **Active Input Handling:** "Both: Use both systems. **This option isn't supported on Android.**" ([Unity Android Player settings](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html)). Project: `activeInputHandler: 2` (Both), with about 22 legacy `Input.*` calls in `Assets/Scripts`. This setting is project-wide, not per-platform. **Decision needed** (see §9 R1).
- **GameActivity** is the default entry point in new projects. "Do not use GameActivity library versions earlier than 4.4.x … These versions cause critical issues, including … ANR … update your Unity Editor to **6000.3.13f1 or later**." Under GameActivity the player loop runs on a native thread, so Java `myLooper`-based plug-in calls fail ([Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/android-application-entries-game-activity-requirements.html)). Project: `androidApplicationEntry: 2` (assumed GameActivity), editor 6000.3.10f1.
- **IL2CPP + ARM64** are required for Play ([Android](https://developer.android.com/google/play/requirements/64-bit)). The project is already set this way.

### 6.2 Input System on-screen controls [P]
Sources: [OnScreen manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/OnScreen.html), [OnScreenStick API](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/api/UnityEngine.InputSystem.OnScreen.OnScreenStick.html)
- On-screen controls "don't have a predefined visual representation". Each one writes to a **control path on a virtual device**, for example `<Gamepad>/leftStick`, and the Input System creates that device when the component is enabled.
- **Inference (UNVERIFIED but structural):** the legacy `Input.GetAxis` / `GetKey` path will *not* see these virtual devices. Gameplay must read movement and look through Input System actions, or through a custom touch layer, for touch to work.
- `OnScreenButton` sets its control to 1 on `OnPointerDown` and 0 on `OnPointerUp`. Both components need a UI hierarchy with an EventSystem (the editor warns when they're not part of a valid UI hierarchy, changelog 1.9.0).
- `OnScreenStick` properties: `behaviour` (the three modes in §4.1), `movementRange` (in pixels), `dynamicOriginRange` (radius), and **`useIsolatedInputActions`**. The isolated mode exists because with PlayerInput auto-switching, the stick "will jump back to center, as though it had been released" or jitter. Isolated mode uses private actions bound by default to `<Touchscreen>/touch*/press` and `/position`.

### 6.3 Touch APIs [P]
([Unity Touch](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Touch.html))
- `Touchscreen`: `primaryTouch` plus a fixed-size `touches` array. Each touch has `position`, `delta`, `startPosition`, `startTime`, `press`, `pressure`, `radius`, `touchId`, `phase`, `tap` and `tapCount`.
- Bind multi-touch actions with `<Touchscreen>/touch*/press`, and use a **pass-through** action type so you get per-touch callbacks.
- `EnhancedTouch` must be enabled with `EnhancedTouchSupport.Enable()`. It gives `Touch.activeTouches` and `Touch.activeFingers`, and allocates no GC garbage.
- `TouchSimulation.Enable()` mirrors mouse or pen input as touch in the editor.
- `Screen.safeArea` is in pixels with a bottom-left origin ([doc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-safeArea.html)). `Screen.cutouts` is a `Rect[]` in pixels ([doc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-cutouts.html)).
- `Screen.dpi` on **Android returns the `densityDpi` bucket**, not the true DPI; average `xdpi` and `ydpi` for precision ([doc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen-dpi.html)).
- CanvasScaler has a **Constant Physical Size** mode (units: cm, mm, in, points, picas; *Fallback Screen DPI*) and Scale-With-Screen-Size match modes Match / **Expand** / Shrink ([uGUI](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-CanvasScaler.html)). Note that **CanvasScaler "points" are 1/72 inch (0.353 mm), not Apple points** (0.166 mm on an iPhone Pro); that equivalence is **derived**.

### 6.4 Known Input System issues relevant to on-screen controls and multitouch
| ID | Issue | Status for 1.18.0 |
|---|---|---|
| ISXB-813 | Multiple `OnScreenStick`s don't work together when used at the same time (isolated mode) | Fixed in **1.12.0** ([changelog](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/changelog/CHANGELOG.html)) |
| ISXB-1006 / ISXB-845 | `pointerId` reused when one finger releases and another presses in the same frame | Fixed in **1.12.0** (changelog) |
| ISXB-656 | Unexpected control-scheme switch with OnScreenControl and pointer schemes | Fixed in **1.12.0** (changelog) |
| ISXB-48 | OnScreenStick unusable with PlayerInput auto-switch | Fixed in **1.5.0** (changelog; the same release added the `behaviour` property); still use isolated mode |
| **ISXB-1027** | `ExactPositionWithDynamicOrigin` behaves differently with isolated actions on vs off (dot doesn't move to the click position) | Not in the 1.18–1.20 changelogs → **treat as open (UNVERIFIED)** ([tracker](https://issuetracker.unity.com/issues/7294)) |
| **ISXB-1118** | Release one touch and press a second in the same frame → the second touch produces no `OnPointerDown`; seen mostly on low-end Android below 30 fps | Not in the changelogs → **treat as open (UNVERIFIED)** ([tracker](https://issuetracker.unity.com/issues/14969)) |
| UUM-100125 | UI Toolkit `ClickEvent` fires after Android rotation (stale touch replay) | Fixed in **1.20.0** (2026-07-21) ([1.20 changelog](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/changelog/CHANGELOG.html)) |
| UUM-137930 | `onAnyButtonPress` doesn't fire on touch | Fixed in **1.20.0** |
| — | Input queued from UI-driven on-screen controls runs one frame late | Design note (changelog 1.1.0-pre) |
| — | More than 10 fingers → error | Fixed in 1.4.0; the cap is 10 |

### 6.5 Test tooling [P]
- Device Simulator: simulates safe area, rotation and touch, but "**doesn't support multitouch**" and does not simulate the gyroscope or performance ([Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/device-simulator-introduction.html)).
- Unity Remote streams touch and sensors ([Sensors](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Sensors.html)).

### 6.6 Vibration and haptics [P]
- `Handheld.Vibrate()`: "Duration of vibration is determined by the operating system … use platform specific libraries" for anything advanced ([Unity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Handheld.Vibrate.html)). On Android this is the legacy, buzzy kind of vibration that Google recommends against (§2.8; mapping **UNVERIFIED**).
- Use **Apple.CoreHaptics** ([apple/unityplugins](https://github.com/apple/unityplugins)) on iOS. On Android, call `View.performHapticFeedback` or `VibrationEffect` through JNI (`AndroidJavaObject`); exact bridging under GameActivity is **UNVERIFIED**.

### 6.7 Frame rate and performance [P]
- On iOS and Android with `vSyncCount = 0` and `targetFrameRate = -1`, "content is rendered at a fixed **30 fps**". Unsupported rates round down to a divisor of the refresh rate. **ProMotion is off by default → 60 Hz**; with it on, 120 Hz on supported displays ([Unity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html)).
- Android *Optimized Frame Pacing* (Swappy) is on in the project (`androidUseSwappy: 1`) ([Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html)).
- **Adaptive Performance** in 6.3: an Android provider ([doc](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.adaptiveperformance.google.android.html)) and a **Basic provider** that uses FrameTimingManager on Android, iOS, macOS and more, but has no thermal API ([doc](https://docs.unity3d.com/6000.3/Documentation/Manual/adaptive-performance/basic-provider.html)). The Apple provider is documented only from Unity 6.5 ([6000.5 doc](https://docs.unity3d.com/6000.5/Documentation/Manual/com.unity.adaptiveperformance.apple.html); the 6000.3 page returns 404).

---

## 7. Numbers to design with

### 7.1 Conversion math (show your work)
- **Apple pt → mm:** `mm/pt = 25.4 / ppi × scale`. iPhone 17/18 Pro (460 ppi, @3x): 25.4/460×3 = **0.1657 mm/pt**. iPad (264 ppi, @2x): **0.1924 mm/pt**.
- **Android dp → mm:** `mm/dp = 25.4 / ppi × density`. Pixel 8: ppi = √(1080² + 2400²) / 6.2 in = 424.5; 25.4/424.5 × 2.625 = **0.1571 mm/dp** (nominal 25.4/160 = 0.1588).
- **Screen → FrontRooms HUD canvas units (cu)** with CanvasScaler ScaleWithScreenSize, ref 1920×1080, match 0.5: `scaleFactor = 2^((log2(W/1920) + log2(H/1080)) / 2) = √((W/1920)·(H/1080))`. Then `cu/px = 1/scaleFactor`, `cu/pt = scale/scaleFactor`, `cu/mm = (ppi/25.4)/scaleFactor`.
  - iPhone 17 Pro (2622×1206 px): √(1.3656 × 1.1167) = 1.2349 → canvas **2123×977 cu**; **2.429 cu/pt**; **14.67 cu/mm**.
  - Pixel 8 (2400×1080): 1.1180 → canvas 2147×966 cu; 2.348 cu/dp; 14.95 cu/mm.
  - iPad 11" (2360×1640): 1.3662 → canvas **1727×1200 cu**; 1.464 cu/pt; 7.61 cu/mm.
  - iPad Pro 13" (2752×2064): 1.6551 → canvas 1663×1247 cu; 1.208 cu/pt; 6.28 cu/mm.
  - With **Expand** instead (`scale = min(W/1920, H/1080)`): iPhone 17 Pro → 2348×1080 cu, 2.687 cu/pt. iPad 11" → 1920×1334 cu. The 1920×1080 reference rectangle then always fits.

### 7.2 Table (phone = iPhone 17 Pro unless noted; tablet = iPad 11")
| Quantity | Source value | Phone mm | Phone cu (match .5) | Tablet mm / cu | Source |
|---|---|---|---|---|---|
| Apple frequent-control minimum | 44×44 pt | 7.29 | **107** | 8.47 / 64 | [HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls) |
| Apple menu / secondary minimum | 28×28 pt | 4.64 | 68 | 5.39 / 41 | same |
| Android minimum | 48×48 dp + 8 dp gap | 7.54 (Pixel 8); gap 1.26 | 113 (Pixel 8); gap 19 | — | [Android](https://developer.android.com/guide/topics/ui/accessibility/apps) |
| Hoober centre / corner (95 % of taps) | 7 mm / 12 mm | 7 / 12 | 103 / **176** | 7 / 53 ; 12 / 91 | [UXmatters](https://www.uxmatters.com/mt/archives/2017/03/design-for-fingers-touch-and-people-part-1.php) |
| GAG phone minimum (ideal) | 0.96 cm (2.4 cm) | 9.6 (24) | 141 (352) | — | [GAG](https://gameaccessibilityguidelines.com/ensure-interactive-elements-virtual-controls-are-large-and-well-spaced-particularly-on-small-or-touch-screens/) |
| **XAG suggested default minimum** | phone 15 mm / tablet 24 mm | 15 = 90.6 pt | **220** | 24 = 124.7 pt / **183** | [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107) |
| iOS text default / minimum | 17 pt / 11 pt | 2.8 / 1.8 | 41 / 27 | 3.3 / 2.1 ; 25 / 16 | [HIG](https://developer.apple.com/design/human-interface-guidelines/designing-for-games) |
| iPhone landscape side inset (Dynamic Island) | 62 pt | 10.27 | **151** (vs HUD margin 72) | — | [Use Your Loaf, S](https://useyourloaf.com/blog/iphone-17-screen-sizes/) |
| iPhone landscape bottom inset (Home indicator) | 20 pt (iOS 26) / 21 pt (iOS 18) | 3.3 | 49 | iPad ~20 pt, **UNVERIFIED** | same / [iPhone 16](https://useyourloaf.com/blog/iphone-16-screen-sizes/) |
| iPhone Air side / bottom inset | 68 / 29 pt | 11.3 / 4.8 | 158 / 67 (Air canvas) | — | [Use Your Loaf, S](https://useyourloaf.com/blog/iphone-17-screen-sizes/) |
| Android gesture-exclusion cap | 200 dp vertical per edge; lifted while the nav bar is stickily hidden | 31.4 (Pixel 8) | 470 (Pixel 8) | — | [View ref](https://developer.android.com/reference/android/view/View#setSystemGestureExclusionRects(java.util.List%3Candroid.graphics.Rect%3E)) |
| Home / bottom gesture | never excludable (Android); deferrable to a 2nd swipe (iOS) | — | — | — | [gesture nav](https://developer.android.com/develop/ui/views/touch-and-input/gestures/gesturenav), [HIG](https://developer.apple.com/design/human-interface-guidelines/going-full-screen) |
| FrontRooms HUD margin | 72 cu | **4.9 mm = 29.6 pt** | 72 | 9.5 mm = 49 pt | §7.1 |
| FrontRooms rhythm | 24 cu | 1.6 mm | 24 | 3.2 mm | §7.1 |
| Stick dead zone (physical sticks) | 0.25 OUYA / ~0.1 X360 (scaled radial) | — | — | — | [Sutphin](https://www.gamedeveloper.com/disciplines/doing-thumbstick-dead-zones-right) |
| Stick dead zone (virtual, proposal) | 0.08–0.12 × radius, scaled radial | ~1–1.5 mm | 15–22 | — | **UNVERIFIED** (derived) |
| Sprint threshold (proposal) | ≥0.85 deflection held ≥150 ms → sprint; release below 0.7 → walk (hysteresis) | — | — | — | Apple says only "big tilt"; numbers **UNVERIFIED** |
| Look gain (proposal) | ≈3.5–4.5 °/mm default; slider ×0.25–×4 | 180° per 40–50 mm swipe | — | — | derived from [Casiez 2008](https://doi.org/10.1080/07370020802278163); **UNVERIFIED** |
| Sensitivity range | ≥ ±50 % of default | — | — | — | [XAG 107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107) |
| Hold duration ceiling | avoid holds ≥2–3 s; "long press (less than 3 seconds)" acceptable | — | — | — | XAG 107 |
| Android haptic tuning | gap ≥50 ms; scale steps ×1.4; 0.5 / 0.7 / 1.0 | — | — | — | [Android](https://developer.android.com/develop/ui/views/haptics/custom-haptic-effects) |
| Simultaneous touches (Unity) | max 10 | — | — | — | [changelog](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/changelog/CHANGELOG.html) |
| Unity mobile default frame rate | 30 fps (vSync 0, target −1) | — | — | — | [Unity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html) |
| Level Up performance bar | avg ≥55, P90 ≥50, P99 ≥30 fps | — | — | tablets exempt | [Level Up](https://developer.android.com/games/guidelines) |
| Landscape aspect ratios to support | 4:3, 16:10, 21:9 (Level Up); 16:10, 19.5:9, 4:3 (Apple) | — | — | — | [Level Up](https://developer.android.com/games/guidelines), [HIG](https://developer.apple.com/design/human-interface-guidelines/designing-for-games) |

---

## 8. Media candidates (URLs only — nothing downloaded)

| # | What it shows | Where | Notes |
|---|---|---|---|
| M1 | Ideal placement heat map for touch controls, iPhone in landscape | HIG Game controls, asset `game-controls-touch-input-heat-map@2x.png` — https://developer.apple.com/design/human-interface-guidelines/game-controls | Best single image for the "where thumbs go" slide |
| M2 | Movement zone on the left, camera zone on the right | same page, `game-controls-camera-thumbstick-zones@2x.png` | Matches the FrontRooms split |
| M3 | Thumb pressing a virtual button; glow and opacity press state visible around the thumb | same page, `game-controls-press-state@2x.png` (and `~dark`) | Press-state rule |
| M4 | Thumbstick at rest (faint) vs in motion (visible) | same page, `game-controls-thumbstick-at-rest@2x.png` / `…-in-motion@2x.png` | Idle-fade pattern |
| M5 | One button with tap and touch-and-hold variants | same page, `game-controls-power-up-action@2x.png` | Hold-to-break-glass analogue |
| M6 | Before/after: a 1:1 controller port cluttering the screen vs the redesigned touch scheme | WWDC26-358 at ~10:05 and ~22:44 — https://developer.apple.com/videos/play/wwdc2026/358/?time=605 and `?time=1364` | Video frame reference |
| M7 | Sprint halo on the stick ring | WWDC26-358 ~21:38 — https://developer.apple.com/videos/play/wwdc2026/358/?time=1298 | Feedback for loud sprint |
| M8 | Right-half touchpad camera, no over-rotation | WWDC26-358 ~17:00–18:02 — `?time=1020` | Look-drag argument |
| M9 | Anchoring components to edges across iPad Pro and iPad mini | Meet with Apple 243 ~2:15 — https://developer.apple.com/videos/play/meet-with-apple/243/?time=135 | Phone/tablet layout argument |
| M10 | Placement regions: avoid centre and stick/camera zones, menus on top | WWDC24-10085 ~19:23 — https://developer.apple.com/videos/play/wwdc2024/10085/?time=1163 | — |
| M11 | Thumb-reach colour zones (green / yellow / red) for one-handed, cradled and two-handed grips | Hoober 2013 — https://www.uxmatters.com/mt/archives/2013/02/how-do-users-really-hold-mobile-devices.php | Original-publication thumb diagrams |
| M12 | Touch accuracy by screen region (Figures 8 and 9) | Hoober 2017 Pt 1 — https://www.uxmatters.com/mt/archives/2017/03/design-for-fingers-touch-and-people-part-1.php | 7 mm centre vs 12 mm corners |
| M13 | "Inactive space" halos around joystick and buttons (fictional RPG) | XAG 107 image `inactive-space.png` — https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107 | Spacing rule |
| M14 | CoD Mobile custom layout editor with scale and opacity sliders | XAG 107 image `cod-customize-controls.png` (same page); video https://www.youtube.com/watch?v=Wdgqe4Ja_WU | Layout editor precedent |
| M15 | Fortnite mobile HUD presets (Old School / Builder Pro / Combat Pro) | XAG 107 image `fortnite-presets.jpg` (same page) | Preset precedent |
| M16 | Diablo Immortal skill-cancel options | XAG 107 image `diablo-skill-cancel.png` (same page) | Cancel pattern |
| M17 | Dual virtual joysticks in the study app (Fig. 4) and GTA3 mobile virtual controls (Fig. 2) | Teather et al. 2017 PDF — https://www.csit.carleton.ca/~rteather/pdfs/ec2017.pdf | Academic figure |
| M18 | Axial vs radial vs scaled-radial dead-zone visualisations | Sutphin — https://www.gamedeveloper.com/disciplines/doing-thumbstick-dead-zones-right | Dead-zone slide |
| M19 | Alien: Isolation mobile control customisation screen (green-highlighted input, pinch to resize) | Feral FAQ text — https://support.feralinteractive.com/docs/en/alienisolation/latest/android/faqs ; capture in-game or from Feral's mobile page https://www.feralinteractive.com/en/mobile-games/alienisolation/ | Closest-genre precedent; screenshot source **UNVERIFIED** |
| M20 | Android back-gesture exclusion zones and gesture insets diagram | https://developer.android.com/develop/ui/views/touch-and-input/gestures/gesturenav | Android edge slide |
| M21 | Display cutout modes in landscape (letterboxed vs short-edges) | https://developer.android.com/develop/ui/views/layout/display-cutout | — |
| M22 | Into the Dead layout presets (four ~25 % splits) | GAG rearrange page — https://gameaccessibilityguidelines.com/allow-interfaces-to-be-rearranged/ | Data point for "offer presets" |
| M23 | GDC talks for citation (no frames): Zach Gage 2012; Brawl Stars 2019 | https://gdcvault.com/play/1015663/ · https://gdcvault.com/play/1025751/ | — |

---

## 9. Implications for FrontRooms (concrete rules)

Context from memory and the brief: world-first minimal HUD; Relay pursuit punishes noise (sprint is loud); E taps doors at ~2 m with a centre-dot crosshair; hold E breaks glass with a progress bar and a *tap mode*; the project keeps platform-specific work on **separate tracks** (as with WebGL). Rules marked (D) are my derivations.

**Track and settings**
- **R0 — Separate track.** All touch code is gated to touch platforms (`#if UNITY_IOS || UNITY_ANDROID`, or a touchscreen present at runtime). Desktop controls and HUD are unchanged. (D, consistent with the project's WebGL separate-track rule.)
- **R1 — Resolve Active Input Handling before any touch work.** Unity 6.3 says *Both* is unsupported on Android, and the setting is project-wide.
  - Option A (recommended): migrate the ~22 legacy `Input.*` reads to Input System actions, with the same desktop bindings (WASD / mouse / Shift / E / S / Esc / O / R / Space), then switch to *Input System Package (New)*. Desktop feel stays identical, and on-screen controls and gamepads then work for free.
  - Option B: keep Both and verify on a real Android device with the *Activity* entry point. This **contradicts Unity's docs**; treat it as a test only.
  - ([Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsAndroid.html))
- **R2 — Android build hygiene.**
  - Upgrade the editor to **≥6000.3.13f1** (GameActivity 4.4), or switch the entry point to Activity.
  - Install the **API 36** SDK and set Target API 36 explicitly.
  - Keep IL2CPP + ARM64.
  - Keep App Category = Game.
  - Decide on predictive back (§R12).
  - ([Unity GameActivity](https://docs.unity3d.com/6000.3/Documentation/Manual/android-application-entries-game-activity-requirements.html), [Play](https://developer.android.com/google/play/requirements/target-sdk))
- **R3 — Frame rate.** Set `Application.targetFrameRate = 60` explicitly on mobile (Unity's default is 30). ProMotion 120 is an opt-in quality tier. ([Unity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html))

**Layout**
- **R4 — Size touch controls in millimetres, not HUD canvas units.**
  - Put touch controls on their own canvas: CanvasScaler *Constant Physical Size* in **mm**, or ScaleWithScreenSize with sizes computed from DPI (Android: xdpi/ydpi via JNI, since `Screen.dpi` is a bucket).
  - Keep the 1920×1080 world-first HUD for text and readouts.
  - Defaults: phone primary buttons **15 mm** (XAG); phone secondary buttons **≥12 mm** (Hoober corners); menu and pause **≥7.3 mm** (44 pt). Tablet primary buttons **24 mm**.
- **R5 — Safe-area-aware margins.**
  - Per side, `margin = max(72 cu, safeInset + 4 mm)`. On iPhone Pro the island side then becomes about 151 + 59 ≈ **210 cu**, not 72.
  - Read `Screen.safeArea` each time the resolution or orientation changes; never hard-code (iOS 18 → 26 changed the landscape top inset from 0 to 20).
  - The world still renders full-bleed.
- **R6 — Tablet canvas.** At match 0.5 the iPad canvas is only ~1727 cu wide. Either switch the HUD CanvasScaler to **Expand** (the 1920×1080 grid always fits; phones get extra width, iPad extra height) or anchor HUD groups to edges, as Apple's "nine anchors" model does. Re-layout on every size change (iPadOS 27 windowing, iPhone Duo fold or unfold).
- **R7 — Screen zones.**
  - Left half below the top ~20 %: movement.
  - Right half: look.
  - Bottom-right thumb arc: contextual action button(s).
  - Top edge inside the safe area: pause / settings / skip (secondary, ≥7.3 mm).
  - Keep the centre (crosshair, doors) clear. No control may start inside the bottom gesture inset (home indicator) or the Android back-gesture strips unless exclusion is active.

**Controls (desktop → touch)**
- **R8 — WASD → floating left stick.**
  - Origin wherever the thumb lands in the left zone (Unity `ExactPositionWithDynamicOrigin`, or a custom stick because of ISXB-1027).
  - Hidden at rest; faint ring on touch.
  - Scaled-radial dead zone 0.08–0.12. Throw radius about 10–12 mm on phone, 14–16 mm on tablet, **scaled by DPI** (`movementRange` is in pixels).
  - Options: *Fixed* stick and *Left-handed mirror*.
- **R9 — Shift sprint → push-past-threshold sprint**, in keeping with Apple's "big tilt".
  - Because sprint is loud and draws the Relay, require **≥0.85 deflection for ≥150 ms**, with hysteresis to drop out below 0.7.
  - Show a **halo on the stick ring** plus the existing stamina cue, and a light haptic on entry (iPhone/Android only).
  - Settings: *Sprint = stick push / toggle button / off*. Never "always sprint" by default in this game, because it would trigger the Relay. (D)
- **R10 — Mouse look → right-half relative touchpad**, not a second stick.
  - 1:1 deltas by default, about 4°/mm (D), separate horizontal/vertical sliders (×0.25–×4), invert Y, no acceleration by default.
  - Optional **gyro: Off (default) / While touching / Always**, with a gyro sensitivity in real-world degrees (GyroWiki).
  - Each finger is owned by `touchId` from touch-began to touch-ended.
- **R11 — E (tap door) → tap the door itself, with a contextual button as the fallback.**
  - (a) A tap on screen is *interaction-first*: if a raycast from the tap point hits a door handle within 2 m, it's a door tap (HIG direct touch). A tap is a touch shorter than ~200 ms that moves under ~3 mm, so it can't be mistaken for look-drag (D).
  - (b) When the centre dot rests on a valid door, show a **door glyph button** near the right thumb (WWDC26 "show when available"), sized at the primary size, activating on **touch-end**, cancelled by sliding off (XAG 107).
  - (c) **Interaction magnetism:** while the look finger moves and the dot is within ~3–5° of a handle, scale look gain down by about 0.5 (semantic-pointing style). On release, the door under the cone counts as targeted. Toggle: *Interaction assist Off / Slow / Snap*, mirroring Alien: Isolation's Off / On / Auto-Aim.
- **R12 — Hold E (break glass) → the same contextual button with a hold-progress ring drawn outside the thumb footprint.**
  - The existing *tap mode* setting becomes the touch accessibility path. Make tap mode the **default on touch if the hold exceeds ~2 s** (XAG: avoid ≥2–3 s holds).
  - The ring must have an audio tick or ramp, because iPad has no haptics.
  - Optional continuous haptic texture on iPhone (Core Haptics continuous event) and on Android only if `arePrimitivesSupported` says so. No buzzy fallback.
- **R13 — Walking into a broken frame to climb** needs no button. Keep it.
- **R14 — S (cancel shot) → a top-right "skip" chip** (≥7.3 mm, appears only during a shot) plus "tap and hold anywhere ~0.5 s". No edge swipes, which would collide with system gestures.
- **R15 — Esc / O / R / Space.**
  - Pause button top-left inside the safe area. Android Back (predictive back on with an `OnBackInvokedCallback`, or the opt-out flag) opens or closes pause. Test how Unity surfaces it on Android 16.
  - Settings (O) only from pause, with touch-native rows and scrolling lists, no virtual cursor (Apple: "It's crucial that your menus respond to touch").
  - Restart (R) only from pause, with confirmation (XAG 115).
  - Title Space → "tap to begin" on touch-end, anywhere.
- **R16 — New touch settings rows.** These sit beside the existing HDR / camera motion % / reduce flashing / break-glass hold-tap / captions / Relay readout:
  - Look sensitivity H/V, invert Y, gyro mode.
  - Stick Fixed/Floating, dead zone.
  - Sprint mode.
  - Interaction assist.
  - Control size (S/M/L/XL, defaulting to the XAG sizes), control opacity, left-handed mirror.
  - **Edit layout** (drag / pinch / opacity / reset to left- or right-hand default; the Feral model).
  - Haptics on/off + intensity, kept separate from **atmosphere haptics** (Relay footsteps) per GAG.
  - Read iOS Reduce Motion (Apple.Accessibility) to set the default for camera motion %.
- **R17 — Feedback.** Every control has a visible press state that extends beyond the finger (glow ring); a sound on press; haptics only where `supportsHaptics` is true (never iPad); haptics never the sole channel (XAG 110). Android: `performHapticFeedback` constants first; avoid `Handheld.Vibrate`.
- **R18 — Idle fade.** Controls fade to about 20–35 % opacity after ~2 s idle and return on touch (D). In the "world-first" HUD, the stick is invisible until touched.

**System edges**
- **R19 — iOS.** During gameplay only, defer **bottom-edge** system gestures (`iOS.Device.deferSystemGesturesMode = BottomEdge`) and leave `hideHomeButton` off, because hiding it disables deferral. Restore defaults in menus.
- **R20 — Android.** During gameplay, use immersive *transient-bars-by-swipe*. If a look drag must start near a side edge, call `Window.setSystemGestureExclusionRects` for that region only (≤200 dp of vertical extent per edge unless the bars are stickily hidden). Never place controls in the bottom home-gesture inset.

**Testing**
- **R21 — Real devices only for input QA.** The Device Simulator has no multitouch and no gyro. Minimum matrix:
  - an iPhone with a Dynamic Island (17 Pro);
  - an iPhone Air or Pro Max;
  - an iPad 11";
  - a 20:9 Android phone at API 36;
  - a low-end Android phone running below 30 fps (ISXB-1118 repro);
  - if available, an iPhone Duo or Android foldable for fold/unfold re-layout.
- **R22 — Metrics.** Log per-session time-to-first-door-tap, mis-taps (taps on empty space within 10 mm of a button), unintended sprints (sprint entries under 0.5 s), and look-finger lifts per 180° turn (a clutching proxy, Casiez). (D)

---

*Sources were last checked 2026-10-03. Several Apple pages render from JSON. Quotes were taken from the Apple developer JSON data that backs each HIG and documentation page, and WWDC transcripts were taken from the video pages.*
