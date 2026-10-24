# R2 - How mobile and Apple-silicon port games present graphics, performance and battery settings

Research for the FRONTROOMSSS settings redesign. Research only. Links only: no image, video or file was downloaded.

## Method

- Date: 2026-10-24.
- The material was gathered 2026-10-24 07:27-07:49 PDT by the original researcher agent. That agent stopped (account limit) before writing. This report was written up from its saved fetches on 2026-10-24.
- Every claim cites a source ID like [S12]. Each ID maps to a page that was actually opened (table at the end). Pages that appeared only in search results, or that failed to open, are listed in a separate table and support no claim.
- Page text was read as extracts (the fetch tool, or the text of a browser pane). No page was seen as a screenshot. So a widget type (slider, toggle, segmented control) is named only when the source text names it. Otherwise the report says "not described".
- Quotes are short and come from those extracts. Wording may be trimmed. Treat quotes as close, not guaranteed word for word.
- Extra checks by the writer on 2026-10-24: six new WebFetch calls. (1) Apple ProMotion article, full text, because the saved extract was cut off [S56]. (2) Android FPS throttling page [S70]. (3) Android Unity large-screen page [S74]. (4) Android Authority Genshin article [S3]. (5) Minecraft official Vibrant Visuals page: timed out, no content. (6) WWDC21 session 10147 [S58].
- The original agent's scratch notes were read as pointers. A detail that rests only on those notes is marked "(agent notes)".
- Where the material is thin the report says "unverified" and gives the reason.
- Case tags: [M] mobile-first game, [P] console or PC game ported to iPhone, iPad or Mac.

## A. Cases

18 cases (M1-M18). Each has: where the settings sit, options and grouping, component, presets and auto-detect, frame rate and resolution, heat and battery wording, media links, and one lesson for FrontRooms. "Lesson" lines are my reading of the source, not a claim by the source.

### Quick matrix (only what the opened text says)

| # | Game | Preset names | Frame-rate choices | Resolution | Heat or battery wording |
|---|------|--------------|--------------------|------------|-------------------------|
| M1 | Genshin Impact [M] | "Lowest" named; full list unverified; plus Custom | 60 / 30 on mobile | Render Resolution Low / Medium / High | live load bar: Smooth ... Overclocked |
| M2 | Honkai: Star Rail [M] | Graphics Quality incl. Custom; "Recommended" marker | 30 / 60 | Resolution Medium / High | none in menu text; Special Effect Mode cuts memory |
| M3 | Wuthering Waves [M] | Quality, High Quality (chip-gated) | unverified on mobile | rises by default on High Quality | "may occasionally cause performance issues"; Frame Generation uses less power |
| M4 | PUBG Mobile [M] | "Smooth" named; others unverified | High 30 / Ultra 40 / Extreme 60 / 90 / Ultra Extreme 120 | not covered | high frame rate "at the cost of battery life" (second-hand) |
| M5 | Call of Duty: Mobile [M] | Low / Medium / High | Low / Medium / High | not covered | none |
| M6 | Minecraft Bedrock [M] | Favor Performance / Favor Visuals | not covered | upscaler TAAU or Bilinear | big performance warning on low-end |
| M7 | Roblox [M] | Automatic or Manual slider | not covered | not covered | "device works less" at lower levels |
| M8 | Diablo Immortal [M] | Quality Selection tool | locked to 30 on a Galaxy A53 | Medium / High / Ultra (Ultra locked) | Device Load bar |
| M9 | Sky: Children of the Light [M] | 4 named modes on a 0-3 bar control | 20 / 30 / 60 | low-res to native | "preserve ... battery", "keep the device cooler" |
| M10 | Alien: Isolation [P] | Graphics / Battery Saver / Performance | not numeric in menu | not covered | Battery Saver "alleviate ... overheating" |
| M11 | GRID Legends [P] | none documented | not covered | not covered | Home-bar frame-pacing issue |
| M12 | Resident Evil Village [P] | several presets + "recommended" | a frame-rate option (values not given) | 2340x1080 / 2796x1290 + scaling | "quite warm" |
| M13 | Resident Evil 4 [P] | none | locked 30 | fixed, about 360p to 720p | "a bit warm" |
| M14 | Death Stranding DC [P] | none | locked 30 | fixed 720p out | not covered |
| M15 | Assassin's Creed Mirage [P] | Low / Medium / High, default Medium | locked 30 | fixed quarter of display | not covered |
| M16 | Cyberpunk 2077 Mac [P] | dynamic "For this Mac" | unverified | auto | not covered |
| M17 | Assassin's Creed Shadows [P] | 5 presets Low ... Ultra High (game-wide) | benchmark; Mac min spec 30 | dynamic res + MetalFX | not covered |
| M18 | No Man's Sky [P] | none documented | not covered | MetalFX | "battery life and thermodynamics" |

### M1 Genshin Impact [M] (iOS, Android)

- Where: pause, open the Paimon Menu, then Settings > Graphics [S3][S4].
- Options and grouping: a "Graphics Quality" preset, then a "Custom" section. Custom lists Render Resolution, Shadow Quality, Visual Effects, SFX Quality, Environment Detail, FPS, Motion blur, Bloom, Anti-Aliasing, Crowd Density, Co-Op Teammate Effects [S3].
- Presets: the guide sets Graphics Quality to "Lowest" [S4]. The full preset list is not in the opened text (unverified).
- Frame rate and resolution: FPS 60, or 30 if the device cannot keep up; Render Resolution Low, or Medium or High if the device can handle it; Anti-Aliasing TAA [S3]. PC offers 30, 45, 60 and has no load bar [S4].
- Load meter: on mobile a bar sits at the top, right of "Current Performance Load", reading "Smooth". The guide says keep the green bar small [S4]. The Android Authority guide says a stuttering device may read "Overclocked", and tells the player to lower the Custom settings [S3].
- Restart: changing settings shows "Some graphics settings will only be applied after the game is restarted." [S4]
- Heat: one player says an iPhone 14 at 60 FPS "turns into a toaster". This is a user anecdote [S5]. The Android Authority mobile section does not mention heat [S3].
- Components: only the load "bar" is named. Other widgets are not described.
- Media: none opened.
- Lesson: a live load gauge beside the quality rows gives feedback without a warning label. Mark the rows that need a restart.

### M2 Honkai: Star Rail [M] (iOS, Android)

- Where: Settings - Graphics, then Display Settings on Android [S6][S7].
- Official notes, version 4.4 (page date field 2026/08/26): "Adds a Recommended marker to the "Graphics Quality" settings." The same line adds hint text and a Recommended marker to "Display Settings" on Android [S6].
- Version 3.8 (2025/12/17): a new switch "Pre-compile Shaders on Login" in Settings - Graphics on mobile. Android users first need "Normal Mode" in Display Settings. The first login after an update then pre-compiles shaders, to cut hitches in play [S7].
- Version 2.3 (2024/06/19): a "Special Effect Mode" on some mobile devices changes how combat effects render "to reduce device memory usage". The same notes fix a bug where the FPS setting flipped from 60 to 30 [S8].
- Third-party Android guide: Graphics Quality has a Custom value. Under it: Resolution (Medium or High), FPS (30 or 60), Shadow Quality, Reflection Quality, Character Quality, Environment Detail, Bloom Effect, Anti-Aliasing (TAA), Light Quality. Levels are Low, Medium, High [S9].
- Components: a "switch" is named for Pre-compile. Others are not described.
- Media: none opened.
- Lesson: a "Recommended" marker on the device-fit preset, with a line of hint text, was added after launch. Ship it on day one.

### M3 Wuthering Waves [M] (iOS, Android)

- Where: Settings > Graphics [S1][S2].
- Version 3.2 notes: "Added High Quality as a graphics quality option on mobile above Quality." It needs Snapdragon 8 Elite or newer (or 8s Gen 4 or newer), Dimensity 9400 or newer, or XRing O1. On supported devices the default in-game resolution rises. The notes warn it "may occasionally cause performance issues" and say to try a lower setting [S1].
- Previews: a button beside certain settings shows a text description, and another shows a visual preview. The notes add that previews are for demonstration only [S1].
- Render Distance (PC): Default, Far, Farthest. The notes say Far and Farthest "may contribute significantly to the device load" [S1].
- Version 3.7 notes (text says "Following the Version 3.7 update maintenance"): a Vulkan option at Settings > Graphics > Vulkan, restart needed; it "improves the game's performance and power consumption". A Frame Generation option appears at Settings > Graphics > Frame Generation after Vulkan is on. Running Frame Generation at 60 FPS (maximum) uses less power than 60 FPS without it [S2].
- Mobile frame-rate tiers (30 / 45 / 60 / 120): not verified in the opened pages. The 3.2 notes mention a 120 FPS option only for PC [S1].
- PC counterpoint: a numeric "Rendering Precision" of 0.6, 0.8, 1.0 (default) up to 1.6, separate from DLSS. Third-party guide, PC only [S48].
- Components: "button" for previews. Others not described.
- Media: none opened.
- Lesson: gate the top tier by chip and say so in text. Add a per-option preview. Say when one option depends on another ("after enabling Vulkan").

### M4 PUBG Mobile [M] (iOS, Android)

- Where: Settings > Graphics [S13]. Older guide lists Graphics, Frame rate settings, Style, Anti-Aliasing, Auto-adjust graphics [S15].
- Presets: "Smooth" is named. Patch 1.5 (2021): "90 FPS is now available in Smooth graphics for some new devices." [S11]. Style "Colorful" [S15]. Other preset names: unverified.
- Frame-rate names: High (30), Ultra (40), Extreme (60) in a 2021 guide [S15]. "Ultra Extreme" is the 120 FPS option [S13]. Before version 3.2 the game capped at 90 fps on all devices. Version 3.2 (May 2024) added 120 on select devices, such as Galaxy S23 and S24 series and Z Fold 5 [S12]. The 120 option shows only on supported devices: if it does not appear, the device does not support it [S13]. Which names the current build uses: unverified.
- Component: one guide calls the control a "Frame Rate" slider [S13].
- Auto: patch 0.16.0 (2019) added an "auto-buffering" setting. It will "automatically detect the combat frame rate" and lower match graphics when the rate is low [S10]. A 2021 guide lists "Auto-adjust graphics" and says AA off saves battery [S15].
- Battery and heat: a third-party page says PUBG stated that a high frame rate comes "at the cost of battery life". This is second-hand [S13]. Another notes that the game frame rate must match the display refresh rate, so 120 FPS does nothing on a 60 Hz display [S14].
- Media: none opened.
- Lesson: frame-rate tiers with house names (Ultra Extreme) need a glossary. Use plain numbers on the row. A tier that vanishes when unsupported is hard to discover.

### M5 Call of Duty: Mobile [M] (iOS, Android)

- Where: a gear icon at the top right of the main menu opens Settings [S17].
- Options: the Graphics submenu lets you "customize Graphic Quality and Frame Rate with Low, Medium, and High options". Toggles: Depth of Field, Anti-Aliasing, Bloom, Ragdolls. Battle Royale graphic style: Realistic, Black Ops, Modern Warfare [S16].
- Caveat: both articles are from 2019 (launch). The current menu may differ (unverified). Neither mentions device performance [S16][S17].
- Components: toggles implied by "turn on"; not described.
- Media: none opened.
- Lesson: quality and frame rate are two separate Low / Medium / High controls in one submenu. Keep them independent.

### M6 Minecraft Bedrock [M] (iOS, Android, plus desktop)

- Graphics modes: Simple, Fancy, Vibrant Visuals, Ray Traced. In-game mode switching is off by default and unavailable on lower-end devices [S18][S19]. Source is the community wiki. The official page timed out twice and was not opened.
- Vibrant Visuals on mobile: Android GPUs from Adreno 640, Mali Valhall and others; iPhone A12 Bionic or higher; iPad A12 or M1 or higher. The wiki lists "iPhone Air and iPhone Duo" among iPhones. It is on by default only on stronger GPUs (for example Adreno 740, Mali-G615) [S19].
- Presets: "Favor Performance" for lower-end devices and "Favor Visuals" for high-end. Options can also be set one by one [S19]. Vibrant Visuals settings: Gamma calibration, Reflections, Shadows, Volumetric fog, Point lighting, Point light shadows, Bloom, Graphics presets [S18].
- Upscaling: TAAU (uses several frames, crisper) or Bilinear (better for low-end), plus a final-resolution quality setting [S19].
- Components: the graphics mode is a dropdown; render distance is a slider [S19].
- Render distance: Simple and Fancy 5-96 chunks, Vibrant Visuals and Ray Traced 0-128, "capped in the in-game settings screen" by device [S18]. Vibrant Visuals has its own "Deferred Render Distance" slider, 2-28 chunks, limited per device [S19]. The two pages give different ranges (unverified which applies).
- Warning: turning Vibrant Visuals on for a low-end device with a high render distance can drop performance a lot [S19].
- Media: none opened.
- Lesson: the slider stays but its maximum is capped per device. Preset names state intent. The upscaler is named by method with a plain note.

### M7 Roblox [M] (iOS, Android, desktop)

- Where: in an experience, the Roblox logo (upper left) or Esc opens the menu, then the Settings tab [S20].
- Options: a "Graphics Mode" line set to Automatic or Manual. In Manual a "Graphics Quality" slider moves right for better graphics, left for better performance [S20]. An older help page says the same with a "Graphics Quality - Auto" toggle in a GRAPHICS section [S21].
- Levels: the engine enum has Automatic plus QualityLevel1 to QualityLevel10 [S22]. The step count in the player UI is not stated on the help pages (unverified).
- Help text: lower levels cost some image quality, but "your device works less" [S21].
- Media: none opened.
- Lesson: the lowest-complexity pattern: one slider plus Automatic. The ends are labelled by what they trade, not by numbers.

### M8 Diablo Immortal [M] (iOS, Android)

- Source is a Digital Trends test on a Galaxy A53 5G [S23].
- Frame rate: the phone "does not support 60 frames per second", so the game locks at 30 [S23].
- Resolution: the top "Ultra" choice is not selectable; Medium and High are [S23].
- A "Quality Selection" tool sets visual effects, shadows, fog, environmental details and monster complexity [S23].
- A "Device Load" bar: Medium means average stress; High and Very High reach the limit [S23]. The reviewer's mix was Medium or Low for effects, shadows and fog, and High for environment detail, monster effects and anti-aliasing [S23].
- Blizzard support articles redirected to a login and were not opened.
- Media: none opened.
- Lesson: unsupported options are shown but locked per device, and a load meter sits next to the choices.

### M9 Sky: Children of the Light [M] (iOS, Android, plus Apple desktop)

- Source is a community wiki; its menu screenshot is the August 2024 version [S24].
- Where: a gear icon at the top right opens the menu, with a "Graphics" entry for Resolution, Motion Blur, Fade to Black only and HDR. The wiki says this setting "can help to preserve the device's battery charge" and may keep the device cooler [S24].
- Component: a stepped control of 0 to 3 bars, each bar a named mode [S24]:
  - Devices without 60 fps: no bars = Energy Saving (20 fps); one = Default (30 fps); two = a boosted 30 fps mode; three = not available.
  - 60 fps devices: none = Energy Saving (low resolution); one = Default (30 fps); two = High Performance (60 fps); three = High Definition (30 fps with boosted graphics, or native resolution on M1 or later).
- Media: none opened.
- Lesson: one stepped control where each step has a name and an fps or resolution outcome. The top step is not the highest fps (High Definition is 30). The step list changes with device ability.

### M10 Alien: Isolation [P] (iOS, Android; Feral)

- Where: Options > Video > Graphics Preset, from the Main or Pause Menu [S25].
- Policy: settings are "tuned per-device for a balance between performance, visuals and battery life", and individual settings cannot be changed [S25].
- Presets [S25]:
  - Graphics: best visuals for the device; default.
  - Battery Saver: "Lowers graphical quality to reduce power consumption." It can also "alleviate performance issues experienced due to overheating".
  - Performance: only on high-end devices (iPhone XS and later, SE 2nd gen, iPad mini 5 and later, iPad Air 3 and later, iPad 8 and later, iPad Pro since 2018). It "slightly reduces graphical quality in favour of a higher framerate".
- Review (TouchArcade, 2021-12-16): the default mode seems a 30 fps target with good pacing; Performance seems 60 (reviewer's guess) with a small fidelity drop. The reviewer likes "giving players control over this aspect of the experience" [S28].
- Battery FAQ: ways to cut drain, "in order of impact": screen brightness, internet connection, audio volume, battery health [S26].
- Other options added for touch: Aim Input (Virtual Trackpad or Virtual Joystick), Aim Assist (three levels: Auto-Aim, On, Off), and toggles for Rapid Tap Assist, Radial Menu Pause, Gameplay Vibration, Virtual Controller Vibration [S27].
- Media: official trailer https://www.youtube.com/watch?v=DboBLkA5WRw, page title "Alien: Isolation - Coming to iOS and Android on December 16". It was not confirmed to show the settings menu [S29].
- Lesson: the closest genre match (first-person horror on touch). Three presets named by intent, no custom rows, a Performance preset gated by a device list, and a pause-menu entry.

### M11 GRID Legends [P] (iOS; Feral)

- The FAQ index lists no graphics-settings entry. Topics: supported devices, gamepad, Calico, multiplayer, a known issue, battery, audio [S30]. Whether the game has a graphics menu: unverified.
- Known issue: the Home bar "can cause inconsistent frame pacing on some devices". Workarounds: use a gamepad, or Guided Access to hide the Home bar [S31].
- Media: none opened.
- Lesson: frame pacing can break from system UI, not from quality settings. Test FrontRooms pacing with the home indicator and edge gestures active.

### M12 Resident Evil Village [P] (iPhone 15 Pro, iPad, Mac)

- GSMArena: the iOS graphics menu matches macOS, which is "nearly identical" to PC. Options named: resolution, frame rate, rendering mode, resolution scaling, anti-aliasing, AMD CAS sharpening; texture resolution, caching, filtering, mesh quality; ambient occlusion, screen-space reflections, volumetric lighting, subsurface scattering, shadow quality, contact shadows, shadow cache; bloom, lens flare, film noise, depth of field, lens distortion. Ray tracing and VRS are removed [S32]. The article does not say how the menu is grouped, so the grouping here is the reviewer's list (unverified).
- Upscaling: MetalFX replaces FSR 1.0, with Performance and Quality modes. Rendering mode, resolution scaling, CAS and AA are disabled when MetalFX is on [S32].
- Presets: several, including a "recommended" mode that turns most options off [S32].
- Resolution: Wccftech names 2340 x 1080 and 2796 x 1290, calls 2340 x 1080 the sweet spot, and says the menu shows available video RAM (about 6 GB headroom on iPhone 15 Pro and Pro Max) [S33].
- Heat: the phone "does get quite warm while playing"; the reviewer suggests keeping it out of its case. At launch, even at lowest settings with MetalFX Performance, it ran near or below 30 fps [S32].
- Media: MrMacRight analysis, published 2023-10-31, 801 s: https://www.youtube.com/watch?v=fLnz8tr7dls. Chapters: 0:43 iOS Tech; 3:08 Touch controls/Controller support; 5:17 Performance; 11:57 Final thoughts [S35]. TouchArcade says the video covers graphics options and presets [S34]. Which chapter shows the menu is not labelled (unverified). Wccftech's gallery shows the menu in images (not opened): https://cdn.wccftech.com/wp-content/uploads/2023/10/Resident-Evil-Village-being-played-on-the-iPhone-15-Pro-Max-2.jpg and ...-3.jpg [S33]. A Datura Plays video is embedded there (not opened): https://www.youtube.com/watch?v=Fzyar232PZ8 [S33].
- Lesson: the full PC menu on a phone is long. Dependencies (MetalFX on disables four rows) are shown by disabling rows. A "recommended" preset is the escape hatch.

### M13 Resident Evil 4 [P] (iPhone 15 Pro)

- GSMArena: no resolution option (about 360p internal, upscaled to 720p, "a quarter" of output). MetalFX is always on, no toggle on iPhone; Mac offers Performance, Quality or off. No presets. The only visual settings are depth of field, motion blur, lens distortion. Locked to 30 fps. Ships with optimized defaults [S36].
- Battery and heat: about 90 minutes used roughly half the battery (iPhone 15 Pro, maximum brightness, Bluetooth controller). The phone "does get a bit warm". One reviewer's test [S36].
- Media: none opened.
- Lesson: hiding every option works only when one fixed config is tuned per device. It leaves the player no recourse for heat.

### M14 Death Stranding Director's Cut [P] (iPhone, iPad, Mac)

- GSMArena: iPhone has a "fixed configuration with no adjustable resolution, frame rate, or graphical settings". Output 1561 x 720; internal about 780 x 360, upsampled with MetalFX temporal. Capped at 30 fps and often struggles. The iPhone settings are "lower than the lowest settings possible on the Mac or PC". Mac has PC-like settings. iPad runs at a higher resolution. No thermal or battery testing [S37].
- Media: none opened.
- Lesson: a phone tier needs its own floor, below desktop "Low". Copying desktop Low to a phone may not be enough.

### M15 Assassin's Creed Mirage [P] (iPhone 15 Pro, M-series iPad)

- GSMArena: "three visual presets, viz. low, medium, and high"; default Medium; they adjust many parameters at once, with no individual settings. Locked to 30 fps. Output is a quarter of display resolution (1278 x 590 on iPhone 15 Pro). All presets keep the same texture resolution; presets change shadow resolution, LOD, object density and decals [S38]. Launch support: A17 iPhones and M1 or later iPads [S39].
- Media: MrMacRight review, published 2024-06-07, 994 s: https://www.youtube.com/watch?v=Xi4ONq4j7cc. Chapters: 0:17 Graphics Quality Modes; 0:50 iPhone 15 Pro; 3:51 M1 iPad Pro; 6:59 M2 iPad Air; 9:24 M4 iPad Pro; 12:50 Why 30 FPS?; 14:20 Final Thoughts [S40]. Only the chapter titles were read, not the video.
- Lesson: presets that move shadows, LOD and density but leave textures alone are easy to explain in a one-line preview per preset.

### M16 Cyberpunk 2077 Ultimate Edition [P] (Mac)

- CD PROJEKT RED press (2026-06-08): the game includes the dynamic "For this Mac" graphics preset. It "automatically adjusts graphics options and resolution based on available hardware", not one config for all Macs [S41]. The 2024-10-30 launch release lists path tracing and frame generation [S42].
- Menu layout and MetalFX labels: not found in the opened pages (the Tom's Guide article returned no body). Unverified.
- Media: none opened.
- Lesson: name a computed preset after the device ("For this iPhone", "For this iPad"). It is a preset the game fills in, not a fixed tier.

### M17 Assassin's Creed Shadows [P] (Mac, PC)

- Ubisoft tech Q&A (game-wide, PC-centred): "5 distinct scalability presets (Low, Medium, High, Very High, Ultra High)", each configuring about 20 options that can be changed one by one. An "in-game benchmark to test and tweak scalability parameters". A performance overlay with FPS and GPU use. Upscalers combine with dynamic resolution "to target a given framerate". The Q&A does not mention MetalFX or auto-detect, and gives Mac requirements only as an image [S43].
- App Store (Mac) minimum spec: "720p at 30 FPS" using dynamic resolution and MetalFX, on M1 Max or M2 Pro, 16 GB RAM; ray tracing "Software selective" [S44].
- Apple Developer news (2025-03-04) names real-time ray tracing on M3 and M4; it does not discuss settings [S45].
- Media: none opened.
- Lesson: state the target (resolution, fps, upscaler) per minimum chip. Offer a benchmark and a live perf overlay as settings parts.

### M18 No Man's Sky [P] (Mac now; iPhone and iPad launch 2026-10-28)

- Hello Games (2023-06-01): "Consistent performance across the full range of Macs" through MetalFX Upscaling, and "console quality graphics whilst maintaining battery life". No graphics options listed [S46].
- iDrop News (2026-10-12): iPhone and iPad launch on October 28 (the article says "later this month"). Needs A12 Bionic or later and iOS 26 or later. Hello Games says it put "tons of work into battery life and thermodynamics". No presets, fps or resolution given [S47]. The nomanssky.com iOS page appeared only in search results.
- Media: none opened.
- Lesson: the iOS release lands four days after this research. Re-check its in-app settings as a primary reference after release.

### Not sourced (skipped, per the brief)

Fortnite mobile, Zenless Zone Zero, Delta Force, Arknights: Endfield, Asphalt Legends, Oceanhorn 2, Myst and Riven, Baldur's Gate 3 Mac, Stray Mac. Their pages either failed to open (403, redirect, empty body) or showed only in search results. See the "not opened" table. No claim is made about them.

## B. Platform guidance and APIs

Each item: the short rule, the URL, then a "use" line (my inference).

### B1 The target device: iPhone Duo

- URL: https://www.apple.com/iphone-duo/specs/ [S49]
- Outer display: 5.4-inch OLED, 1398 x 2034 pixels, 460 ppi. Inner display: 7.6-inch folding OLED, 1878 x 2670 pixels, 430 ppi. Both use ProMotion up to 120 Hz. Chip: A20 Pro, 6-core CPU, 7-core GPU with Neural Accelerators, hardware ray tracing [S49].
- The page lists no point sizes, no gaming data and no thermal data [S49]. So the brief's point sizes (678 x 466 outer, 951 x 669 inner) are not confirmed by this page (unverified).
- My arithmetic from the page: inner = 5.01 million pixels, outer = 2.84 million, so the inner screen has 1.76 times the pixels of the outer one. The brief's inner points (951 x 669) against the page's pixels give a scale of 2.807 on both axes, not a whole number. Size render targets from pixels, not from points.
- Use: the inner display is the heavy case. A render scale or an upscaler should be defined against native pixels.

### B2 Apple Human Interface Guidelines

- URLs (data feed of the HIG pages): https://developer.apple.com/tutorials/data/design/human-interface-guidelines/designing-for-games.json [S50] and https://developer.apple.com/tutorials/data/design/human-interface-guidelines/settings.json [S51]. The normal HTML page https://developer.apple.com/design/human-interface-guidelines/designing-for-games returned only its title when fetched, so it supports no claim.
- Designing for games [S50]: "Provide great default settings." Use player device information "to choose the best defaults", for example the resolution that makes graphics look great. Games must "look good and behave well at various aspect ratios" (16:10, 19.5:9, 4:3). "Avoid fixed layouts as much as possible." The page has nothing on graphics presets, frame rate, performance, battery, thermal state, Game Mode or a pause menu (per the extract).
- Settings [S51]: aim for "default settings that give the best experience to the largest number of people". Example given: "automatically maximize performance for the device your game is running on", instead of asking the player after launch. "Minimize the number of settings you offer." Open settings with Esc in games. Put task-specific options where the task is, not in the settings area.
- Use: Apple asks for few settings and a good default. That fits "preset first, Custom second". The default preset should be chosen by the game from the device.

### B3 MetalFX (upscaling and frame interpolation)

- URLs: https://developer.apple.com/documentation/metalfx.md [S52]; https://developer.apple.com/videos/play/wwdc2022-10103 [S53]; https://developer.apple.com/videos/play/wwdc2023/10123/ [S54]; https://developer.apple.com/videos/play/wwdc2025/211/ [S55]
- Framework page [S52]: two ways to upscale. Temporal (needs colour, depth and motion) or spatial (needs only a colour texture). Available from iOS 16, iPadOS 16, macOS 13. The scaler "takes time to initialize", so create it at launch or when the display resolution changes. A diagram on the page shows the MetalFX path taking about half the time of native rendering. That is an illustration, not a benchmark.
- WWDC22 [S53]: the trade-off is stated as "the tradeoff is a lower rendering quality". Spatial is the cheaper, easier option and runs after tone mapping. Temporal gives the best quality, "comparable" to native, but needs jitter, motion and depth. Creating scalers is expensive, so do it only at start or on a display-resolution change. Reset history on scene cuts to avoid ghosting. The session does not say how to expose presets or scale sliders (per the extract).
- WWDC23 "Bring your game to Mac" part 1 [S54]: recommends playing with the Metal Performance HUD on, and says MetalFX upscaling "will boost your game's performance even further". The extract has no advice on settings menus, resolution choices, windowed or full screen, or Game Mode.
- WWDC25 session 211 [S55]: "MetalFX frame interpolation" arrives on all Apple platforms. Inputs: two rendered frames, motion vectors, depth. Place it after tone mapping. Upscaling and interpolation can run in one pipeline. "Try to have at minimum 30 frames per second before interpolation." UI note: a composited UI cannot be unblended perfectly, so render the UI offscreen or every frame for best results. The extract gives no latency or power numbers.
- Use: if a render-scale slider drives MetalFX, do not rebuild the scaler on every drag step (inference from the "only at start or on resolution change" rule). Interpolation needs a 30 fps floor, so it is not a fix for a game that runs below 30.

### B4 Refresh rate: ProMotion, the plist key, Low Power Mode

- URLs: https://developer.apple.com/documentation/quartzcore/optimizing-iphone-and-ipad-apps-to-support-promotion-displays.md [S56]; https://developer.apple.com/documentation/bundleresources/information-property-list/cadisableminimumframedurationonphone.md [S57]; https://developer.apple.com/videos/play/wwdc2021/10147/ [S58]
- Range [S56]: ProMotion runs 10 Hz to 120 Hz on supported iPhones and 24 Hz to 120 Hz on iPad Pro. The device list names iPhone Air, iPhone 17 and later, and iPhone 13 Pro and later. The Duo is not named on that page (the Duo spec page does list ProMotion [S49]).
- Rates the iPhone can present [S56]: 120, 80, 60, 48, 40, 30, 24, 20, 16, 15, 12, 10 Hz. iPad Pro: 120, 60, 40, 30, 24 Hz. So 120, 60, 40 and 30 exist on both.
- Power [S56]: "Higher refresh rates can result in significant power consumption." Advice: choose the lowest frame rate that gives the visual flow you want. The hint table lists "first-person, full-motion gaming" at 80-120 Hz, preferred 120, with the note "Use sparingly to minimize power consumption."
- Games [S56]: in iOS 15 and later the system gives games "special priority to 30Hz and 60Hz" when the game sets a frame-rate range using those values.
- Plist key [S57][S56]: `CADisableMinimumFrameDurationOnPhone` (iOS 15+). With YES the app can request any rate the display supports. With NO, rates above the system default are unavailable. Without it Core Animation does not reach rates above 60 Hz on iPhone. iPad Pro does not need it.
- Throttles [S56][S58]: the system disables faster refresh rates in Low Power Mode or when the device gets hot. WWDC21 adds a Limit Frame Rate switch in Accessibility that caps at 60 Hz, and says Low Power Mode enforces the 60 Hz cap (stated for iPadOS 15). It warns that UIScreen always reports 120 Hz on ProMotion; the CADisplayLink duration shows the real shortest interval [S58].
- Feedback loop [S56]: "When a device overheats, the system may respond by reducing the frame rate." Also: "maintaining a consistent drawing quality" avoids bad power states, and adjusting render quality from refresh rate inside the display-link callback "can throttle the GPU". Use separate code paths per frame rate and test on devices.
- Use: offer fps caps that are real display steps (30, 40, 60, 120). Read the live cap and show why 120 is unavailable. Do not tie quality to measured fps without hysteresis.

### B5 Thermal state and Low Power Mode

- URLs: https://developer.apple.com/documentation/foundation/processinfo/thermalstate-swift.enum.md [S59]; https://developer.apple.com/documentation/foundation/processinfo/islowpowermodeenabled.md [S60]; https://developer.apple.com/documentation/xcode/responding-to-power-notifications.md [S61]; https://developer-mdn.apple.com/library/archive/documentation/Performance/Conceptual/power_efficiency_guidelines_osx/RespondToThermalStateChanges.html [S62]; https://developer.apple.com/library/archive/documentation/Performance/Conceptual/EnergyGuide-iOS/LowPowerMode.html [S63]
- Thermal states [S59]: nominal ("within normal limits"), fair ("slightly elevated"), serious ("high"), critical (the system is badly slowed and "the device needs to cool down"). Available on iOS and iPadOS 11 and later.
- Low Power Mode [S60]: reduces CPU and GPU performance, screen brightness and background work. Query `isLowPowerModeEnabled` any time and watch `NSProcessInfoPowerStateDidChange`. iOS 9+, macOS 12+.
- Actions [S61]: when Low Power Mode is on, pause optional work, reduce display updates, minimise animations. Register for thermal-state change notices and adjust by value: nominal means use everything. The saved extract is cut off after "fair" (agent notes: serious and critical ask for more cuts).
- Frame-rate rule [S62]: this is the older macOS power guide. At "serious", apps should "Reduce frame rates." and "Use lower-quality visual effects." At "critical", cut CPU, GPU and I/O to the minimum.
- iOS guide [S63] (page last updated 2016): in Low Power Mode an app "could reduce the use of animations, lower frame rates" and more.
- Use: a "Battery and heat" group with one switch such as "Adapt to heat and Low Power Mode". It should show its state when active ("Cooling down: 30 fps cap").

### B6 Game Mode and Metal HUD

- URLs: https://developer.apple.com/documentation/bundleresources/information-property-list/lssupportsgamemode.md [S64]; https://support.apple.com/105118 [S65]; https://developer.apple.com/documentation/xcode/monitoring-your-metal-apps-graphics-performance.md [S66]
- Key `LSSupportsGameMode` (iOS and iPadOS 18.6+, macOS 26+) [S64]: Game Mode "minimizes background activity for smoother gameplay and more consistent frame rates". It turns on automatically when you launch a game. If the key is missing, Game Mode might not turn on.
- Support page [S65]: on iPhone and iPad (iOS or iPadOS 18+) it gives the game "highest priority access to your CPU and GPU" and doubles the Bluetooth sampling rate. Mac needs Apple silicon, macOS Sonoma 14, and a game that supports full screen. The player switches it in the Game Overlay (Controls > Game Mode). The page gives no frame-rate numbers.
- Metal Performance HUD [S66]: an overlay showing the Metal device, resolution, present mode, memory and whether Game Mode is on, plus average FPS, GPU time and frame interval with a chart of the last 120 frames. It is enabled in the Xcode scheme diagnostics. The agent's notes add `MTL_HUD_ENABLED=1` and a Developer settings switch (agent notes; the saved extract is cut off before that text).
- Use: Game Mode is a system switch, so the game does not need its own row. Use the HUD to check frame pacing on the Duo.

### B7 Android: Game Mode, interventions, FPS throttling

- URLs: https://developer.android.com/games/optimize/adpf/gamemode/about-API-and-interventions [S67]; https://developer.android.com/games/optimize/adpf/gamemode/gamemode-api [S68]; https://developer.android.com/games/optimize/adpf/gamemode/gamemode-interventions [S69]; https://developer.android.com/games/optimize/adpf/gamemode/fps-throttling [S70]
- Purpose [S67]: the API lets a game optimise "for the best performance or battery life" based on the player's pick. Available from Android 12 on selected devices.
- Values [S68]: UNSUPPORTED, STANDARD, PERFORMANCE ("in exchange for reduced battery life and fidelity"), BATTERY ("longest possible battery life in exchange for reduced fidelity or frame rates"). BATTERY here is game-specific and does not change system Battery Saver. The player switches it in the Game Dashboard (Pixel) or an OEM app.
- Best practice [S68]: for top frame rates, accept small cuts in fidelity. For battery, pick a lower refresh rate (30 Hz or 60 Hz) and pace frames to it. Even casual games should support a battery saver mode that lowers peak frame rates. The advice to query the mode again on resume is in the agent notes only.
- Interventions [S69]: OEM-set optimisations for games no longer updated. Backbuffer resize can cut "up to 30% of GPU and 10% of overall system power usage". "A 90% (0.9) resize is almost negligible, whereas 50% (0.5) is significant." Keep resize at 70% or more; it needs an app restart. A game can opt out with `allowGameDownscaling` and `allowGameFpsOverride` set to false.
- FPS throttling [S70]: Android 13+. It can only lower the frame rate. Advised rates are divisors of the display's maximum: 60 Hz gives 60 and 30; 90 Hz gives 90, 45, 30; 120 Hz gives 120, 60, 40, 30. If an app also paces its own frames, the lower target wins.
- Use: name the intent modes after the platform (Performance, Battery) and map them to the same three presets used on Apple devices.

### B8 Android: thermal headroom and quality steps

- URLs: https://developer.android.com/games/optimize/adpf/thermal [S71]; https://developer.android.com/games/optimize/adpf/best-practices-adpf [S72]; https://android-developers.googleblog.com/2023/09/kakao-games-increased-fps-stability-through-android-adapatability.html [S73]
- Thermal headroom [S71]: a value from 0.0 (no throttling) to 1.0 (heavy throttling). "You shouldn't call it more than once every 10 seconds." If a workload is too heavy, the game should "decrease the workload to a sustainable level" by lowering frame rate or fidelity. A status callback can replace polling.
- Best practices [S72]: default scaling targets generic Low, Medium, High presets. A game with its own quality options must wire ADPF to its own settings. "Avoid drastic preset changes." Change shadows, resolution and particles one at a time ("Decouple settings"), in "small, incremental adjustments", and fade changes in over a few frames ("Smooth transitions").
- Kakao case [S73]: a six-step quality sequence (resolution, texture, shadow, effects) lifted FPS stability from 75% to 96%. Throttling began about 16 minutes in before, and about 22 minutes in after. Players who want maximum quality can turn adaptive performance off.
- Use: if FrontRooms adapts quality at run time, step individual settings in small moves and give the player an off switch.

### B9 Android: foldables and large screens

- URLs: https://developer.android.com/games/engines/unity/unity-large-screen [S74]; https://developer.android.com/large-screens/gallery/games [S75]
- Unity guide [S74]: "your game must be resizable" so it handles portrait, landscape, multi-window and folded or unfolded states. Otherwise the platform letterboxes it. Adjust the camera and canvas aspect ratio to fit, anchor the UI, respect the safe area, and use a fold-aware layout for half-open postures.
- The Unity page does not cover render resolution, render scale or graphics quality for large screens. The large-screens games gallery [S75] is only an index. So no official rule was found for render scale on a foldable's inner and outer display (gap).
- Use: the Duo fold changes the window and aspect at run time. Rebuild any resolution-dependent render targets on that change, and re-evaluate the preset.

## C. Frame rate, resolution and battery: what is published

No official or Digital Foundry-level source in the opened material gives a number for the power cost of 120 fps against 60 fps against 30 fps on a phone. No Digital Foundry page was opened. The numbers that do exist are below.

| # | Statement | Number | Source | Scope and quality |
|---|-----------|--------|--------|-------------------|
| C1 | Android Game Mode backbuffer resize (lower render resolution) | up to 30% less GPU load and 10% less overall system power; "results can vary" | [S69] | official Google doc; OEM intervention; base resolution not stated |
| C2 | Android FPS throttling | up to 50% less GPU power and 20% less system power | [S70] | official Google doc; start and target rates for the figure are not in the extract |
| C3 | Resize steps that matter | 0.9 "almost negligible"; 0.5 "significant"; keep at 70% or more on Android 12 | [S69] | official Google doc |
| C4 | Apple ProMotion refresh rates | no number; "Higher refresh rates can result in significant power consumption." | [S56] | official Apple doc |
| C5 | Apple ProMotion adaptivity | no number; the display adapts to content and so "reduces its power consumption" | [S58] | official Apple WWDC21 session; the page states it gives no numbers |
| C6 | Apple Low Power Mode and heat | reduces CPU and GPU performance; faster refresh rates are disabled in Low Power Mode or when hot | [S60][S56] | official Apple docs; no numbers |
| C7 | Wuthering Waves Frame Generation | 60 fps with Frame Generation "consumes less power" than 60 fps without; Vulkan "improves ... power consumption" | [S2] | game maker's patch notes; no numbers |
| C8 | Feral battery order | in order of impact: screen brightness, internet, audio volume, battery health | [S26] | game publisher FAQ; no numbers |
| C9 | Resident Evil 4 on iPhone 15 Pro | about 90 minutes used roughly half the battery at maximum brightness, 30 fps lock | [S36] | one reviewer's test; not controlled |
| C10 | iPhone Duo displays | video playback about 44 h (outer) against 31 h (inner); typical use about 24 h | [S49] | Apple spec page; display cost only, no gaming figure. 31 / 44 = 0.70 is my arithmetic |
| C11 | MetalFX | diagram shows the MetalFX path at about half the frame time of native | [S52] | Apple illustration, not a benchmark, not power |
| C12 | Kakao Games "Ares" | FPS stability 75% to 96%; throttling began about 16 min in, then about 22 min | [S73] | Google case study; thermal stability, not power |

Reading: official numbers exist only for Android resolution and fps interventions. Apple states the direction (higher refresh costs power; hot or Low Power Mode removes 120 Hz) but gives no figure.

## D. Synthesis

### Recurring mobile patterns

1. Presets first, details second. Almost every mobile game opens on a preset, and many add a Custom block: Genshin M1, Star Rail M2, Minecraft M6, Diablo Immortal M8, Call of Duty: Mobile M5. Typical ladders are 3 steps (Low / Medium / High: M5, M15) or 5 steps (M17). The top tier is often gated by chip: Wuthering Waves "High Quality" M3, Diablo Immortal "Ultra" M8. The only ports with no choice at all are RE4 M13 and Death Stranding M14.
2. Two naming families. Quality ladders (M5, M15, M17) and intent names: Graphics / Battery Saver / Performance (M10); Energy Saving / Default / High Performance / High Definition (M9); Favor Performance / Favor Visuals (M6); Android PERFORMANCE and BATTERY (B7). Intent names carry more battery meaning. A third kind is computed by the game: "For this Mac" (M16), a "Recommended" marker (M2), Automatic (M7).
3. Defaults come from the device. Alien: Isolation tunes "per-device" and defaults to the best-looking preset (M10). Mirage defaults to Medium (M15). Minecraft's Vibrant Visuals is on by default only on strong GPUs (M6). Apple's HIG says the same (B2).
4. Frame rate is its own control with few steps. 30 / 60 (M1, M2). Named tiers up to 90 and 120 (M4). Locked at 30 on ports (M13, M14, M15). Android advises divisors of the display rate (B7), and Apple's real steps are 120, 60, 40, 30 on both iPhone and iPad Pro (B4). 120 is gated by device and display (M4, B4). House names such as "Ultra Extreme" (M4) need a glossary.
5. Render resolution is shown as named steps or hidden. Low / Medium / High in Genshin and Star Rail (M1, M2) and Diablo Immortal (M8). Ports show pixel sizes (M12) or fix it at about a quarter of the display (M13, M15). Numeric multipliers (0.6 to 1.6) appear only on PC (M3, [S48]) and in Android OEM resize (B7). No mobile case in the opened material shows a percent slider for render scale (unverified beyond the opened pages).
6. Upscaler labels. Apple ports use the system name with two modes, "MetalFX Performance / Quality" (M12), or hide it (M13, M14). Minecraft names the method, "TAAU / Bilinear" (M6). Wuthering Waves names the feature, "Frame Generation" (M3). Dependent rows are disabled when an upscaler is on (M12).
7. Heat and battery wording is mostly indirect. (a) A live load meter: Genshin "Smooth / Overclocked" (M1), Diablo "Device Load" (M8). (b) A preset description: Battery Saver "alleviate ... overheating" (M10), Sky "preserve ... battery" (M9). (c) A cost warning on a heavy option: "may occasionally cause performance issues" (M3), a low-end warning (M6). (d) Auto-adapt: PUBG auto-buffering (M4), Android thermal guidance (B8), Apple thermal-state guidance (B5). No opened case shows a literal "may cause overheating" string in a menu (unverified).
8. Unsupported options are handled three ways: hidden (M4), shown but locked (M8), or capped (the Minecraft slider max, M6). Frame-rate tiers can also be gated by Low Power Mode and heat (B4).
9. Where settings sit: a pause menu or in-game menu (M1, M7, M10), a gear icon (M5, M9), Settings > Graphics (M2, M3). No opened case shows a graphics prompt at first launch (unverified). Apple's HIG says choose defaults instead of asking after launch (B2).
10. Restarts and dependencies are called out: restart notice (M1), Vulkan restart and "after enabling Vulkan" order (M3), mode switching off by default (M6), rows disabled by MetalFX (M12). Android downscaling also needs a restart (B7).
11. Help inside settings: preview buttons (M3), hint text (M2), a benchmark and overlay (M17), a video-RAM readout (M12).
12. Foldables: the platform guidance is about resizing, aspect and fold-aware UI. Nothing found on render scale for inner and outer displays (B9).

### Candidate directions for FrontRooms (inference, not decisions)

- One Quality control with named steps and a Custom block. Pick the default from the device, and mark it "Recommended" (M2, M16, B2).
- Use intent names that tie to battery: for example Battery, Balanced, Quality. Match Android modes (M9, M10, B7).
- A separate frame-rate segmented control with real display steps (30, 40, 60, 120). Disable 120 with a reason when Low Power Mode or heat removes it (B4).
- Render scale as a few named steps backed by a number, with the pixel size shown. Size it from native pixels. Do not rebuild an upscaler on every slider tick (B1, B3).
- A "Battery and heat" group with an adapt-to-heat switch and a visible state chip (B5, B8). Step individual settings in small moves and allow the player to turn it off (B8).
- A live frame-time or load gauge beside the rows (M1, M8, M12).
- Mark rows that need a restart or depend on another row (M1, M3, M12).
- Keep the entry in the pause menu. Keep the visible list short; hide detail under Custom (B2).
- Check the iOS No Man's Sky settings after its 2026-10-28 launch (M18).

### Gaps and unverified

- Widget types (slider, segmented control, toggle) are rarely named in the sources. Only these are: Roblox slider and mode line (M7), Minecraft dropdown and render-distance slider (M6), PUBG "Frame Rate" slider (M4, third-party), Sky bar control (M9), Genshin load bar (M1), Star Rail switch (M2), Wuthering Waves preview button (M3). No menu screenshot was opened.
- Not sourced: Fortnite mobile, Zenless Zone Zero, Delta Force, Arknights: Endfield, Asphalt Legends, Oceanhorn 2, Myst and Riven, Baldur's Gate 3 Mac, Stray Mac. Epic's Fortnite help pages returned 403 to the fetch tool and showed no text in the browser pane.
- Cyberpunk, Assassin's Creed Shadows and No Man's Sky Mac menus: layout and MetalFX labels not found in the opened pages.
- No source for power cost of 30 vs 60 vs 120 fps on a phone (section C).
- No official render-scale advice for foldable inner and outer displays (B9).
- PUBG, Call of Duty: Mobile and Wuthering Waves frame-rate names on mobile come from older or third-party pages and may have changed.
- The brief's Duo point sizes are not on Apple's spec page (B1).
- Fetch results were read through extracts; some pages were cut off before the end.

## Sources

All rows below were opened. Accessed date is 2026-10-24 for every row. The usage note is the same for every row.

| # | URL | What it shows | Owner / publisher | Accessed | Usage note |
|---|-----|---------------|-------------------|----------|------------|
| S1 | https://wutheringwaves.kurogames.com/en/main/news/detail/4447 | Patch notes 3.2: mobile High Quality tier, chip gates, preview buttons, Render Distance (PC) | Kuro Games (Wuthering Waves) | 2026-10-24 | © Kuro Games (Wuthering Waves); link only, reference use |
| S2 | https://wutheringwaves.kurogames.com/en/main/news/detail/5571 | Patch notes (text says 3.7 maintenance): Vulkan, Frame Generation, power statement | Kuro Games (Wuthering Waves) | 2026-10-24 | © Kuro Games (Wuthering Waves); link only, reference use |
| S3 | https://www.androidauthority.com/genshin-impact-boost-fps-3173923/ | Genshin guide 2024-05-30: mobile Custom options, Current Performance Load | Android Authority | 2026-10-24 | © Android Authority; link only, reference use |
| S4 | https://www.sportskeeda.com/esports/best-genshin-impact-settings-high-fps-low-end-devices | Genshin low-end guide: Paimon Menu path, Smooth bar, restart message, PC FPS | Sportskeeda | 2026-10-24 | © Sportskeeda; link only, reference use |
| S5 | https://www.hoyolab.com/article/28903107 | Player post 2024/05/21: phone heat at 60 FPS (anecdote) | HoYoLAB (player content) | 2026-10-24 | © HoYoLAB (player content); link only, reference use |
| S6 | https://hsr.hoyoverse.com/en-us/news/165195 | Version 4.4 notes: Recommended marker on Graphics Quality, Android hint text | HoYoverse (Honkai: Star Rail) | 2026-10-24 | © HoYoverse (Honkai: Star Rail); link only, reference use |
| S7 | https://hsr.hoyoverse.com/en-us/news/161539 | Version 3.8 notes: Pre-compile Shaders on Login switch | HoYoverse (Honkai: Star Rail) | 2026-10-24 | © HoYoverse (Honkai: Star Rail); link only, reference use |
| S8 | https://hsr.hoyoverse.com/en-us/news/124161 | Version 2.3 notes: Special Effect Mode, FPS setting bug | HoYoverse (Honkai: Star Rail) | 2026-10-24 | © HoYoverse (Honkai: Star Rail); link only, reference use |
| S9 | https://www.sportskeeda.com/gaming-tech/best-honkai-star-rail-settings-android-devices | Star Rail Android settings list by device class | Sportskeeda | 2026-10-24 | © Sportskeeda; link only, reference use |
| S10 | https://www.pubgmobile.com/en-US/m/news_detail/webplat/info/news_version3/35372/35482/35483/35484/35485/m20570/201912/839688.shtml | Patch notes 0.16.0: auto-buffering setting | PUBG MOBILE official site | 2026-10-24 | © PUBG MOBILE official site; link only, reference use |
| S11 | https://www.pubgmobile.com/webplat/info/news_version3/35372/60662/60663/60724/60725/60765/m22521/202107/894225.shtml | Patch notes 1.5: 90 FPS in Smooth graphics on some devices | PUBG MOBILE official site | 2026-10-24 | © PUBG MOBILE official site; link only, reference use |
| S12 | https://www.sammobile.com/?p=15061687 | PUBG Mobile 3.2 adds 120 fps on select Samsung devices (updated 2024-05-15) | SamMobile | 2026-10-24 | © SamMobile; link only, reference use |
| S13 | https://www.esports.net/wiki/guides/pubg-mobile-120-fps/ | PUBG 120 FPS guide: Ultra Extreme, Smooth + Modest setup, device list | esports.net | 2026-10-24 | © esports.net; link only, reference use |
| S14 | https://www.pockettactics.com/pubg-mobile/120-fps | PUBG 3.2 120 FPS news; frame rate must match display refresh | Pocket Tactics | 2026-10-24 | © Pocket Tactics; link only, reference use |
| S15 | https://www.sportskeeda.com/esports/pubg-mobile-graphics-settings-better-fps-less-lag | PUBG settings guide 2021-09-05: Smooth, High/Ultra/Extreme, Style, Auto-adjust | Sportskeeda | 2026-10-24 | © Sportskeeda; link only, reference use |
| S16 | https://blog.activision.com/ca/en/call-of-duty/2019-10/Getting-a-Grip-on-the-Call-of-Duty-Mobile-Controls | CoD Mobile Graphics submenu: quality and frame rate Low/Medium/High, toggles, BR style | Activision | 2026-10-24 | © Activision; link only, reference use |
| S17 | https://blog.activision.com/call-of-duty/2019-09/Call-of-Duty-Mobile-Boot-Camp-Part-1-Getting-Started-in-the-Game | CoD Mobile gear icon opens Settings | Activision | 2026-10-24 | © Activision; link only, reference use |
| S18 | https://minecraft.wiki/w/Graphics_settings | Bedrock graphics modes, render distance ranges, mode-switching limits | Minecraft Wiki (community) | 2026-10-24 | © Minecraft Wiki (community); link only, reference use |
| S19 | https://minecraft.wiki/w/Vibrant_Visuals | Vibrant Visuals: mobile device support, Favor Performance/Visuals, TAAU/Bilinear, render distance slider | Minecraft Wiki (community) | 2026-10-24 | © Minecraft Wiki (community); link only, reference use |
| S20 | https://en.help.roblox.com/hc/en-us/articles/203314310-Graphics-Quality | Graphics Mode Automatic/Manual and Graphics Quality slider | Roblox | 2026-10-24 | © Roblox; link only, reference use |
| S21 | https://en.help.roblox.com/hc/en-us/articles/203314150-How-to-Reduce-Lag-and-Speed-Up-Play | Lower graphics level steps; Graphics Quality - Auto toggle | Roblox | 2026-10-24 | © Roblox; link only, reference use |
| S22 | https://create.roblox.com/docs/reference/engine/enums/SavedQualitySetting | Engine enum: Automatic and QualityLevel1 to QualityLevel10 | Roblox | 2026-10-24 | © Roblox; link only, reference use |
| S23 | https://digitaltrends.com/?p=3115237 | Diablo Immortal on Galaxy A53: 30 fps lock, locked Ultra, Quality Selection, Device Load | Digital Trends | 2026-10-24 | © Digital Trends; link only, reference use |
| S24 | https://sky-children-of-the-light.fandom.com/wiki/Menus_and_Controls | Sky Graphics menu: 0-3 bar modes, battery and heat note (Aug 2024 screenshot) | Fandom wiki (community) | 2026-10-24 | © Fandom wiki (community); link only, reference use |
| S25 | https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_modes | Alien: Isolation mobile FAQ: Graphics, Battery Saver, Performance presets | Feral Interactive | 2026-10-24 | © Feral Interactive; link only, reference use |
| S26 | https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_battery_usage | Feral battery FAQ: drain causes in order of impact | Feral Interactive | 2026-10-24 | © Feral Interactive; link only, reference use |
| S27 | https://www.feralinteractive.com/en/support/ios/alienisolation/#faqs/ios_new_options | Alien: Isolation mobile new options (aim input, aim assist, toggles) | Feral Interactive | 2026-10-24 | © Feral Interactive; link only, reference use |
| S28 | https://toucharcade.com/2021/12/16/alien-isolation-mobile-review-frame-rate-resolution-performance-battery-saver-ipad-pro-iphone-11-settings/ | Alien: Isolation mobile review 2021-12-16: three graphics modes, pacing | TouchArcade | 2026-10-24 | © TouchArcade; link only, reference use |
| S29 | https://www.youtube.com/watch?v=DboBLkA5WRw | Alien: Isolation iOS/Android trailer (page title only; settings not confirmed) | YouTube (uploader not read) | 2026-10-24 | © YouTube (uploader not read); link only, reference use |
| S30 | https://www.feralinteractive.com/en/support/ios/gridlegends/ | GRID Legends iOS FAQ index (no graphics-settings entry) | Feral Interactive | 2026-10-24 | © Feral Interactive; link only, reference use |
| S31 | https://www.feralinteractive.com/en/support/ios/gridlegends/#faqs/ios_known_issue_frame_pacing | GRID Legends known issue: Home bar frame pacing, workarounds | Feral Interactive | 2026-10-24 | © Feral Interactive; link only, reference use |
| S32 | https://m.gsmarena.com/resident_evil_village_for_iphone_review-news-60519.php | RE Village iPhone review: full option list, MetalFX modes, heat | GSMArena | 2026-10-24 | © GSMArena; link only, reference use |
| S33 | https://wccftech.com/resident-evil-village-for-iphone-15-pro-offers-pc-graphics-settings/ | RE Village iPhone: resolutions, MetalFX modes, VRAM readout; image and video links | Wccftech | 2026-10-24 | © Wccftech; link only, reference use |
| S34 | https://toucharcade.com/2023/10/31/resident-evil-village-iphone-15-pro-resolution-frame-rate-performance-video-analysis-mrmacright-youtube | RE Village analysis video news post 2023-10-31 | TouchArcade | 2026-10-24 | © TouchArcade; link only, reference use |
| S35 | https://www.youtube.com/watch?v=fLnz8tr7dls | MrMacRight RE Village iPhone 15 Pro video; chapter list (2023-10-31) | MrMacRight (YouTube) | 2026-10-24 | © MrMacRight (YouTube); link only, reference use |
| S36 | https://www.gsmarena.com/resident_evil_4_for_iphone_review-news-61067.php | RE4 iPhone review: no options, 30 fps, MetalFX always on, battery and heat | GSMArena | 2026-10-24 | © GSMArena; link only, reference use |
| S37 | https://www.gsmarena.com/death_stranding_directors_cut_for_iphone_review-news-61645.php | Death Stranding DC iPhone review: fixed config, 720p, MetalFX temporal | GSMArena | 2026-10-24 | © GSMArena; link only, reference use |
| S38 | https://www.gsmarena.com/assassins_creed_mirage_for_iphone_review-news-63311.php | AC Mirage iPhone review: three presets, default Medium, 30 fps | GSMArena | 2026-10-24 | © GSMArena; link only, reference use |
| S39 | https://toucharcade.com/2024/06/07/assassins-creed-mirage-iphone-15-pro-resolution-frame-rate-graphics-modes-ipad-m4-pro-testing-video-gameplay/ | AC Mirage performance video news post 2024-06-07; device support | TouchArcade | 2026-10-24 | © TouchArcade; link only, reference use |
| S40 | https://www.youtube.com/watch?v=Xi4ONq4j7cc | MrMacRight AC Mirage iPhone and iPad video; chapter list (2024-06-07) | MrMacRight (YouTube) | 2026-10-24 | © MrMacRight (YouTube); link only, reference use |
| S41 | https://press.cdprojektred.com/en/news/1829/cyberpunk-2077-ultimate-edition-secures-win-at-apple-design-awards-2026 | Cyberpunk Mac press 2026-06-08: dynamic For this Mac preset | CD PROJEKT RED | 2026-10-24 | © CD PROJEKT RED; link only, reference use |
| S42 | https://press.cdprojektred.com/en/news/1672/cd-projekt-red-announces-cyberpunk-2077-ultimate-edition-arriving-on-mac | Cyberpunk Mac launch press 2024-10-30 | CD PROJEKT RED | 2026-10-24 | © CD PROJEKT RED; link only, reference use |
| S43 | https://www.ubisoft.com/en-us/game/assassins-creed/news/4XbPPtFyQEtIMWrA9xVDmZ/assassins-creed-shadows-tech-qa | AC Shadows tech Q&A: five presets, benchmark, overlay, upscalers | Ubisoft | 2026-10-24 | © Ubisoft; link only, reference use |
| S44 | https://apps.apple.com/us/app/assassins-creed-shadows/id6497794841?mt=12 | AC Shadows Mac App Store listing: minimum spec 720p at 30 FPS with MetalFX | Apple App Store (listing by the publisher) | 2026-10-24 | © Apple App Store (listing by the publisher); link only, reference use |
| S45 | https://developer.apple.com/news/?id=q2zte70j | Apple Developer news 2025-03-04: AC Shadows on Mac, ray tracing | Apple | 2026-10-24 | © Apple; link only, reference use |
| S46 | https://www.nomanssky.com/2023/06/no-mans-sky-on-mac | No Man's Sky on Mac 2023-06-01: MetalFX, battery claim | Hello Games | 2026-10-24 | © Hello Games; link only, reference use |
| S47 | https://www.idropnews.com/news/no-mans-sky-iphone-ipad-release-date/269495/ | No Man's Sky iPhone and iPad news 2026-10-12: launch 2026-10-28, battery claim | iDrop News | 2026-10-24 | © iDrop News; link only, reference use |
| S48 | https://www.invenglobal.com/articles/21520/just-follow-these-settings-a-guide-to-visual-frame-rate-changes-by-graphics-option | Wuthering Waves PC guide: Rendering Precision 0.6 to 1.6, DLSS (PC only) | Inven Global | 2026-10-24 | © Inven Global; link only, reference use |
| S49 | https://www.apple.com/iphone-duo/specs/ | iPhone Duo displays, chip, battery, ProMotion | Apple | 2026-10-24 | © Apple; link only, reference use |
| S50 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/designing-for-games.json | HIG Designing for games (data feed): defaults, aspect ratios, menus | Apple | 2026-10-24 | © Apple; link only, reference use |
| S51 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/settings.json | HIG Settings (data feed): defaults, minimize settings, Esc in games | Apple | 2026-10-24 | © Apple; link only, reference use |
| S52 | https://developer.apple.com/documentation/metalfx.md | MetalFX framework: temporal and spatial scalers, availability | Apple | 2026-10-24 | © Apple; link only, reference use |
| S53 | https://developer.apple.com/videos/play/wwdc2022-10103 | WWDC22 session 10103: MetalFX upscaling trade-offs | Apple | 2026-10-24 | © Apple; link only, reference use |
| S54 | https://developer.apple.com/videos/play/wwdc2023/10123/ | WWDC23 session 10123: Bring your game to Mac part 1 | Apple | 2026-10-24 | © Apple; link only, reference use |
| S55 | https://developer.apple.com/videos/play/wwdc2025/211/ | WWDC25 session 211: MetalFX frame interpolation | Apple | 2026-10-24 | © Apple; link only, reference use |
| S56 | https://developer.apple.com/documentation/quartzcore/optimizing-iphone-and-ipad-apps-to-support-promotion-displays.md | ProMotion article: rates, power, game priority, thermal feedback loop | Apple | 2026-10-24 | © Apple; link only, reference use |
| S57 | https://developer.apple.com/documentation/bundleresources/information-property-list/cadisableminimumframedurationonphone.md | CADisableMinimumFrameDurationOnPhone key | Apple | 2026-10-24 | © Apple; link only, reference use |
| S58 | https://developer.apple.com/videos/play/wwdc2021/10147/ | WWDC21 session 10147: variable refresh, Low Power Mode, Limit Frame Rate | Apple | 2026-10-24 | © Apple; link only, reference use |
| S59 | https://developer.apple.com/documentation/foundation/processinfo/thermalstate-swift.enum.md | ProcessInfo.ThermalState cases | Apple | 2026-10-24 | © Apple; link only, reference use |
| S60 | https://developer.apple.com/documentation/foundation/processinfo/islowpowermodeenabled.md | isLowPowerModeEnabled: what Low Power Mode does | Apple | 2026-10-24 | © Apple; link only, reference use |
| S61 | https://developer.apple.com/documentation/xcode/responding-to-power-notifications.md | Responding to power notifications: Low Power Mode and thermal state | Apple | 2026-10-24 | © Apple; link only, reference use |
| S62 | https://developer-mdn.apple.com/library/archive/documentation/Performance/Conceptual/power_efficiency_guidelines_osx/RespondToThermalStateChanges.html | Archive macOS power guide: actions per thermal state | Apple | 2026-10-24 | © Apple; link only, reference use |
| S63 | https://developer.apple.com/library/archive/documentation/Performance/Conceptual/EnergyGuide-iOS/LowPowerMode.html | Archive iOS energy guide: react to Low Power Mode (2016) | Apple | 2026-10-24 | © Apple; link only, reference use |
| S64 | https://developer.apple.com/documentation/bundleresources/information-property-list/lssupportsgamemode.md | LSSupportsGameMode key | Apple | 2026-10-24 | © Apple; link only, reference use |
| S65 | https://support.apple.com/105118 | Use Game Mode: what it does, requirements, how to switch | Apple | 2026-10-24 | © Apple; link only, reference use |
| S66 | https://developer.apple.com/documentation/xcode/monitoring-your-metal-apps-graphics-performance.md | Metal Performance HUD contents and enabling | Apple | 2026-10-24 | © Apple; link only, reference use |
| S67 | https://developer.android.com/games/optimize/adpf/gamemode/about-API-and-interventions | Game Mode API and interventions overview | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S68 | https://developer.android.com/games/optimize/adpf/gamemode/gamemode-api | Game Mode values, selection, best practices | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S69 | https://developer.android.com/games/optimize/adpf/gamemode/gamemode-interventions | Interventions: backbuffer resize numbers, FPS throttling, opt-out | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S70 | https://developer.android.com/games/optimize/adpf/gamemode/fps-throttling | FPS throttling: 50% GPU and 20% system power, rates by display | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S71 | https://developer.android.com/games/optimize/adpf/thermal | Thermal API: headroom 0.0 to 1.0, 10 s limit, responses | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S72 | https://developer.android.com/games/optimize/adpf/best-practices-adpf | ADPF best practices: granular steps, avoid drastic preset changes | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S73 | https://android-developers.googleblog.com/2023/09/kakao-games-increased-fps-stability-through-android-adapatability.html | Kakao Games case: six-step quality sequence, FPS stability 75% to 96% | Google (Android Developers Blog) | 2026-10-24 | © Google (Android Developers Blog); link only, reference use |
| S74 | https://developer.android.com/games/engines/unity/unity-large-screen | Unity on large screens and foldables: resizable, camera, fold-aware UI | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S75 | https://developer.android.com/large-screens/gallery/games | Large-screens games gallery (index only, no usable text) | Google (Android Developers) | 2026-10-24 | © Google (Android Developers); link only, reference use |
| S76 | https://wwdcnotes.com/documentation/wwdc23-10123-bring-your-game-to-mac-part-1-make-a-game-plan/ | Opened; community notes page for WWDC23 10123 had no notes. Not used for claims | WWDC Notes (community) | 2026-10-24 | © WWDC Notes (community); link only, reference use |
| S77 | https://developer.apple.com/documentation/bundleresources/app-services.md | Opened; extract cut off before the Game Mode keys. Not used for claims | Apple | 2026-10-24 | © Apple; link only, reference use |

### Not opened or not usable (no claim rests on these)

| URL | Why not used |
|-----|--------------|
| https://developer.apple.com/design/human-interface-guidelines/designing-for-games | Fetched; the page text returned only its title. The data feed [S50] was used instead |
| https://genshin-impact.fandom.com/wiki/Settings | Fetch tool got HTTP 402; no page text in the saved material |
| https://androidauthority.com/genshin-impact-boost-fps-3173923 | Fetch tool got HTTP 403 on this host form; the www form worked [S3] |
| https://www.hoyolab.com/article/365958 | Fetch returned only a Loading message |
| https://wutheringwaves.kurogames.com/en/main/news/detail/5513 | Fetch returned only the page title |
| https://videogames.si.com/guides/wuthering-waves-best-iphone-ipad-settings-120-fps | 301 redirect to https://www.si.com/videogames/guides/wuthering-waves-best-iphone-ipad-settings-120-fps, not followed |
| https://www.epicgames.com/help/c-202300000001636/c-202300000001719/how-do-i-enable-120-fps-in-fortnite-on-my-samsung-device-a202300000082623 | HTTP 403; browser pane showed no text. Fortnite is unsourced |
| https://www.epicgames.com/help/en-US/c-Category_Fortnite/c-Fortnite_TechnicalSupport/how-do-i-change-graphic-performance-to-increase-frame-rate-fps-in-fortnite-a000089646 | HTTP 403 |
| https://www.minecraft.net/en-us/vibrant-visuals-update | Fetch timed out twice (agent and writer) |
| https://feedback.minecraft.net/hc/en-us/community/posts/35391950770317-Vibrant-Visuals-FAQ-Thread | HTTP 403 |
| https://us.support.blizzard.com/en/article/000276850 | Redirected to a login page |
| https://us.support.blizzard.com/en/article/000311412 | Redirected to a login page |
| https://toucharcade.com/2024/01/30/death-stranding-directors-cut-iphone-15-pro-review-graphics-performance-settings-controller/ | Fetch tool got HTTP 403; browser not tried |
| https://www.tomsguide.com/gaming/we-benchmarked-cyberpunk-2077-on-mac-heres-how-well-it-runs-on-m1-m4-macs-vs-windows | Fetched, but only the headline came back (body truncated) |
| https://www.ubisoft.com/en-gb/help/assassins-creed-shadows/connectivity-and-performance/article/system-requirements-for-assassin-s-creed-shadows/000111013 | Fetch returned an empty body |
| https://www.callofduty.com/blog/2024/03/call-of-duty-warzone-mobile-prepare-for-launch-need-to-know | Fetch timed out twice |
| https://primagames.com/gaming/zenless-zone-zero-zzz-settings-guide-best-graphics-and-performance-on-pc-ps5-and-mobile | Search result only (Zenless Zone Zero) |
| https://www.dtgre.com/2026/01/arknights-endfield-graphics-presets-explained.html | Search result only (Arknights: Endfield) |
| https://endfield.gg/?p=1256 | Search result only (Arknights: Endfield) |
| https://www.giga.de/entertainment/delta-force-die-besten-grafikeinstellungen--01JFA1141K750W8BQTZ4BS8MDT | Search result only (Delta Force) |
| https://ggrecon.com/guides/baldurs-gate-3-mac/ | Search result only (Baldur's Gate 3 Mac) |
| https://www.nomanssky.com/2026/10/no-mans-sky-ios/ | Search result only (No Man's Sky iOS announcement) |
| https://www.pcgameshardware.de/Grafikkarten-Grafikkarte-97980/News/Metal-4-mit-MetalFX-Frame-Interpolation-und-Denoising-1474693/ | Search result only (MetalFX frame interpolation news) |
| https://www.sportskeeda.com/esports/what-best-settings-get-high-fps-diablo-immortal-pc-mobile | Search result only (Diablo Immortal settings) |
| https://sky-children-of-the-light.fandom.com/wiki/Sky_for_Apple_Platforms | Search result only (Sky on Apple platforms) |
| https://www.youtube.com/watch?v=Fzyar232PZ8 | Video linked from the Wccftech page [S33]; not opened |
| https://cdn.wccftech.com/wp-content/uploads/2023/10/Resident-Evil-Village-being-played-on-the-iPhone-15-Pro-Max-2.jpg | Image linked from [S33]; not opened or downloaded |
| https://cdn.wccftech.com/wp-content/uploads/2023/10/Resident-Evil-Village-being-played-on-the-iPhone-15-Pro-Max-3.jpg | Image linked from [S33]; not opened or downloaded |

