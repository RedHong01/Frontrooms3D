# iPhone UI motion variations

These three MP4s are motion studies for the 874 × 402 pt Figma phone master. They use the same skeleton, positions, colors, labels, hit-target intent, and safe-area anchors; only the transition language changes. The title now follows the latest review direction: `TAP TO START` is a Bayon text line below the wordmark, with no button container, and the wordmark plus prompt fade together after the tap.

| Variation | Motion language | Timing decisions | Best use |
| --- | --- | --- | --- |
| `motion-variation-01-cinematic.mp4` | Quiet, spatial, layered | 240 ms menu wash/card, 18 pt vertical settle, 24 ms settings stagger, 180 ms control fade | Default exploration and pause/settings navigation |
| `motion-variation-02-tactile.mp4` | Fast, tactile, responsive | 120–180 ms response, 0.92 press scale, spring return, 1.08 sprint socket pulse | Door/glass interaction and players who value immediate feedback |
| `motion-variation-03-reduced.mp4` | Reduced motion | Opacity-only transitions, no spatial slide or scale, 120 ms response | iOS Reduce Motion / motion-sensitive players |

## Research translated into the studies

- Apple’s motion guidance recommends purposeful, brief feedback that is tied to the action and gives users alternatives such as haptics or sound. That is why the studies keep the UI response short and leave the Figma labels unchanged: [Motion](https://developer.apple.com/design/human-interface-guidelines/motion?changes=l_9_3).
- Apple’s handheld-game guidance emphasizes adapting console/PC interaction patterns to the thumb reach, camera placement, and feedback needs of iPhone/iPad: [Design great interfaces for handheld games](https://developer.apple.com/videos/play/meet-with-apple/243/).
- Apple’s touch guidance uses 44 pt as the minimum target, so the visual 72 pt USE ring and the separate 88 pt hit area are kept distinct: [Design tips](https://developer.apple.com/design/tips/).
- Reduce Motion requires changing or removing depth, parallax, blur, and similar motion effects. Variation 03 removes the slide, scale, and pulse rather than simply shortening them: [Reduce Motion evaluation](https://developer.apple.com/help/app-store-connect/manage-app-accessibility/reduced-motion-evaluation-criteria).

## Figma source constraints

The Figma file contains static screens rather than exported timelines. The implementation therefore treats the phone masters and TC12 interaction/haptics notes as the source of truth for geometry and semantics, while the three motion curves are proposed behavior for review. The key geometry is preserved in the runtime view: pause chips at 75/83/79 × 40 pt, 44 pt pause hit target, 72 pt USE visual, 88 pt USE hit target, 874 × 402 logical canvas, and the dark/warm-white/yellow palette.

## Typography and alignment correction

The previous review render used family labels that CoreText could resolve to a fallback face. The renderer now registers the bundled fonts and addresses their exact PostScript names: `Bayon-Regular` for 17/17 labels and 48/44 title, `IBMPlexMono-Regular` for 11/14 metadata and settings values, and `SourceSerif4Roman-Regular` for 20/22 hint copy and 26/26 zone context. Text is positioned from CoreText ascent/descent and the declared Figma line height, then aligned inside measured frames; this removes the old `size × 0.82` baseline guess and prevents optical drift between labels and values. Settings rows are split into a Bayon label and right-aligned IBM Plex Mono value so numerals never inherit Bayon. The title logo uses the supplied lockup asset, tinted to the light Figma treatment, at its measured frame `(167, 133.28, 540, 107.44)`.

Safe-area placement keeps the Figma phone frame as a full 874 × 402 logical canvas, because its artwork already includes the 62 pt side and 21 pt bottom margins. Only dynamic action anchors convert the current `Screen.safeArea` through the CanvasScaler point-to-pixel factor; the fallback touch rectangles use the same conversion. This avoids the earlier double-inset and Retina-pixel mismatch that pushed the USE and fixed-stick targets away from their Figma centers.

## Unity mapping

The production motion is implemented in `Frontrooms3D/Assets/Scripts/Input/FrontRoomsTouchControlsView.cs`:

- state changes remain immediate for touch hit testing;
- the visual shell eases on unscaled time so pause/settings transitions finish while the game is paused;
- menu wash/card, USE prompt, joystick/socket, pause icon, menu chips, and settings rows each have independent targets;
- press scale and sprint pulse are suppressed when the existing motion/flashing settings request reduced behavior;
- safe-area and left-handed target positions are refreshed before visual easing.

The mobile branch in `Frontrooms3D/Assets/Scripts/FrontRooms3DGame.cs` now uses the same 874 × 402 canvas for the room rule, hint card, crosshair/prompt, hold bar, stamina, captions, key readout, logo, pause text, and caught text. The desktop 1920 × 1080 layout remains separate.

The MP4s are review artifacts, not screenshots of a live device build. Final device acceptance still requires exporting the Unity iOS project and checking the scene on an iPhone.
