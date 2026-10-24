# Sources · rounded display corners

Everything was read on 2026-10-24. The status of each source:
- **Primary:** the vendor's own docs, talks or forum staff.
- **Measured:** our own probe.
- **Community:** reported by players or developers, not verified by us.

| # | Source | What it supports | Status |
|---|---|---|---|
| S1 | Apple, UIKit `UIView.LayoutRegion`: `safeArea(cornerAdaptation:)`, `margins(cornerAdaptation:)`, `readableContent(cornerAdaptation:)`, `edgeInsets(for:)`; iOS 26+. https://developer.apple.com/documentation/uikit/uiview/layoutregion. Also the iOS 27.1 SDK header `UIKit.framework/Headers/UIViewLayoutRegion.h` (`UIViewLayoutRegionAdaptivityAxis` None / Horizontal / Vertical) and `UIView.h` (`edgeInsetsForLayoutRegion:`) | The corner-adapted regions exist, and Objective-C can call them | Primary |
| S2 | Apple Developer Forums, "Accommodating asymmetric safe area insets and system clusters across both display surfaces" (iPhone Duo UIKit Q&A, Apple Technology Evangelist reply). https://developer.apple.com/forums/thread/848019 | For iPhone Duo, use `margins(cornerAdaptation:)` and `reservedRegions(kind:options:)` | Primary (forum staff) |
| S3 | Apple, WWDC25 session 356 "Get to know the new design system". https://developer.apple.com/videos/play/wwdc2025/356/ | Concentric shapes (inner radius = the parent's radius less the padding). On iPhone, "use a capsule with extra margin to create space near the screen edge"; on iPad and Mac, concentric shapes aligned with the window edge | Primary |
| S4 | Apple, Meet with Apple "Design great interfaces for handheld games" (2025). https://developer.apple.com/videos/play/meet-with-apple/243 | For games, safe areas are guides; anchor HUD parts to the nearest edge at consistent distances; 44 pt targets, 17 pt text | Primary |
| S5 | Apple HIG, Layout. https://developer.apple.com/design/human-interface-guidelines/layout (read through its JSON data feed) | Respect safe areas, margins and guides; tvOS 60 pt top and bottom, 80 pt sides | Primary |
| S6 | Android Developers, "Insets: apply rounded corners". https://developer.android.com/develop/ui/views/layout/insets/rounded-corners | `RoundedCorner` and `WindowInsets.getRoundedCorner()` (API 31+) give radius and centre. The sample keeps a corner button inside the arc's 45° point (offset = r · sin 45°); the full-bleed sample pads by the whole radius; no effect on square screens | Primary |
| S7 | Samsung Developers, One UI "Designing for foldable". https://developer.samsung.com/one-ui/largescreen-and-foldable/designing_for_foldable.html | Fill both screens with no letterboxing; continuity across the fold; no corner rule on that page | Primary |
| S8 | Microsoft Xbox developer docs, "Screen areas". https://devdocs.xbox.com/build/core-features/graphics/overviews/screen-areas | Title-safe is the inner 90 % (1720 × 1000 at 1080p), action-safe 95 % | Primary |
| S9 | Wikipedia, "Safe area (television)". https://en.wikipedia.org/wiki/Safe_area_(television) | Where "title safe" comes from | Secondary |
| S10 | Minecraft Feedback, "Screen Safe Zone" (a player thread; the page returned 403 to our fetch, so this is from the search summary). https://feedback.minecraft.net/hc/en-us/community/posts/360067316492-Screen-Safe-Zone | Bedrock has "Change screen safe area" under Video settings; players report that the box doesn't fit the corners on some iPads | Community |
| S11 | itch.io devlog (v1.3 update). https://itch.io/devlog/1139871/-update-v13.amp | A calibration mode so nothing is cut off on curved edges or gesture bars | Community |
| S12 | Game Developer, "Mobile games issues with frontal camera notch". https://www.gamedeveloper.com/game-platforms/mobile-games-issues-with-frontal-camera-notch | The HUD clipped by notches; a test setting with several cut-out shapes | Secondary |
| S13 | Huawei HarmonyOS, "Apply immersive effects" (`getWindowAvoidArea`, `AvoidAreaType`). https://developer.huawei.com/consumer/cn/doc/harmonyos-guides/arkts-develop-apply-immersive-effects | Avoid areas cover the status bar, navigation indicator and cutouts; no corner rule found | Primary |
| M1 | Our probe, `~/FrontRoomsInteractionWork/duo/probe_margins/main.m`, on the iOS 27.1 iPhone Duo simulator and the iOS 26.2 iPhone 17 Pro and iPad Pro 13 (M5) simulators, 2026-10-24 05:4x–06:1x (logs in the session scratchpads) | The §2 table and the 0.293 r formula | Measured |
| M2 | Harness round e, `iphone-duo-20261024/Project/Verification/touch-playtest/e-adaptive/adaptive_2_playing.png` and `_ui.json` (inner display, 951 × 669 pt, corners 55) | Today's gaps: pause 3.5 pt, zone text 6.9 pt (rc01) | Measured |

**Not confirmed:**
- **Material 3 margins.** It is often quoted as 16 dp for compact width and 24 dp for medium and expanded, but no official page was read for it.
- **Game settings.** Players say Genshin Impact, PUBG Mobile, Honor of Kings and similar games have in-game "notch / screen adaptation" sliders. The searches found no source, so they are left out of the README.
