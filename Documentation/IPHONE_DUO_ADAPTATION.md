# iPhone Duo adaptation — evidence and acceptance

Research checked 2026-10-24. Sources below are Apple documentation and Unity's official platform-team update. This document distinguishes project-level adaptive layout from verified native-device support. A passing Editor test is not a claim that an iPhone Duo build has passed simulator or hardware testing.

## Platform and submission requirements

iPhone Duo was announced September 9, 2026, with availability beginning October 23. It is an official product, not a speculative profile. [Apple announcement](https://www.apple.com/newsroom/2026/09/apple-unveils-iphone-duo/)

The currently enforced general App Store Connect minimum is Xcode 26 or later with the iOS 26 SDK or later, effective April 28, 2026. This submission minimum is distinct from the SDK needed for the full Duo experience. [Apple upcoming requirements](https://developer.apple.com/news/upcoming-requirements/)

Apple documents three compatibility levels:

| Build SDK | Duo behavior |
| --- | --- |
| iOS 26 or earlier | Existing app runs in a compatibility-sized region. Inner display does not become a full adaptive layout. |
| iOS 27 | App resizes across displays, with a reserved status-bar strip. |
| iOS 27.1 or later | Full display use, including updated vertical native navigation/toolbars. Layout must handle this change. |

Use Xcode 27.1's Duo simulator to validate native behavior. App resizability follows the current view/window bounds and scale, not a cached main screen or a list of known models. Size and insets can change during the same session. [Apple preparation guide](https://developer.apple.com/iphone-duo/prepare/)

## Dimensions: three different kinds of evidence

| Measurement | Outer display | Inner display | Meaning |
| --- | --- | --- | --- |
| Apple physical panel specification | 1398 × 2034 pixels, 5.4 inches | 1878 × 2670 pixels, 7.6 inches | Hardware specification, not a fixed app window or point-space layout. |
| App Store Connect screenshot dimensions | 1398 × 2034 or 2034 × 1398 | 2007 × 2853 or 2853 × 2007 | Accepted submission assets; do not substitute panel pixels for these dimensions. |
| Project synthetic landscape regression profile | 678 × 466 points at assumed 3× | 890 × 626 points at assumed 3× | Deliberately synthetic layout inputs derived from panel pixels; scale and safe insets are not measured Duo UIKit metrics. |

[Apple hardware specifications](https://www.apple.com/iphone-duo/specs/). [App Store screenshot specifications](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications).

The screenshot specifications currently state that, starting April 2027, apps using iOS 27.1 SDK or later must provide Duo screenshots. Submitting them also enables the associated optimization badge. Do not treat the synthetic Editor profiles as suitable App Store screenshots.

## Layout rules used by this project

- Read available bounds continuously. A phone can have a large view and a tablet can have a narrow view; hardware identity must not choose a layout.
- Preserve game/menu state across display transitions. Cancel touch ownership and accumulated gesture deltas when the coordinate space changes so a stale hold, swipe, or pinch does not become a new action.
- Keep USE, inspect BACK, sprint, settings content, and similar foreground elements in the safe region; full-screen backgrounds may extend behind it. Handle left, right, top, and bottom independently because safe areas are asymmetric.
- Superseded in part by "Give way, locally" below (Red, 2026-10-24, later the same day). User-directed visual exception, 2026-10-24: position the pause button at the right screen bleed (`frame.width - 28 pt`), with a 44-point target inside the full frame and vertical safe bounds. Center the pause/menu group on the full frame, not the horizontally asymmetric safe rectangle. The settings card remains centered within its safe region. This corner-button choice overrides the earlier blanket foreground-safe rule; confirm camera/system-overlay interaction in the target device poses rather than claiming horizontal safe-area containment.
- Reflow settings into one scrollable column when two columns cannot fit. Keep the existing point sizes and 44-point target floor instead of shrinking the whole interface to fit.
- Check narrow and short windows as well as closed/open phone sizes. Split View and other system arrangements create app sizes that differ from the full panel.

These are implementation choices applying Apple's adaptive design guidance to the game's custom Unity UI. Apple's native toolbars automatically adopt the side axis; a Unity Canvas does not become a native toolbar merely by recompiling. [Duo Human Interface Guidelines](https://developer.apple.com/design/human-interface-guidelines/designing-for-iphone-duo), [Apple safe-area discussion](https://developer.apple.com/videos/play/tech-talks/111461/).

## Measured on the iOS 27.1 Duo simulator (2026-10-24, build with the native layout log)

| | Outer display | Inner display |
| --- | --- | --- |
| View | 678 × 466 pt @3 (landscape) | 951 × 669 pt @3 (landscape); 669 × 951 upright |
| Safe area | at launch the camera's whole edge (B 82 bottom right, T 82 turned over); settled: L 84 turned over; home B 34 | B 34 (home indicator), both orientations |
| Camera | at launch no region (only the band); settled: an occlusion region 37 × 37 pt (centre 47.83 pt from the far corner's two edges) inside an 84 × 82 pt keep-out region | FaceTime camera: an occlusion region, 58 × 37 pt at the top in landscape, active only while it runs |
| Corner radii | settled: 59 pt far side, 8 pt hinge side (UICornerConfiguration probe; 0 at launch) | 55 pt (UICornerConfiguration probe) |
| Fold | — | division region, 40 pt band with 20 pt margins, inactive when flat |

Apple's iPhone Duo tech talk: "The outer display camera is always present", handled by the safe area. Before iOS settles, `UIViewLayoutRegion` bar regions and corner-adapted safe areas carry no position along that edge, so the project carries the outer camera's place itself until iOS reports the region (they match to 0.1 pt).

## Give way, locally (Red, 2026-10-24)

Red: no whole-edge avoidance. Everything stays full bleed; only content that would collide with the camera (or a rounded corner) is pushed away, with a motion transition, in every rotation and on the outer display alone. Implementation and numbers: `TOUCH_CONTROLS.md` §3.1 (`FrontRoomsHandheld.Shape`, `ClearRect` / `ClearCircle`, 12 pt from a camera, 0.28 s OutCubic). The settings sheet is black frosted and full bleed to the bottom; views squarer than 16:9 keep 16:9's horizontal field of view.

Rounded corners (Red, 2026-10-24, on the inner display: the HUD sat on the curves): no platform puts the corners into the safe area; iOS 26's corner-adapted layout regions and Android's `RoundedCorner` sample both stop at the arc's 45° point (0.293 r, measured on four simulators), where a box's corner touches the curve. The in-play HUD's corner parts now keep 16 pt from the curve (`FrontRoomsHandheld.HudCornerGap`, `ClearCornerBlock`); research and options in `research/interaction/duo/rounded_corners/`, numbers in `TOUCH_CONTROLS.md` §3.1, before/after VL435.

Cold start (Red, 2026-10-24): the launch screen is the title's logo on black, the game's first frame the same picture, the room revealed once it has drawn and TAP TO START once everything is loaded (`TOUCH_CONTROLS.md` §3.2, VL434). The unfold transition (outer to inner) is researched separately in `research/interaction/duo/fold_transition/`.

Verification (Duo copy, 2026-10-24): adaptive layout tests 737 passed, 0 failed; touch playtests with 0 failures on iPhone 16 Pro (155 checks), iPad 11 (166), Android 20:9, Duo outer (167) and Duo inner; the adaptive run's give-way checks (zone, pause, USE, settings heading, BACK, every row, also scrolled to the end) on both outer poses (launch and settled) and the inner display, 68 checks, 0 failures; on the iOS 27.1 simulator: the outer display turned over and settled (zone and SETTINGS pushed 12 pt past the camera, pause drawn in along the 59 pt corner, rows narrowing as they scroll past the camera, the 84 pt side band given back) and the inner display (field of view, 55 pt corner draw-in). Folding in Xcode 27's Device Hub is not scriptable; the build's `[HandheldNative]` log records each real fold and rotation.

## Fold avoidance is separate from rectangular safe-area handling

The center fold is a division region rather than an outside screen inset. iOS 27.1 supplies `UIViewReservedRegion` and `reservedRegionsOfKind:options:` for custom native layouts. Occlusion regions describe covered content; division regions describe areas that split content. Region frames are in the owning view's points and already include margins. A division becomes inactive when the device is fully open.

`Screen.safeArea` alone cannot describe an interior hinge region. A build must not claim fold-aware button displacement unless it actually consumes the native division-region geometry and verifies the resulting layout. Native region bridging and half-fold device acceptance remain separate work when absent from the implementation. [UIKit reserved regions](https://developer.apple.com/documentation/uikit/uiviewreservedregion), [Apple adaptive-fold talk](https://developer.apple.com/videos/play/tech-talks/111463/).

## Unity-specific evidence and limits

Unity's official platform team reports that the rendered view and UI systems already respond to window resizing; `Screen.width`, `Screen.height`, and `Screen.safeArea` provide changing bounds. The team recommends testing against SDK 27.1. Its published observations were initially simulator-based.

With SDK 27.1, enabling `UIRequiresFullScreen` can preserve supported orientations with system letterboxing on Duo; disabling it allows the inner view to rotate freely. It does not opt out of inner-display side-by-side use. Treat the flag as compatibility behavior, not a substitute for responsive layout. Unity also reports incorrect Duo `Screen.dpi` values, so physical-size scaling based on that value is unsuitable.

Unity staff states that the Unity Device Simulator will not receive Duo support, and a dedicated fold/unfold event is not planned. Use Apple's simulator for device poses and a native plugin when fold-specific geometry is needed. [Official Unity platform update](https://discussions.unity.com/t/info-ipados-27-sdk-27-adaptability-changes-iphone-duo-updates/1737980)

## Local regression harness and remaining acceptance

Run in an isolated Unity project copy, outside Play Mode:

```text
-executeMethod FrontRoomsAdaptiveLayoutTests.RunBatch -quit
```

The harness creates an inactive control component and a minimal settings host; it does not load a game scene. It checks ordinary iPhone positions, synthetic Duo profiles, portrait and narrow windows, asymmetric insets, multiple control scales, settings reachability, menu hit-target bounds, and transient input reset on resizing. Pause is tested against the full horizontal frame and vertical safe bounds, and menu centering is tested against the full frame, following the user's visual direction above. Results are written to `Verification/adaptive-layout-tests.json`. The report is generated only by running the harness; the presence of this source file is not a passing test result.

Remaining native acceptance must cover:

1. Build/export with the actual installed Unity and Xcode 27.1+ toolchain and inspect the SDK used by the resulting app.
2. Launch the Xcode Duo simulator on outer and inner displays, then resize/switch displays while moving, holding USE, pinching, pausing, and scrolling settings.
3. Exercise portrait, landscape, left/right Split View, book-like half fold, and table-like half fold. Check HUD, menus, input, camera framing, and retained game progress.
4. Confirm native point scale, asymmetric safe areas, reserved regions, rendering, performance, and controls on hardware. Editor geometry checks cannot establish those outcomes.

## Implementation and current build evidence (2026-10-24)

- Source baseline: `main` at `7802f3ee`; changes are scoped to the mobile display/input/layout and build path.
- Build identity: `com.redwang.frontroomsss`, version `0.1.0 (16)`. The iOS build profile disables `UIRequiresFullScreen`; ordinary outer-screen gameplay retains its existing landscape policy, while the inner window is allowed to resize.
- Native point metrics use Unity's current UIView bounds and trait display scale, refreshed each frame. The Unity pixel/point conversion accounts for render-resolution scaling; layout no longer uses a cached `UIScreen.main.nativeScale` or iOS DPI estimate.
- HUD and vector logo use current point-space geometry. Touch ownership is cancelled when geometry changes. Settings stack and scroll at narrow widths; pause/caught text and legends reflow.
- Editor regression: **322 passed, 0 failed**. JSON: `Builds/Mobile/iPhoneDuo-Build16/Evidence/adaptive-layout-tests.json`. This is geometry/input evidence; simulator and hardware results are recorded separately below.
- Build/QA staging: `/Users/redwang/FrontRoomsBuildWork/iphone-duo-20261024/`. Original Build 15 exports/archives are preserved.
