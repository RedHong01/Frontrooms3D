# R1. PC and console settings screens: well-regarded cases

Research for the FRONTROOMSSS settings redesign. Research only. No design decisions are made here.

## Method

- Date: the material was gathered on 2026-10-24, 07:27-07:49 PDT, by the original researcher agent. It used WebSearch, WebFetch and the built-in browser pane (page text and screenshots). That agent hit its account limit before it wrote anything.
- This report was written up on 2026-10-24 from the agent's saved fetches. Inputs: the spec (`raw/R1_spec.md`) and the raw extract (`raw/R1_pc_console_raw.md`).
- The raw extract cuts long pages at 2,500 characters and drops images. So I also read the agent's full saved transcript and the screenshots it captured in the browser pane. This was read-only. Nothing was downloaded. Nothing was written inside the game project folder.
- I made one new WebFetch (the Ubisoft page, S25). I made no new searches.
- Evidence ranks, strongest first: (1) a screenshot page opened in the browser; (2) page text read in the browser; (3) a WebFetch answer (written by a small model, so weaker); (4) search-result snippets. Rank 4 was never opened and is not used as evidence.
- The main hub is ComputerBase (German PC tech site). For many games it shows the graphics menu in screenshots and has a table "Das Grafikmenü im Detail" (presets, FPS limiter, VRAM display, live preview, benchmark). That gives one consistent view of 15+ games. The screenshots use the German interface. I translated the labels. The table is the editors' own judgement.
- Reading guide: `[S12]` points to a row in the SOURCES table at the end. "(shot)" means a screenshot page. "(gk)" means general knowledge, not taken from an opened page. I use it only for developer names and release years where no opened page gives them.
- Quotes are kept under 15 words. German labels are translated by me.
- The Game UI Database (the hub named in the spec) could not be opened. See "Gaps".

### Known weak points of the material

- In one case a WebFetch answer was wrong. For The Last of Us Part I, the WebFetch table said there is no VRAM display. The screenshot shows a VRAM bar. I follow the screenshot (C2).
- For some games I only have a table row or a doc page, not a screenshot. Those cases say so.
- "What Custom does when you change one option" is rarely visible in a still image. Where I only infer it, I say "inference".
- The YouTube video found for RE4 did not show the menu in the frame that was captured (C1).
- What an icon or a star means is my reading of a still image. Where I could not tell, I wrote "unverified".

## Cases

### C1. Resident Evil 4 (2023), PC

1. Game, platform, year, developer: Resident Evil 4 (2023 remake). Windows, released 2023-03-24; macOS since December 2023. Capcom with K2, M-Two, Redworks and Playerium, on the RE Engine [S5].
2. Structure:
   - A tab strip with Q and E key hints. Eight tabs in this order: Controls, Camera, Display, Graphics, Language and subtitles, Audio, Accessibility, Server [S2].
   - The Graphics tab is one long scrolling list with no sub-tabs. At least 28 rows are visible across three shots: a "PC specifications" info row, a "Presets" row, then resolution, refresh rate, frame rate, display mode, VSync, cutscene resolution, ray tracing, two FSR rows, image quality, rendering method, anti-aliasing, texture quality, texture filter, mesh quality, shadow quality, shadow cache, contact shadows, ambient occlusion, volumetric light, particle lighting, bloom, screen-space reflections, volume scatter, hair strands, dismemberment [S2][S3][S4]. The list may continue past what the shots show.
   - Presets: ComputerBase counts five. Names (translated): "Prioritise performance", "Balanced", "Prioritise graphics", "Ray tracing", "Maximum" [S1]. In the shot the "Presets" row has an open-in-new icon, so it opens a sub-screen [S2]. I did not see that sub-screen. What Custom does is unverified.
3. Components and input:
   - The focused row shows ‹ value › arrows (shadow cache "On"). Other rows show the value in plain centred text [S3][S4].
   - A row can be greyed out (screen-space reflections "Off" is greyed while ray tracing is "High" in the other shot). The reason is not shown on screen [S2][S4].
   - Footer hints: the BS key (Backspace) resets all, R resets the category, a mouse icon goes back [S2]. Q and E switch tabs [S2].
   - PCGamingWiki adds: image quality is a slider that goes up to 200 % supersampling; FOV runs 90-110 degrees; frame rate can be 30, 60, 120 or Variable [S5].
4. Explanations and cost hints:
   - A right-hand panel shows a one-line description and a picture of the scene that matches the option (shadow cache; dismemberment) [S3][S4].
   - Under the picture: a line "maximum graphics memory use" with a number (14.44 GB / 22.84 GB) and six tick boxes with tiny bars: compute load, image quality, models, light, visual effects, motion rendering. The ticks show which categories an option touches [S3][S4].
   - The first row's info panel shows current memory use (5.2 GB / 22.84 GB) [S2].
   - The texture row label carries a size hint: "Texture quality (recommended VRAM size)", value "High (8 GB)" [S3].
   - ComputerBase: "a really good graphics menu", but the VRAM display "still behaves erratically". Preset quality drops sharply below "Balanced". Only a benchmark is missing [S1].
   - DSOGaming (WebFetch summary): small windows on the right show changes in real time [S6]. ComputerBase calls them example screenshots [S1]. Treat "live" as unverified.
   - Restart or main-menu-only markers: not seen. PCGamingWiki links crashes ("Fatal D3D Error") to running out of VRAM [S5]. OC3D says VRAM use is a problem below 8 GB [S7].
5. Links:
   - ComputerBase test, menu section: https://www.computerbase.de/artikel/gaming/resident-evil-4-remake-benchmark-test.83715/ [S1]
   - Screenshots (ComputerBase gallery, link only): https://pics.computerbase.de/1/0/7/1/7/4-3154958c63bd3a15/1-1280.jpg [S2], .../2-1280.jpg [S3], .../3-1280.jpg [S4]
   - PCGamingWiki: https://www.pcgamingwiki.com/wiki/Resident_Evil_4_(2023) [S5]
   - Video, per-setting before/after (YouTube, channel Flamerate, published 2026-03-19): https://www.youtube.com/watch?v=mAzinClQ_1o&t=42s [S8]. The description lists: 00:27 Ray Tracing, 00:42 Anti Aliasing, 01:29 Texture Quality, 02:54 Ambient Occlusion, 07:07 Comparison. The one frame the agent captured (near 0:28) is a split-screen of FSR1, anti-aliasing and FSR2, not the menu. Use it as a before/after reference only.
6. Lesson for FrontRooms: Borrow the right-hand panel (one line, one picture, category ticks) and the three reset scopes (row, category, all). Avoid a VRAM number that jumps, and avoid a preset button that opens a screen with no summary.

### C2. The Last of Us Part I (PC), 2023

1. Game, platform, year, developer: The Last of Us Part I. Windows. PC test and patches dated March to May 2023 [S9]. Naughty Dog; the PC port was made by Iron Galaxy [S9].
2. Structure:
   - The header reads OPTIONS plus the current page (DISPLAY or GRAPHICS) [S10][S12].
   - A box with the VRAM bar and the render resolution stays pinned above the list while the list scrolls [S10]-[S14].
   - Display page: group GENERAL (video device, display, display mode, aspect ratio, resolution, ultrawide pillarboxing, V-Sync, frame-rate limit), then group EFFECTS (field of view, camera shake, motion blur, chromatic aberration, film grain, violence) [S10][S11].
   - Graphics page: groups with caps headings, e.g. TEXTURE SETTINGS and LIGHTING SETTINGS. Draw distance and detail-level rows come first [S12][S13][S14].
   - ComputerBase: four presets (Low, Medium, High, Ultra), "countless options, almost too many" [S9]. Where the preset row sits is not visible in the saved shots (unverified).
3. Components and input:
   - Toggle switch with the word ON/OFF (V-Sync, image-based lighting) [S10][S14].
   - Stepper: value, then a left arrow and a dot. The dot replaces an arrow at the end of the range (rows at "Ultra" show ◀ and a dot) [S12][S13].
   - Slider with the number to its left (field of view 0; camera shake 10; motion blur 10; film grain 10) [S11]. The frame-rate limiter is a slider whose top end reads "UNLOCKED" [S10].
   - Padlock on a row that is blocked (Sharpening) [S11]. An indented child row sits under its parent (dynamic screen-space shadows under screen-space shadow quality) [S14].
   - Two rows (video device, display) end in a dash icon; its meaning is not stated [S10].
   - Footer: F applies changes, ◀▶ changes the value, R resets to defaults, a Back key goes back [S10]. So changes need an explicit Apply.
   - A build number sits bottom left. A shader-build percentage sits bottom right (17-20 % in the shots) [S10]-[S14].
4. Explanations and cost hints:
   - Right panel: option name, one or two sentences, a picture (a close-up of the characters for the shadow option; a street for the detail-level option) [S12][S13][S14].
   - Below the text: "Effect on performance" with three lines, GPU, VRAM, CPU. Each is a word (Low, Moderate, High) in green, yellow or red [S12][S13][S14].
   - The pinned VRAM box: "91 %", 14,628 / 16,064 MB, a split bar (OS and apps 3,212 MB; the game 11,416 MB) and a tick mark on the bar [S10]. A WebFetch summary of ComputerBase's table claimed there is no VRAM display; that is wrong against the shot, so I do not use it. ComputerBase's own article text confirms the per-option CPU/GPU/VRAM display [S9].
   - Naughty Dog support lists 14 settings with a "Moderate to Major" effect on VRAM by menu path, says an in-game performance HUD lives at Options > HUD > Performance, and gives 4 GB minimum and 8 GB recommended VRAM [S15].
   - Presets buy little: High is only +13 % to +15 % over Ultra. ComputerBase advises FSR 2 or DLSS 2 instead [S9].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/the-last-of-us-part-i-benchmark-test.83755/ [S9]
   - Shots: https://pics.computerbase.de/1/0/7/2/2/6-934a6ef151ae141d/1-1280.jpg [S10] (Display page, VRAM box), .../3-1280.jpg [S11] (Effects), .../7-1280.jpg [S14] (impact words), .../5-1280.jpg [S12] (detail levels), .../6-1280.jpg [S13] (texture sampling)
   - Official support: https://feedback.naughtydog.com/hc/en-us/articles/16157836719892-My-VRAM-usage-is-high-What-can-I-do [S15]
6. Lesson for FrontRooms: This is the best model for cost hints: three words (Low, Moderate, High) per option, plus one pinned budget bar. In ink and paper, use weight or a small mark in place of green, yellow and red. The "dot instead of arrow" at the end of a range is a cheap way to show limits on a stepper.

### C3. Returnal (PC), 2023

1. Game, platform, year, developer: Returnal. Windows. ComputerBase says Returnal reached PC just before The Last of Us Part I, so early 2023 [S9]. Housemarque (gk).
2. Structure:
   - Title SETTINGS. Tab strip with Q and E hints: Controls, Gameplay, Audio, Video, Graphics [S17][S18].
   - The Video tab is a left list of categories (Display, Colour-blind mode, Adjust status bar, Performance UI) beside a right-hand option list. An "Ultrawide" group is greyed out [S17].
   - The Graphics tab opens with a preset carousel pinned above the list. It shows three labels: previous, current, next ("Epic", "Custom" (highlighted), "Low") with arrows at both ends [S18]. The highlighted label reads "Angepasst" ("adjusted"); I read it as the custom state. So Custom is a named state in the same carousel. What happens when you edit a row is inference only.
   - A POST-PROCESSING group: volumetric fog, particle quality, depth of field, film grain 100 %, bloom, emission intensity 100 %, brightness 50 %, SDR black point 2 %, contrast 50 % [S18].
   - ComputerBase: presets, FPS limiter 10-240 in steps of 1, several upscalers, in-game scaling 25-100 % in steps of 1, dynamic resolution, variable-rate shading [S16].
3. Components and input:
   - Stepper ‹ value › for enumerations; slider with a percentage for film grain, emission, brightness, contrast; a greyed row for anti-aliasing quality [S18]. A small sub-screen button for the status-bar safe zone [S17].
   - Footer: Esc back; A and D adjust; Backspace restores the default; Alt hides the preview; Tab starts a benchmark [S18]. A benchmark key sits inside the menu.
4. Explanations and cost hints:
   - ComputerBase: every option has a description and a marker for whether it hits CPU, GPU or VRAM. There are example screenshots. A game overlay turns on by itself and shows FPS, CPU and GPU load, GPU temperature and clock, and VRAM. The menu is transparent, so the scene shows through. ComputerBase says other developers should learn from this menu (my paraphrase of the editors' praise) [S16].
   - Shot: right panel with the option name, a description and "Probable effects" with three bars (CPU, GPU, VRAM), each four segments long; the emission option fills one of four GPU segments [S18]. The overlay in the shot's top-left reads FPS 49, CPU 0 %, GPU 100 %, 56 degrees C, 1,830 MHz, VRAM 8.40 GB [S18].
   - Hint "Alt hides preview" shows the menu can be hidden to judge the image [S18].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/returnal-benchmark-test.83360/ [S16]
   - Shots: https://pics.computerbase.de/1/0/6/7/8/1-62db832e35338b59/2-1280.jpg [S17] (Video tab), .../5-1280.jpg [S18] (Graphics tab, cost bars, overlay)
6. Lesson for FrontRooms: A transparent menu over the live scene fits "no dark cards". Copy the four-step cost bar, the hide-menu key and the Custom label inside the preset carousel. The benchmark key could become a "test this device" key.

### C4. Call of Duty: Modern Warfare III (PC), 2023

1. Game, platform, year, developer: Call of Duty: Modern Warfare III. Windows. 2023 (the ComputerBase page URL carries "2023") [S19]. Activision (gk).
2. Structure:
   - Full-screen page with a left rail of seven icons (the first is a favourites star; the graphics icon is active). Header: game logo and "GRAPHICS". Three sub-tabs: DISPLAY | QUALITY | VIEW [S20][S21].
   - Display sub-tab: display mode (with a star), monitor, graphics card, refresh rate, resolution, aspect ratio, V-Sync, custom frame-rate limit, and a button "Restart shader preload" with an external-link icon [S20]. Some rows have a "Show more" expander under them [S20].
   - Quality sub-tab: anisotropic filter, depth of field, detail-quality level, particle resolution, bullet impacts, persistent effects, shader quality, on-demand texture streaming, local texture-streaming quality, then a group "Shadows and lighting" [S21].
   - ComputerBase: five presets (Extreme, Ultra, Balanced, Basic, Minimum). The FPS limiter is set separately for game and menu [S19]. Where the preset row sits is not in the two shots (unverified).
3. Components and input:
   - A dropdown with a chevron for named choices (display mode, monitor, resolution, even V-Sync "ON") [S20][S21].
   - A toggle switch for booleans (depth of field, bullet impacts) [S21].
   - A star on a row marks a favourite. Bottom bar: "Reset tab", "Search", and a toggle "Accessibility filter" [S20].
4. Explanations and cost hints:
   - Right panel: a big title, two side-by-side pictures (before and after), one or two sentences with a highlighted key phrase, and a grey "Note" line [S20].
   - Chips under the title: "Effect on: GPU: MEDIUM / VRAM: MEDIUM" [S21].
   - A main-menu-only marker as a note: this setting can only be changed in the main menu [S21].
   - A bar "Estimated VRAM use" split into the game (5.98 GB) and other apps (1.44 GB), with a TARGET marker. The total reads 7.41 / 15.68 GB [S20][S21].
   - ComputerBase's table marks detailed descriptions, comparison screenshots, FPS counter, overlays and VRAM display as present; live preview and benchmark as absent [S19].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/cod-modern-warfare-iii-2023-benchmark-test.86069/ [S19]
   - Shots: https://pics.computerbase.de/1/0/9/9/6/8-d6f02cbccd41e71c/1-1280.jpg [S20] (Display, V-Sync), .../4-1280.jpg [S21] (Quality, impact chips)
6. Lesson for FrontRooms: Borrow "reset this tab", a search box, a before/after picture pair, a target mark on the budget bar, and a plain note when a row only works from the main menu. Avoid the icon rail plus three sub-tabs; it is a lot of chrome.

### C5. Assassin's Creed Shadows (PC), 2025

1. Game, platform, year, developer: Assassin's Creed Shadows. Windows. Ubisoft (gk), 2025 (gk).
2. Structure:
   - Top tab strip with Q and E hints: Video, Controls, Sound, Interface, Gameplay, and a sixth labelled "Dritte" (third parties; meaning not clear) [S23][S24].
   - Under Video, two sub-tabs with Y and C hints: Display | Scalability [S23][S24].
   - Display: "Image calibration" button, a field-of-view slider (100 %), then the group Screen (monitor, display mode, aspect ratio, V-Sync, resolution) [S23].
   - Scalability: near the top, "Global ray-tracing lighting" and "General preset: Custom". Then groups Ray tracing (quality, BVH quality) and Lighting (screen-space effects, light-source quality) [S24].
   - ComputerBase: five presets (Extreme High, Very High, High, Medium, Low); FPS limiter, dynamic resolution and sharpening present [S22].
   - The preset row reads "Custom" while RT quality is "Extreme high". So the preset row falls to Custom when the options are mixed (inference from the shot) [S24].
3. Components and input:
   - Dropdown chevron for monitor, display mode, aspect ratio and resolution [S23].
   - ‹ › stepper for V-Sync, the preset and the quality rows [S23][S24].
   - Slider with a number and a filled bar for field of view [S23].
   - A button with an arrow-out icon for the calibration screen [S23].
   - Footer: Esc back; R reset page; X apply (greyed until a change is pending); a second R reset [S23][S24].
4. Explanations and cost hints:
   - Right panel: title, one or two sentences, a large preview picture (a character in a doorway for RT quality) [S24].
   - A VRAM bar at the bottom right of the right panel: 7,633 MB / 32,101 MB [S23][S24].
   - ComputerBase marks VRAM display, comparison screenshots and an integrated benchmark as present; live preview as absent [S22]. Ubisoft's own page lists an "In-Game Benchmark Tool for Performance Analysis" and "In-Depth Customization Options" [S25].
   - A per-setting performance-cost meter: not seen in the saved shots (unverified). Ubisoft's page does not mention one [S25].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/assassins-creed-shadows-benchmark-test.91755/ [S22]
   - Shots: https://pics.computerbase.de/1/1/6/4/8/9-9fc859bb07d0495a/1-1280.jpg [S23] (Display), .../3-1280.jpg [S24] (Scalability)
   - Ubisoft news page (WebFetch): https://www.ubisoft.com/en-gb/game/assassins-creed/news/79Ep6jxIt1MeGC3f3sG21G [S25]
6. Lesson for FrontRooms: Split "Display" (what the screen needs) from "Scalability" (what costs frames). The same split maps to our desktop-versus-phone problem.

### C6. Avatar: Frontiers of Pandora (PC), 2023

1. Game, platform, year, developer: Avatar: Frontiers of Pandora. Windows. Massive Entertainment and Ubisoft (gk), December 2023 (gk).
2. Structure:
   - Tab strip with Q and E hints: Gameplay, Controls, Video, Graphics, UI, Audio, Language [S27][S28]. The Video tab is one list [S27][S28].
   - ComputerBase: presets Ultra, High, Medium, Low. FPS limiter 20-300 in steps of 1. VRAM display, comparison screenshots and integrated benchmark present. Live preview and FPS counter absent [S26].
3. Components and input:
   - Each row is a rounded glowing bar. The value is centred. A short row of dots under the value shows the position in the range [S27][S28].
   - Two-state options use a segmented "OFF | ON" where the active side is bright (Frame generation, Frame-rate limit) [S27][S28].
   - A disabled row is dimmed: Frame generation dims when the upscaler is DLSS, and its help text says it works only with FSR 3 [S27][S28].
   - Footer: G resets to default; F applies; Esc goes back [S27].
4. Explanations and cost hints:
   - Right panel: title and one sentence [S27].
   - ComputerBase warns that the menu still talks about ray tracing on cards without RT hardware, and that some RT effects cannot be switched off [S26].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/avatar-frontiers-of-pandora-benchmark-test.86316/ [S26]
   - Shots: https://pics.computerbase.de/1/1/0/2/6/5-880764e7811fb9d2/1-1280.jpg [S27], .../3-1280.jpg [S28]
6. Lesson for FrontRooms: A segmented "OFF | ON" is the clearest two-state control and is easy to hit by touch. Dim and explain a blocked row; do not hide it. Do not offer an option the device cannot do.

### C7. God of War Ragnarök (PC), 2024

1. Game, platform, year, developer: God of War Ragnarök. Windows. Santa Monica Studio (gk), PC 2024 (gk).
2. Structure:
   - One scrolling page titled "GRAPHICS & CAMERA". Headings in order: DISPLAY, SCALING, QUALITY, GRAPHICS, CAMERA [S30][S31][S32].
   - DISPLAY: graphics card, monitor, display mode, resolution (greyed), VSync, aspect ratio, frame-rate limit, latency reduction, frame generation [S30].
   - SCALING: method (DLSS), quality [S30].
   - QUALITY: a "Preset" row, then eight child rows drawn with tree lines (textures, models, anisotropic, lighting, shadows, reflections, atmospherics, ambient occlusion). While the preset reads "Ultra", the children look dimmed [S31]. Whether editing a child flips the preset to Custom is unverified.
   - GRAPHICS: screen calibration (button), grain and motion-blur sliders, "Reduced flashing" (Off). CAMERA: invert vertical and horizontal, speed sliders (5) [S32].
   - ComputerBase: presets Ultra, High, Medium, Low; VRAM display and live preview present; no dynamic resolution [S29].
3. Components and input:
   - Bordered value boxes with arrows; sliders with a diamond knob and a number; a button with an Enter icon; headings with a rule [S30][S31][S32].
   - Footer keys: Q page up, E page down, U all default, H default (this row), Esc back, Enter select [S30][S32].
4. Explanations and cost hints:
   - Right panel: title, one sentence, and a line "Default setting: Medium". The world (a character in a cave) stays visible behind the page [S30][S31].
   - A small readout bottom right: "VRAM 45 %, PSOs 100 %". I do not know what "PSOs" counts on screen (unverified) [S31].
   - Some labels are blue; the meaning is not verified [S30][S31].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/god-of-war-ragnaroek-benchmark-test.89735/ [S29]
   - Shots: https://pics.computerbase.de/1/1/4/2/3/6-145733d867cc1405/1-1280.jpg [S30], .../2-1280.jpg [S31] (preset with children), .../3-1280.jpg [S32]
6. Lesson for FrontRooms: State the default in the help text. Use a parent row with indented children for preset-plus-detail. Give long lists page-up and page-down keys. Put camera and motion comfort last, in the same page.

### C8. Ratchet & Clank: Rift Apart (PC, port by Nixxes), 2023

1. Game, platform, year, developer: Ratchet & Clank: Rift Apart. Windows (Steam and Epic). PC port by Nixxes [S34]. Insomniac Games (gk). ComputerBase test URL dated 2023-08 [S33].
2. Structure:
   - Two tabs: SCREEN | GRAPHICS, switched with Tab [S35][S36][S37].
   - Nixxes support page, in order: DISPLAY MENU (monitor, window mode, resolution, aspect ratio, refresh rate, VSync, NVIDIA Reflex, brightness, HDR with max luminance and paper white, contrast, DLSS frame generation, upscale method, upscale quality, sharpness, dynamic resolution scaling, anti-aliasing); GRAPHICS MENU ("Preset", texture quality, filtering, shadows, ambient occlusion, screen-space reflections); Ray-tracing settings; Geometry settings; Camera effects [S34]. The settings can be opened from the menu before the game starts and from the pause menu [S34].
   - Preset: five levels, Very Low to Very High. The page says the preset "changes the settings below" [S34]. ComputerBase lists the same five [S33].
   - Ray tracing is independent: the page says ray-tracing settings "are controlled independently from graphics presets" and are off by default [S34].
   - In the Graphics tab the first row reads "Preset: CUSTOM", with a small reset-arrow icon at the right of the row [S36]. So the preset row shows Custom when options were changed (inference), and offers a one-step way back.
3. Components and input:
   - ‹ value › stepper for most rows (window mode, resolution, VSync) [S35].
   - Slider with a number for brightness (50), contrast (10), HDR paper white (200), upscale sharpness (5) [S35].
   - Greyed rows when blocked (monitor, refresh rate, HDR max luminance, anti-aliasing "DLAA") [S35].
   - Dynamic resolution scaling takes a target frame rate: "OFF, 30, 45, 60, or higher" [S34].
   - Footer: Tab switches tabs; F resets to default (the scope is not stated, probably the focused row); R resets all; Esc goes back [S35].
4. Explanations and cost hints:
   - A tooltip box floats beside the focused row. The menu is a narrow panel on the left, so the live city scene stays visible [S35][S37].
   - The support page names the cost in words: texture quality needs "more video memory"; lower level of detail improves "GPU and CPU performance" [S34].
   - ComputerBase: VRAM display present, live preview present, dynamic resolution present. The top preset beats maximum details by only 6-7 % fps [S33]. I did not see the VRAM meter in the three saved shots (unverified where it sits).
5. Links:
   - Nixxes support: https://support.nixxes.com/hc/en-us/articles/48538446144403-PC-performance-tuning [S34]
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/ratchet-und-clank-rift-apart-benchmark-test.85001/ [S33] (the agent first opened the alias https://www.computerbase.de/2023-08/ratchet-und-clank-rift-apart-benchmark-test/ and it landed here)
   - Shots: https://pics.computerbase.de/1/0/8/6/8/7-f8aaf9eb96bcb088/2-1280.jpg [S35] (Screen tab), .../3-1280.jpg [S36] (Custom preset), .../4-1280.jpg [S37] (FOV tooltip)
6. Lesson for FrontRooms: A preset row that reads Custom plus a reset-arrow is simple and honest. Keep the one expensive feature (ray tracing) outside the preset ladder. Order: display, quality, expensive extras, geometry, camera effects last.

### C9. Marvel's Spider-Man Remastered (PC, port by Nixxes)

1. Game, platform, year, developer: Marvel's Spider-Man Remastered. Windows. PC port by Nixxes [S38]. Insomniac Games (gk), PC 2022 (gk).
2. Structure (from the Nixxes support page; no in-game screenshot was opened):
   - DISPLAY MENU: monitor, window mode, resolution, refresh rate, VSYNC, brightness, HDR, HDR max luminance, anti-aliasing, upscale method, upscale quality, dynamic resolution scaling (12 options).
   - GRAPHICS MENU: "Preset" (Very Low, Low, Medium, High, Very High), texture quality, texture filtering, shadow quality, ambient occlusion, screen-space reflections (6).
   - Ray-tracing settings (4), Geometry settings (5: level of detail, traffic density, crowd density, hair quality, weather particles), Camera effects (8: depth of field, field of view, motion blur, film grain, bloom, vignette, chromatic aberration, lens flares) [S38]. My count: 35 options.
   - The page says the settings are reachable from the menu before the game and from the pause menu [S38].
3. Components and input: not described on the page. Values are named: texture filtering has five steps; dynamic resolution takes "OFF, 30, 45, 60" [S38]. DSOGaming names a field-of-view slider [S39]. GGRecon (WebFetch) does not describe the menu; named presets appear only in the system requirements [S40].
4. Explanations: each option has one or two sentences in the doc. The doc says the camera effects "do not have a big impact on performance and can be configured to taste" [S38]. The in-game screen was not seen.
5. Links:
   - Nixxes support: https://support.nixxes.com/hc/en-us/articles/48438642781459-PC-performance-tuning [S38]
   - DSOGaming (thin; menu image only, WebFetch): https://www.dsogaming.com/screenshot-news/here-are-the-pc-graphics-settings-for-marvels-spider-man-remastered/ [S39]
   - GGRecon (thin, WebFetch): https://ggrecon.com/guides/spider-man-remastered-pc-features [S40]
6. Lesson for FrontRooms: Use this as the group-order and wording template. Put a "Camera effects" group last and say that it costs little. FrontRooms' CAMERA MOTION and REDUCE FLASHING belong in that kind of group.

### C10. Marvel's Spider-Man 2 (PC, port by Nixxes), 2025

1. Game, platform, year, developer: Marvel's Spider-Man 2. Windows. PC port by Nixxes [S43]. Insomniac Games (gk), PC 2025 (gk).
2. Structure:
   - Two tabs with Q and E hints: SCREEN | GRAPHICS [S42].
   - Nixxes page: DISPLAY MENU (adds frame generation and "Max generated frames", an upscale "preset" for DLSS models); GRAPHICS MENU with "Preset" and a separate "Raytracing Preset" (pre-defined raytracing settings), then ray-traced reflections, interiors, shadows, ambient occlusion, geometry detail, object range, DLSS ray reconstruction; Geometry settings; Camera effects extended with "Fullscreen effects" and "Screen shake" on/off toggles [S43].
   - ComputerBase: presets Very High, High, Medium, Low; live preview present; FPS limiter, dynamic resolution, sharpening present; no VRAM meter, no benchmark [S41].
3. Components and input (shot):
   - Orange section headings; ‹ value › steppers; sliders with numbers (brightness 50, contrast 50). Small star marks sit at the end of some rows; I do not know what they mean (unverified) [S42].
   - The description sits in a box under the list, not beside it. Footer: H hides the menu; R resets all; Esc closes [S42].
4. Explanations and cost hints:
   - Per-option text on the Nixxes page (e.g. texture quality "allow for more memory and video memory") [S43].
   - ComputerBase: keep ray-traced reflections at the top setting or they get low-resolution and flicker [S41]. A hint like this belongs in the menu text.
5. Links:
   - Nixxes support: https://support.nixxes.com/hc/en-us/articles/49284471919635-Marvel-s-Spider-Man-2-PC-Performance-Tuning [S43]
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/spider-man-2-benchmark-test.91345/ [S41]
   - Shot: https://pics.computerbase.de/1/1/6/0/2/2-78d2c62d6a406d8e/3-1280.jpg [S42]
6. Lesson for FrontRooms: Copy the "hide menu" key (H) so the player can judge the image. Keep a second, separate preset for the one expensive effect. Put "Screen shake" and "Fullscreen effects" toggles beside the graphics options, as FrontRooms already does with CAMERA MOTION.

### C11. Horizon Forbidden West (PC, port by Nixxes), 2024

1. Game, platform, year, developer: Horizon Forbidden West. Windows. Guerrilla; the PC port is by Nixxes. The ComputerBase PC test carries updates dated 23 and 26 March 2024, so the PC version is from early 2024 [S44].
2. Structure:
   - Title SETTINGS. Tab strip with Q and E hints: General, Display, Graphics, Audio, Controller, Mouse and keyboard, Accessibility [S45].
   - Display tab (one scroll): resolution, aspect ratio, "Letterboxing for cutscenes" (indented child), refresh rate (greyed), VSync, Reflex, then rows ending in "..." (Dynamic Range; Colour-weakness mode), then dynamic resolution scaling, anti-aliasing (greyed, "DLAA"), upscaling method (DLSS) with indented children (upscaling quality, DLSS frame generation) [S45].
   - ComputerBase: five presets (Very High to Very Low); live preview present; dynamic resolution and sharpening present; no FPS limiter, no VRAM meter, no benchmark [S44]. The table marks "detailed descriptions" absent, yet the shot shows a long description panel; the table seems to mean something stricter [S44][S45].
3. Components and input:
   - Chevron ‹ value › steppers. Children are indented with a corner mark. A row ending in "..." opens a sub-menu [S45].
   - Footer: Esc back; F reset; R apply (greyed when nothing is pending) [S45].
4. Explanations and cost hints:
   - Right panel: the option name in caps and a paragraph of about six sentences. For the upscaling method it explains the choices, and what dynamic resolution does when it is on [S45].
   - ComputerBase on presets: texture quality is the most visible step; set textures to maximum whatever the preset, if VRAM allows; "Medium" is the only sensible lower preset [S44] (this last remark comes from a WebFetch answer only).
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/horizon-forbidden-west-benchmark-test.87503/ [S44]
   - Shot: https://pics.computerbase.de/1/1/1/6/7/9-a16f3846a020f305/2-1280.jpg [S45]
6. Lesson for FrontRooms: Show a blocked or dependent row as an indented child. Mark a row that opens another screen with "...". Write the description so it explains how two options affect each other.

### C12. Ghost of Tsushima Director's Cut (PC, port by Nixxes), 2024

1. Game, platform, year, developer: Ghost of Tsushima Director's Cut. Windows. Port by Nixxes [S47]. Sucker Punch (gk), PC 2024 (gk).
2. Structure (table only; no shot opened):
   - ComputerBase: five presets (Very High to Very Low), individual options, dynamic resolution, sharpening. No FPS limiter, FPS counter, VRAM meter, live preview, comparison screenshots, detailed descriptions or benchmark [S46]. This is the bare end of the range.
   - Nixxes patch 8 notes: "Added a Steam Deck graphics preset", plus Steam Deck verified status and UI changes for the Deck [S47].
3. Components and input: unverified (no screenshot).
4. Explanations: none, per ComputerBase. The article's only caption is "The graphics menu of the PC version" [S46].
5. Links:
   - Nixxes patch notes: https://support.nixxes.com/hc/en-us/articles/49255788828691-Ghost-of-Tsushima-Director-s-Cut-PC-Patch-8-Release-Notes [S47]
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/ghost-of-tsushima-benchmark-test.88153/ [S46]
6. Lesson for FrontRooms: When a platform has a fixed budget, ship a preset named after it (Steam Deck here). For us that means presets named for device classes (iPhone, iPad, Mac, foldable) instead of one Low-to-Ultra ladder. Avoid the bare menu with no explanation.

### C13. Cyberpunk 2077 (Phantom Liberty era), PC

1. Game, platform, year, developer: Cyberpunk 2077. Windows (and Steam Deck, see below). CD Projekt Red (gk). The ComputerBase test is of the Phantom Liberty update (2023, gk) [S48].
2. Structure:
   - Tab strip with 1 and 3 key hints: Sound, Controls, Gameplay, Graphics, Video, Interface, Key bindings [S49].
   - Graphics tab: a button "B Start benchmark" at the left of the list. The first row is "Quick preset" as a stepper; in the shot it reads "Custom" (the German label is "Angepasst", "adjusted"). Then groups: NVIDIA DLSS (frame generation, super resolution, sharpness, ray reconstruction, DLAA), Resolution scaling (dynamic scaling, FSR 2.1, XeSS 1.1), then a basic group [S49].
   - ComputerBase counts nine presets: RT Overdrive, RT Ultra, RT Medium, RT Low, Ultra, High, Medium, Low, and "Steam Deck" [S48]. So ray-tracing tiers and a device preset share one ladder.
   - Also: FPS limiter 10-240 in steps of 1, configurable dynamic resolution, sharpening, and an integrated benchmark. No live preview, VRAM display or descriptions [S48].
3. Components and input:
   - ‹ value › stepper with a small underline mark for position [S49].
   - A toggle that shows its state as a word (OFF) [S49].
   - A slider with a boxed number (DLSS sharpness 0.05) [S49].
   - A greyed row for ray reconstruction [S49].
   - Footer: a "Standard" button at the bottom centre; F1 resets to default [S49].
4. Explanations: none visible in the shot. ComputerBase's table has no descriptions [S48]. The NVIDIA article the agent opened (https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-optimize-rtx-settings/) covers GeForce Experience, not the in-game menu, so I do not use it [S50].
5. A known fault (ComputerBase): the upscaling setting can change by itself after you close and reopen the menu or change a preset. DLSS "Balanced" can turn into "Performance". The fix is to cycle the mode and confirm. "Whoever changes presets has to check upsampling separately" (my translation of the editors' remark) [S48].
6. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/cyberpunk-2077-phantom-liberty-benchmark-test.85634/ [S48]
   - Shot: https://pics.computerbase.de/1/0/9/4/5/1-f003dc6e97ced5ca/1-1280.jpg [S49]
7. Lesson for FrontRooms: A hardware-named preset beside quality tiers works (Steam Deck here). But a preset must not silently change a second system (upscaling). If a preset touches a linked option, show the new value on screen right away.

### C14. Alan Wake 2 (PC), 2023

1. Game, platform, year, developer: Alan Wake 2. Windows (patch notes cover PC, PS5 and Xbox). Remedy (gk), 2023 (gk). The patch notes are on the game's own site [S53].
2. Structure:
   - Header "OPTIONS" with an Esc hint. Tab strip with Q and E hints: Gameplay, Controls, Graphics, Audio, Interface [S52].
   - Graphics page, in order: DISPLAY (display mode, resolution, render resolution, upscaling, DLSS frame generation, V-Sync, brightness calibration), EFFECTS (motion distortion, film grain), QUALITY (quality preset, post-processing quality, more rows below) [S52].
   - Preset row: LOW | MEDIUM | HIGH | CUSTOM as a row of words; the selected word is underlined. In the shot CUSTOM is the selected one [S52]. ComputerBase counts three presets plus individual options [S51].
3. Components and input:
   - Segmented words with an underline on the chosen one: display mode (Windowed | Borderless | Fullscreen), upscaling (DLSS | FSR 2), quality preset [S52].
   - Checkboxes for on/off options (frame generation, V-Sync, motion distortion, grain) [S52].
   - A dropdown with a small caret for long lists (resolution, render resolution) [S52].
   - A link-out arrow for brightness calibration [S52].
   - No cards and no boxes. This is the closest case to an editorial look.
4. Explanations and cost hints:
   - A help line at the top of the right column: "Choose whether the game favours visual quality or performance" (my translation; the row it belongs to is not marked) [S52].
   - ComputerBase marks "detailed descriptions" as partial. No VRAM display, no FPS limiter [S51].
   - Patch notes: 1.0.15 added "preview images for most tweakable settings" in the PC graphics menu, a field-of-view slider in the UI, faster apply of graphics changes, and a fix so the options scroll position no longer resets. 1.2.8: the Ultra ray-tracing setting is unavailable when DLSS Ray Reconstruction is off or unsupported. 1.0.15: a horror flash option with Low and Normal "visual and audio intensity". 1.0.12: "Restore to Default" for key bindings [S53].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/alan-wake-2-benchmark-test.85961/ [S51]
   - Shot: https://pics.computerbase.de/1/0/9/8/4/6-4799dbb3efd98a5c/1-1280.jpg [S52]
   - Official update notes: https://www.alanwake.com/story/alan-wake-2-update-notes/ [S53] (WebFetch summary)
6. Lesson for FrontRooms: Segmented words with an underline and plain checkboxes fit ink and paper. Make Custom the last word in the preset row. A "flash intensity" option exists in a shipped horror game; it supports our REDUCE FLASHING row (but ours should stay a clear toggle or a 2-step choice).

### C15. Silent Hill 2 (2024), PC

1. Game, platform, year, developer: Silent Hill 2 (remake). Windows. Bloober Team (gk), 2024 (gk).
2. Structure:
   - Tab strip with a key hint at each end: Gameplay, Display and Graphics, Audio, Controls, Language and text, Accessibility. A "!" mark sits next to the active tab and next to the first row; its meaning is not stated (unverified) [S55].
   - First row: "Render quality presets", a five-segment stepper; in the shot it reads "Custom". Next row: "Advanced quality settings", a button with a sub-screen icon [S55].
   - GRAPHICS group: global motion blur, motion blur in game (locked), graphics mode, colour-blind mode, colour-blind severity 0.50 (locked slider), inventory effects, crosshair in game, high-contrast mode, colours of James, allies and enemies (locked) [S55]. Accessibility options sit in the same page.
   - ComputerBase: presets Epic, High, Medium, Low; FPS limiter, dynamic resolution and in-game up/down-scaling; no VRAM display, no benchmark [S54].
3. Components and input:
   - Every enumeration is a value word above a five-notch bar, with small arrows [S55].
   - A padlock icon on a row that depends on a parent (motion blur in game; colour-blind severity; colour of James) [S55].
   - A numeric slider (0.50). A sub-screen icon on "Advanced quality settings" [S55].
   - Footer: R restores default; Esc goes back [S55].
4. Explanations: a right panel with one sentence ("Enable ray-tracing effects") and a large preview picture of a bar interior [S55]. ComputerBase's table marks live preview and descriptions as absent, so the picture is static [S54].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/silent-hill-2-benchmark-test.89885/ [S54]
   - Shot: https://pics.computerbase.de/1/1/4/4/0/5-66e896a2b3ddfa5c/2-1280.jpg [S55]
6. Lesson for FrontRooms: Use a padlock for a row that waits on another row, and say why. Keep a short main list and push the detail into an "Advanced" sub-screen. A five-notch bar shows position without a number.

### C16. Dead Space (2023 remake), PC

1. Game, platform, year, developer: Dead Space (remake). Windows. Motive Studio (gk), 2023 (gk).
2. Structure:
   - A panel headed "DISPLAY AND GRAPHICS" laid over the game, with a long help paragraph at the right. Rows: Video options (sub-screen arrow), Brightness (sub-screen arrow), motion blur (slider 50), film grain (toggle), colour-vision mode (‹ None ›), colour-vision contrast (greyed slider 50), dynamic resolution scaling (toggle), scaling rate (100 %), anti-aliasing ("NVIDIA DLSS"), DLSS mode ("Quality") [S57].
   - ComputerBase calls the menu "not extensive". It has enough single options, four presets (Low, Medium, High, Ultra), dynamic resolution that aims to hold the frame rate at 25, 50 or 100 % of the display refresh rate, and DLSS/FSR [S56].
3. Components and input: sub-screen arrows, sliders with numbers, toggle bars, ‹ value › stepper. Footer: T resets; Esc goes back [S57].
4. Explanations: the right paragraph explains the colour-vision modes by name (protanopia, deuteranopia, tritanopia) [S57].
5. Gotchas found by ComputerBase [S56]:
   - The Ultra preset turns on ray-traced ambient occlusion. On a card without ray tracing, the screen-space AO is switched off at Ultra and must be turned back on by hand. So you get no ambient occlusion at all.
   - "Medium" removes almost all screen-space reflections and looks poor. "Low" looks worse.
6. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/dead-space-remake-benchmark-test.83164/ [S56]
   - Shot: https://pics.computerbase.de/1/0/6/5/5/2-00e756a38c55722c/1-1280.jpg [S57]
7. Lesson for FrontRooms: A preset must never silently remove a feature the player needs. Our lighting is the game's main horror tool; if a low tier drops it, the menu must say so.

### C17. DOOM: The Dark Ages (PC), 2025

1. Game, platform, year, developer: DOOM: The Dark Ages. Windows. id Software (gk), 2025. The Bethesda update post is for the June 2025 PC update [S60].
2. Structure:
   - Page title "GRAPHICS" with a strip of seven icon tabs between Q and E hints; the display icon is active [S59].
   - List: field of view (slider 90), chromatic aberration (‹ Off ›), depth of field (‹ On ›), sharpening (slider 1.50), grain (slider 1.00), upscaler (dropdown "DLSS"), DLSS super resolution (dropdown), DLSS sharpness (slider 46 %), frame generation (dropdown "2x"), FSR rows greyed [S59].
   - ComputerBase: six presets (Ultra Nightmare, Nightmare, Ultra, High, Medium, Low); FPS counter, overlays and VRAM display present; no live preview, no benchmark [S58].
   - Bethesda: a "Benchmark Mode" in the main menu under Extras, with a level picker; graphs for FPS, frame time, GPU and CPU frame time that you can switch between fps and ms. A "Path Tracing" section in Video Settings with a Denoiser option (Ray Reconstruction) [S60].
3. Components and input:
   - Slider with a diamond knob and a number; ◀ ▶ stepper with an underline; dropdown with a ▼ [S59].
   - Footer: Esc back; Select; V reset to default; R apply; B enlarge (I do not know what is enlarged, unverified) [S59].
4. Explanations and cost hints: right panel with the option name and three sentences that state hardware needs: 2x frame generation needs an RTX 40 series card or newer; 3x and 4x need an RTX 50 series card (my translation) [S59]. A readout bottom right: "VRAM: 11541 MiB / 27606 MiB" [S59]. Bethesda's support page tells players to revert to the "auto-detected" setting, so a detected baseline exists [S61]. Bethesda warns that path tracing is only officially supported on listed GPUs [S60].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/doom-the-dark-ages-benchmark-test.92497/ [S58]
   - Shot: https://pics.computerbase.de/1/1/7/3/1/2-55eb89a955601c82/4-1280.jpg [S59]
   - Bethesda update post (WebFetch): https://slayersclub.bethesda.net/en-US/news/doom-the-dark-ages-june-2025-pc-update [S60]
   - Bethesda support (WebFetch): https://help.bethesda.net/app/answers/detail/a_id/70052/~/what-do-i-do-if-im-experiencing-performance-issues-in-doom:-the-dark-ages-on [S61]
6. Lesson for FrontRooms: Put hardware requirements in the help text. Keep an auto-detected default and a way back to it. Offer the benchmark from the main menu, not from deep inside settings.

### C18. Resident Evil Requiem (PC), 2026

1. Game, platform, year, developer: Resident Evil Requiem. Windows. Capcom (gk), 2026 (gk; the ComputerBase test page is undated in the extract) [S62].
2. Structure:
   - Header OPTIONS. Tab strip with Q and E: Controls, Camera, HUD, Graphics, Audio, Language and subtitles, Accessibility, Network [S63][S64].
   - Inside Graphics, two sub-tabs, hinted with 1 and 3: Display | Detail settings [S63][S64]. Compare with the older RE4 (C1), which had one long list.
   - Display: display mode, "Display area" and "Brightness" (both sub-screen buttons), HDR, screen (greyed), colour space, VSync [S63].
   - Detail settings: upscaling quality (greyed, value "Custom"), light and shadow quality ("Maximum"), then a group "Advanced quality settings" with an expander row "Advanced options", then the group "Graphics quality": texture quality, texture filter, anti-aliasing (greyed), mesh quality, screen-space reflections [S64].
   - ComputerBase: five presets (Maximum, High, Medium, Low, Minimum); FPS limiter, VRAM display and comparison screenshots present; no live preview, no benchmark, no detailed descriptions [S62].
3. Components and input:
   - ‹ value › stepper; a short notch bar under the value shows position in the range [S63][S64].
   - Sub-screen buttons with an open icon [S63].
   - Footer: Tab resets all; R resets the sub-category; a mouse icon goes back [S63].
4. Explanations and cost hints: the right panel shows a one-line description, a picture, a "VRAM use" bar with a legend (Game | Other applications), and a "Compute load" bar [S64].
5. Links:
   - ComputerBase test: https://www.computerbase.de/artikel/gaming/resident-evil-requiem-benchmark-test.96230/ [S62]
   - Shots: https://pics.computerbase.de/1/2/1/4/2/9-045ceb7bc5982cdf/31-1280.jpg [S63] (Display), .../33-1280.jpg [S64] (Detail settings)
6. Lesson for FrontRooms: This shows the sequel's cleanup of C1: a split into Display and Details, a fold for "Advanced options", a VRAM bar that separates the game from other apps, and reset scopes named all and sub-category.

### C19. Forza Horizon 5 (PC), 2021: what to avoid

1. Game, platform, year, developer: Forza Horizon 5. Windows. Playground Games (gk), 2021 (gk). The ComputerBase test is the only source [S65].
2. Structure (text only; no shot opened):
   - ComputerBase: many individual options but not much comfort (my translation). No example screenshots and no usable explanations. No in-game downsampling [S65].
   - Presets in the benchmark chart: Very Low, Low, Medium, High, Ultra, Extreme, plus "maximum details" above Extreme [S65]. High nearly doubles the frame rate against Extreme [S65].
   - Upscaling hides behind the label "Resolution scaling", with the levels Ultra Quality, Quality, Balanced, Performance. Microsoft would not say it is AMD's FSR [S65].
3. Fault: after the resolution was changed, the game could hang in an endless loading loop and had to be restarted (Store version, Windows 10 and 11) [S65].
4. Links: https://www.computerbase.de/artikel/gaming/forza-horizon-5-benchmark-test.78538/ [S65]. The Forza support page was blocked (HTTP 403) and is not used.
5. Lesson for FrontRooms: Do not ship jargon with no help text, vendor technology under a vague label, or a resolution change that can lock the game. Always keep a confirm-or-revert step for display changes.

### C20. PS5 graphics-mode pickers (Insomniac and others), console

1. Game, platform, year, developer: Marvel's Spider-Man: Miles Morales, Marvel's Spider-Man 2, Ratchet & Clank: Rift Apart (PS5); Alan Wake 2 (PS5 Pro and PC). Insomniac Games (gk) and Remedy (gk). Opened pages: Insomniac support [S66][S67], Game Informer [S68], Push Square [S69], PlayStation Blog [S70], Alan Wake update notes [S53].
2. Structure and choices:
   - Miles Morales lists "Graphics Mode" under Visual: "Fidelity" = 30 FPS with ray-traced lighting and extra effects from a 4K base; "Performance" = 60 FPS without those extras, from a lower-resolution base [S66]. Two named modes, each with its frame-rate target in the text.
   - Spider-Man 2 keeps the same two modes. Fidelity targets 30 FPS with all ray tracing on; Performance targets 60 FPS and simplifies some ray tracing, crowds and hair [S68].
   - Two system settings change the target. "VRR" (Settings > Screen and Video > Video Output > VRR) has "Smoothed" or "Uncapped". "Enable 120 Hz output" lifts Fidelity from 30 to 40 FPS. Game Informer recommends Fidelity, VRR Smoothed and 120 Hz on a 120 Hz 4K screen [S68].
   - Insomniac FAQ: with VRR and the 120 Hz option the frame rate can exceed the chosen mode's 30 or 60 FPS target by 50 % or more [S67].
   - Push Square: Rift Apart patch 1.002 added "120 Hz Display Mode". 40 FPS "divides equally into" 120 Hz, which keeps pacing even [S69].
   - PS5 Pro: Insomniac's new default is "Performance Pro", 4K with ray tracing at 60 FPS [S70].
   - Alan Wake 2 (update 1.2.7): a new "Balanced" graphics mode, needing a display that can reach 120 Hz, and a PSSR on/off toggle [S53].
3. Components and input: a picker with two or three named modes. No sliders, no custom mode. Modes can depend on the screen: Balanced needs 120 Hz [S53].
4. Explanations: each mode is described in one sentence that names the trade (look versus speed) and the frame rate [S66][S68].
5. Links:
   - https://support.insomniac.games/hc/en-us/articles/46670895263635-What-Accessibility-options-does-Marvel-s-Spider-Man-Miles-Morales-feature [S66]
   - https://support.insomniac.games/hc/en-us/articles/46679539272851-What-is-VRR-support [S67]
   - https://www.gameinformer.com/guide/2023/10/19/heres-the-best-graphics-mode-for-marvels-spider-man-2 [S68] (WebFetch)
   - https://www.pushsquare.com/news/2021/06/ratchet_and_clank_rift_apart_patch_1_002_adds_40fps_mode_for_120hz_tvs [S69] (WebFetch)
   - https://blog.playstation.com/2024/09/24/ps5-pro-developers-share-how-theyre-using-new-tech-to-enhance-their-games/ [S70] (WebFetch)
   - https://www.alanwake.com/story/alan-wake-2-update-notes/ [S53] (WebFetch)
6. Lesson for FrontRooms: On phones and tablets, a picker of two or three modes named by what the player gets ("Smooth 60", "Detailed 30", and a 40 or 120 mode where the panel allows) is easier than sliders. Offer only modes the device can hold. The 40 FPS idea suits 120 Hz panels.

## Source hubs and gaps

### Hubs that were opened

- ComputerBase (the "Das Grafikmenü im Detail" table and menu screenshots). Used in C1-C8, C10-C19. Link only.
- Nixxes support pages (a doc for each Nixxes port: option names, order and one-line help). Used in C8, C9, C10, C12. Nixxes also states for Death Stranding 2 that upscaling and frame generation can be mixed across vendors (for example DLSS upscaling with FSR frame generation) and adds an upscaler named PICO [S75]. That page does not describe the menu itself.
- Interface In Game (interfaceingame.com) is a screenshot library, with a "Settings" element filter of 1,905 items [S71]. The filtered list that the agent read had 24 pages, including `alan-wake-ii-graphics`, `doom-eternal-accessibility`, `hello-neighbor-settings`, `halo-infinite-brightness-setup` and `bloodborne-brightness` [S72]. Two pages were opened: Alan Wake II "Graphics" [S73] and Doom Eternal "Accessibility" [S74]. Both show only a screenshot with a generic alt text and a like count. The one capture of the Alan Wake II page is blank, so I do not describe it. Footer: "(c) 2022 Interface In Game", with all trademarks owned by their owners [S71]. Link only.

### Could not be opened (no claims taken from them)

- Game UI Database. `https://www.gameuidatabase.com/about.php` returned HTTP 403. `https://www.gameuidatabase.com/index.php?scrn=134` showed a Cloudflare "Performing security verification" page in the browser, and the agent did not go past it (bot checks are not to be bypassed). So there is no Game UI Database entry in this report. Searching its name returned only its About page and a news item (search result only).
- Digital Foundry and Eurogamer: the tool's user agent is blocked for both domains.
- TechSpot, GamesKinny and OC3D (The Last of Us pages): HTTP 403. Forza support page: 403. Horizon wiki on Fandom: 402. An Insomniac release-notes page: 404. A PlayStation Blog Spider-Man Remastered article: 404.
- PCGamingWiki worked in the browser for RE4 only. No other PCGamingWiki page was opened.

### Games in the brief with no case

- Alien: Isolation, Amnesia: The Bunker, The Outlast Trials: searched, no page about the menu was opened.
- Control, Star Wars Outlaws, Forza Motorsport, Hellblade II, Death Stranding PC: no page about the menu was opened. Death Stranding 2 has only the feature page [S75].
- Assassin's Creed Shadows and Avatar: the brief hoped for per-setting performance-impact and VRAM-impact meters. I saw a global VRAM bar (C5) and a VRAM display in the ComputerBase table (C6), but no per-setting meter in the saved material.
- Restart-required markers: only one was found (C4, "main menu only"). The rest is unverified.
- Videos: only one video page was opened (C1, S8). The other YouTube hits (Last of Us and RE4 guides) were search results only, so no video with a menu timestamp exists for the other cases.
- Controller driving (bumpers and sticks): all shots are keyboard builds with key hints (Q/E, Tab, R). Controller glyphs were not seen.
- Years and developers marked (gk) come from memory, not from a page.

## Synthesis

Numbers in brackets are case numbers.

1. **Group order.** Screen first, scaling next, quality and preset after that, expensive extras after the quality group, and camera or effects last.
   - Nixxes order: Display, Graphics (preset first), Ray tracing, Geometry, Camera effects [C8, C9, C10].
   - Ubisoft splits Display from Scalability [C5]. Capcom's sequel splits Display from Detail settings [C18]. Santa Monica uses Display, Scaling, Quality, Graphics, Camera on one page [C7].
   - Comfort options sit beside the graphics, not in a separate "accessibility" ghetto: camera shake and motion blur [C2], reduced flashing [C7], screen shake [C10], colour-blind and high contrast [C15]. A horror flash-intensity option exists in a shipped game [C14]; the notes do not say where it sits in the menu.
2. **Tabs.** A top tab strip with two key hints (Q and E, or bumpers) is standard [C1, C3, C5, C6, C10, C11, C13, C14, C17, C18]. Depth stops at two levels. A single scrolling page with headings is enough up to about 30 rows [C1, C7, C13]. Sub-tabs appear only where the page is long [C4, C5, C18].
3. **Preset and Custom.**
   - The preset row is the first row of the quality group [C5, C8, C13, C15] or a carousel pinned above it [C3]. A preset row with four named words (Low, Medium, High, Custom) is the closest to our type style [C14].
   - Custom is shown as a state of the same control: "Custom" in the preset row after you change a detail [C3, C5, C8, C13, C14, C15]. A small reset arrow beside it undoes the change [C8]. Cases without a visible Custom state: C1 and C7 (unverified).
   - A parent row with indented children (children dimmed under a preset) is another pattern [C7].
   - Four to six levels is normal [C1, C2, C8, C11, C15, C16, C17, C19]. Special presets exist: ray-tracing tiers and "Steam Deck" [C13], "Steam Deck" alone [C12], and a separate ray-tracing preset kept outside the main ladder [C8, C10].
   - Reviewers find that presets buy little speed (High vs Ultra +13-15 % [C2]; +6-7 % [C8]; +10-12 % [C16]) and that the low steps look bad [C1, C2, C11, C16, C19]. So each step needs design, not just a lower number.
   - Presets can have side effects. One resets the upscaler [C13]. One removes ambient occlusion with no warning [C16].
4. **Cost hints.** Three kinds are in use, and they stack.
   - Per option, three words (Low, Moderate, High) for GPU, VRAM and CPU [C2], or two chips [C4], or four-step bars [C3], or category tick boxes [C1].
   - One global budget bar for VRAM, pinned or in the right panel [C2, C4, C5, C18], or a number only [C1, C7, C17]. The best ones split game from other apps [C2, C4, C18] and add a target mark [C4]. A bar can mislead: ComputerBase calls the RE4 one erratic [C1].
   - A benchmark inside the menu: a key [C3], a button [C13], or main menu Extras [C17]; Ubisoft lists one for Shadows [C5].
5. **Preview and explanation.**
   - A right-hand panel with a title, one to three sentences and a picture is the norm [C1, C2, C3, C4, C5, C14, C15, C18]. Others put the text in a tooltip beside the row [C8] or in a box under the list [C10].
   - Pictures are mostly static examples. A before/after pair is rarer [C4]. Alan Wake 2 added pictures later by patch [C14].
   - The live route: a narrow or transparent menu so the scene stays visible [C3, C7, C8, C10, C11], with a key to hide the menu [C3 Alt, C10 H].
   - The help text can state the default [C7], the hardware needed [C17, C6], and a dependency between two options [C11].
6. **Component rules seen.**
   - Stepper (‹ value ›) for named levels, up to about six [almost all cases].
   - Dropdown for long lists such as monitors and resolutions [C4, C5, C17].
   - Segmented words for two to four choices [C14, C6].
   - Toggle or checkbox for booleans [C2, C4, C14].
   - Slider with the number shown for continuous values: FOV, sharpness, brightness, grain [C2, C5, C8, C13, C17].
   - Button with an arrow icon, or "...", for a sub-screen such as calibration [C1, C5, C11, C14, C15, C18].
   - A blocked row is dimmed or locked and stays visible [C2, C6, C8, C11, C15, C17].
7. **Input and footer.** A footer with key chips is universal. Reset has up to three scopes: row, category or page, all [C1, C7, C8, C18]. Five cases show an explicit Apply key [C2, C5, C6, C11, C17]; the rest do not show one.
8. **Console and device modes.** A picker of two or three named modes with a frame-rate target, no sliders [C20]. System switches (120 Hz output, VRR) move the target, for example 30 to 40 FPS [C20]. Some modes need a capable screen [C20]. Device-named presets exist on PC [C12, C13].
9. **Faults to avoid.** No explanations [C12, C19]. Vendor jargon under a vague label [C19]. Showing options the hardware cannot do [C6]. A display change that can hang the game [C19]. A preset that silently changes another setting [C13, C16].


## SOURCES

All rows below were opened (in the browser pane or with WebFetch) on 2026-10-24. "WebFetch summary" means a small model wrote the answer from the page, so it is weaker evidence. Screenshot rows are image pages on the ComputerBase picture server; nothing was downloaded.

| # | URL | what it shows | owner/publisher | accessed | usage note |
|---|---|---|---|---|---|
| S1 | https://www.computerbase.de/artikel/gaming/resident-evil-4-remake-benchmark-test.83715/ | RE4 test, menu section: five presets, VRAM display, verdict (WebFetch + page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S2 | https://pics.computerbase.de/1/0/7/1/7/4-3154958c63bd3a15/1-1280.jpg | RE4 menu shot: tabs, Presets row, PC specs panel, footer keys | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S3 | https://pics.computerbase.de/1/0/7/1/7/4-3154958c63bd3a15/2-1280.jpg | RE4 menu shot: shadow cache row, preview picture, category tick boxes, VRAM line | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S4 | https://pics.computerbase.de/1/0/7/1/7/4-3154958c63bd3a15/3-1280.jpg | RE4 menu shot: dismemberment row, preview, tick boxes | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S5 | https://www.pcgamingwiki.com/wiki/Resident_Evil_4_(2023) | RE4 facts: developers, release dates, video options, FOV range, frame-rate values, VRAM crash note (page text in browser) | PCGamingWiki (community wiki) | 2026-10-24 | PCGamingWiki content; link only, reference use |
| S6 | https://www.dsogaming.com/pc-performance-analyses/resident-evil-4-remake-pc-performance-analysis/ | RE4 PC analysis; says small windows on the right show changes in real time (WebFetch summary only) | DSOGaming | 2026-10-24 | (c) DSOGaming; link only, reference use |
| S7 | https://www.overclock3d.net/reviews/software/resident_evil_4_remake_pc_performance_review_and_optimisation_guide/1 | RE4 review intro: VRAM is a problem below 8 GB (page text in browser; page loaded as overclock3d.net/.../guide/) | Overclock3D | 2026-10-24 | (c) Overclock3D; link only, reference use |
| S8 | https://www.youtube.com/watch?v=mAzinClQ_1o&t=42s | YouTube "RE4 Remake OPTIMIZATION GUIDE" by Flamerate, published 2026-03-19. Description lists per-setting timestamps. Menu not seen in the captured frame | Flamerate (YouTube channel) | 2026-10-24 | (c) video owner; link only, reference use |
| S9 | https://www.computerbase.de/artikel/gaming/the-last-of-us-part-i-benchmark-test.83755/ | TLoU Part I PC test: menu praise, four presets, preset gains, update dates (page text in browser; table from WebFetch not used where wrong) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S10 | https://pics.computerbase.de/1/0/7/2/2/6-934a6ef151ae141d/1-1280.jpg | TLoU Part I menu shot: Display page: pinned VRAM box, General group, footer keys | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S11 | https://pics.computerbase.de/1/0/7/2/2/6-934a6ef151ae141d/3-1280.jpg | TLoU Part I menu shot: Display page: Effects group (sliders, padlock row) | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S12 | https://pics.computerbase.de/1/0/7/2/2/6-934a6ef151ae141d/5-1280.jpg | TLoU Part I menu shot: Graphics page: detail levels with GPU/VRAM/CPU words | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S13 | https://pics.computerbase.de/1/0/7/2/2/6-934a6ef151ae141d/6-1280.jpg | TLoU Part I menu shot: Graphics page: texture sampling, group headings | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S14 | https://pics.computerbase.de/1/0/7/2/2/6-934a6ef151ae141d/7-1280.jpg | TLoU Part I menu shot: Graphics page: screen-space shadows with impact words, child row | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S15 | https://feedback.naughtydog.com/hc/en-us/articles/16157836719892-My-VRAM-usage-is-high-What-can-I-do | Official support: 14 settings with Moderate to Major VRAM effect; HUD > Performance; VRAM 4 GB min, 8 GB rec. (page text in browser) | Naughty Dog (support site) | 2026-10-24 | (c) Naughty Dog / Sony; link only, reference use |
| S16 | https://www.computerbase.de/artikel/gaming/returnal-benchmark-test.83360/ | Returnal PC test: menu praise, descriptions, CPU/GPU/VRAM markers, overlay, FPS limiter and scaling ranges (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S17 | https://pics.computerbase.de/1/0/6/7/8/1-62db832e35338b59/2-1280.jpg | Returnal menu shot: Video tab, category list, ultrawide group greyed | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S18 | https://pics.computerbase.de/1/0/6/7/8/1-62db832e35338b59/5-1280.jpg | Returnal menu shot: Graphics tab, preset carousel, CPU/GPU/VRAM bars, overlay, footer keys | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S19 | https://www.computerbase.de/artikel/gaming/cod-modern-warfare-iii-2023-benchmark-test.86069/ | MWIII PC test: "Das Grafikmenü im Detail" table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S20 | https://pics.computerbase.de/1/0/9/9/6/8-d6f02cbccd41e71c/1-1280.jpg | MWIII menu shot: Display sub-tab, V-Sync help with before/after pictures, VRAM bar with target | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S21 | https://pics.computerbase.de/1/0/9/9/6/8-d6f02cbccd41e71c/4-1280.jpg | MWIII menu shot: Quality sub-tab, GPU/VRAM impact chips, main-menu-only note | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S22 | https://www.computerbase.de/artikel/gaming/assassins-creed-shadows-benchmark-test.91755/ | Assassin's Creed Shadows PC test: menu table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S23 | https://pics.computerbase.de/1/1/6/4/8/9-9fc859bb07d0495a/1-1280.jpg | AC Shadows menu shot: Video > Display, FOV slider, dropdowns, VRAM bar | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S24 | https://pics.computerbase.de/1/1/6/4/8/9-9fc859bb07d0495a/3-1280.jpg | AC Shadows menu shot: Video > Scalability, preset row reading Custom, preview picture | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S25 | https://www.ubisoft.com/en-gb/game/assassins-creed/news/79Ep6jxIt1MeGC3f3sG21G | Ubisoft page: lists an in-game benchmark tool, dynamic resolution, upscalers; no per-setting indicators (WebFetch by me, 2026-10-24; summary only) | Ubisoft | 2026-10-24 | (c) Ubisoft; link only, reference use |
| S26 | https://www.computerbase.de/artikel/gaming/avatar-frontiers-of-pandora-benchmark-test.86316/ | Avatar FoP PC test: menu table, RT-label remarks (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S27 | https://pics.computerbase.de/1/1/0/2/6/5-880764e7811fb9d2/1-1280.jpg | Avatar FoP menu shot: Video tab, OFF|ON segment, help text, footer keys | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S28 | https://pics.computerbase.de/1/1/0/2/6/5-880764e7811fb9d2/3-1280.jpg | Avatar FoP menu shot: greyed Frame generation, frame-rate limit segment | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S29 | https://www.computerbase.de/artikel/gaming/god-of-war-ragnaroek-benchmark-test.89735/ | God of War Ragnarök PC test: menu table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S30 | https://pics.computerbase.de/1/1/4/2/3/6-145733d867cc1405/1-1280.jpg | GoW Ragnarök menu shot: Graphics & Camera, Display and Scaling groups | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S31 | https://pics.computerbase.de/1/1/4/2/3/6-145733d867cc1405/2-1280.jpg | GoW Ragnarök menu shot: Preset row with child rows, "Default setting" line, VRAM/PSOs readout | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S32 | https://pics.computerbase.de/1/1/4/2/3/6-145733d867cc1405/3-1280.jpg | GoW Ragnarök menu shot: Graphics and Camera groups, sliders, reduced flashing | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S33 | https://www.computerbase.de/artikel/gaming/ratchet-und-clank-rift-apart-benchmark-test.85001/ | Rift Apart PC test: menu table, preset gain (page text in browser; first opened as /2023-08/ratchet-und-clank-rift-apart-benchmark-test/) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S34 | https://support.nixxes.com/hc/en-us/articles/48538446144403-PC-performance-tuning | Nixxes doc for Rift Apart PC: every option by menu group, presets, independent ray tracing (page text in browser) | Nixxes Software (support site) | 2026-10-24 | (c) Nixxes / Sony; link only, reference use |
| S35 | https://pics.computerbase.de/1/0/8/6/8/7-f8aaf9eb96bcb088/2-1280.jpg | Rift Apart menu shot: Screen tab, tooltip box, footer keys | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S36 | https://pics.computerbase.de/1/0/8/6/8/7-f8aaf9eb96bcb088/3-1280.jpg | Rift Apart menu shot: Graphics tab, "Preset: Custom" with reset icon | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S37 | https://pics.computerbase.de/1/0/8/6/8/7-f8aaf9eb96bcb088/4-1280.jpg | Rift Apart menu shot: Graphics tab, geometry and camera effects, FOV tooltip | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S38 | https://support.nixxes.com/hc/en-us/articles/48438642781459-PC-performance-tuning | Nixxes doc for Spider-Man Remastered PC: option list by group (page text in browser) | Nixxes Software (support site) | 2026-10-24 | (c) Nixxes / Sony; link only, reference use |
| S39 | https://www.dsogaming.com/screenshot-news/here-are-the-pc-graphics-settings-for-marvels-spider-man-remastered/ | Spider-Man Remastered PC settings news; thin: names FOV slider, no structure (WebFetch summary only) | DSOGaming | 2026-10-24 | (c) DSOGaming; link only, reference use |
| S40 | https://ggrecon.com/guides/spider-man-remastered-pc-features | Spider-Man Remastered PC feature list; presets named only in requirements (WebFetch summary only) | GGRecon | 2026-10-24 | (c) GGRecon; link only, reference use |
| S41 | https://www.computerbase.de/artikel/gaming/spider-man-2-benchmark-test.91345/ | Spider-Man 2 PC test: menu table, RT reflection remark (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S42 | https://pics.computerbase.de/1/1/6/0/2/2-78d2c62d6a406d8e/3-1280.jpg | Spider-Man 2 menu shot: Screen tab, description box under list, H hides menu | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S43 | https://support.nixxes.com/hc/en-us/articles/49284471919635-Marvel-s-Spider-Man-2-PC-Performance-Tuning | Nixxes doc for Spider-Man 2 PC: options, Raytracing Preset, camera effects (page text in browser) | Nixxes Software (support site) | 2026-10-24 | (c) Nixxes / Sony; link only, reference use |
| S44 | https://www.computerbase.de/artikel/gaming/horizon-forbidden-west-benchmark-test.87503/ | Horizon Forbidden West PC test: menu table, preset remarks (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S45 | https://pics.computerbase.de/1/1/1/6/7/9-a16f3846a020f305/2-1280.jpg | HFW menu shot: Display tab, tab strip, child rows, "..." rows, long description | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S46 | https://www.computerbase.de/artikel/gaming/ghost-of-tsushima-benchmark-test.88153/ | Ghost of Tsushima PC test: menu table, no extras (WebFetch + page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S47 | https://support.nixxes.com/hc/en-us/articles/49255788828691-Ghost-of-Tsushima-Director-s-Cut-PC-Patch-8-Release-Notes | Official Patch 8 notes: Steam Deck graphics preset, Steam Deck verified (page text in browser) | Nixxes Software (support site) | 2026-10-24 | (c) Nixxes / Sony; link only, reference use |
| S48 | https://www.computerbase.de/artikel/gaming/cyberpunk-2077-phantom-liberty-benchmark-test.85634/ | Cyberpunk 2077 PC test: nine presets, menu table, upscaling fault (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S49 | https://pics.computerbase.de/1/0/9/4/5/1-f003dc6e97ced5ca/1-1280.jpg | Cyberpunk 2077 menu shot: Graphics tab, Quick preset Custom, benchmark button, DLSS group | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S50 | https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-optimize-rtx-settings/ | NVIDIA 2020 article; covers GeForce Experience, not the in-game menu; not used for claims (WebFetch summary) | NVIDIA | 2026-10-24 | (c) NVIDIA; link only; not used as evidence |
| S51 | https://www.computerbase.de/artikel/gaming/alan-wake-2-benchmark-test.85961/ | Alan Wake 2 PC test: menu table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S52 | https://pics.computerbase.de/1/0/9/8/4/6-4799dbb3efd98a5c/1-1280.jpg | Alan Wake 2 menu shot: Graphics page, segmented words, checkboxes, preset row with Custom | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S53 | https://www.alanwake.com/story/alan-wake-2-update-notes/ | Official update notes: preview images, FOV, horror flash option, Balanced mode, PSSR toggle, RT rule (WebFetch summary) | Remedy Entertainment | 2026-10-24 | (c) Remedy; link only, reference use |
| S54 | https://www.computerbase.de/artikel/gaming/silent-hill-2-benchmark-test.89885/ | Silent Hill 2 PC test: menu table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S55 | https://pics.computerbase.de/1/1/4/4/0/5-66e896a2b3ddfa5c/2-1280.jpg | Silent Hill 2 menu shot: Display and Graphics page, Custom preset stepper, padlock rows | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S56 | https://www.computerbase.de/artikel/gaming/dead-space-remake-benchmark-test.83164/ | Dead Space remake PC test: menu paragraph, preset gotchas (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S57 | https://pics.computerbase.de/1/0/6/5/5/2-00e756a38c55722c/1-1280.jpg | Dead Space menu shot: Display and Graphics panel with help paragraph | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S58 | https://www.computerbase.de/artikel/gaming/doom-the-dark-ages-benchmark-test.92497/ | DOOM: The Dark Ages PC test: menu table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S59 | https://pics.computerbase.de/1/1/7/3/1/2-55eb89a955601c82/4-1280.jpg | DOOM TDA menu shot: Graphics page, sliders, dropdowns, VRAM readout, footer keys | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S60 | https://slayersclub.bethesda.net/en-US/news/doom-the-dark-ages-june-2025-pc-update | Bethesda post: Benchmark Mode (Extras), Path Tracing section, GPU warning (WebFetch summary) | Bethesda Softworks | 2026-10-24 | (c) Bethesda; link only, reference use |
| S61 | https://help.bethesda.net/app/answers/detail/a_id/70052/~/what-do-i-do-if-im-experiencing-performance-issues-in-doom:-the-dark-ages-on | Bethesda support: lower quality level; revert to auto-detected setting (WebFetch summary) | Bethesda Softworks | 2026-10-24 | (c) Bethesda; link only, reference use |
| S62 | https://www.computerbase.de/artikel/gaming/resident-evil-requiem-benchmark-test.96230/ | Resident Evil Requiem PC test: menu table (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S63 | https://pics.computerbase.de/1/2/1/4/2/9-045ceb7bc5982cdf/31-1280.jpg | Requiem menu shot: Graphics > Display sub-tab, sub-screen buttons, footer reset scopes | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S64 | https://pics.computerbase.de/1/2/1/4/2/9-045ceb7bc5982cdf/33-1280.jpg | Requiem menu shot: Detail settings, advanced fold, VRAM bar with legend, compute-load bar | ComputerBase | 2026-10-24 | (c) ComputerBase / game publisher; screenshot link only, reference use |
| S65 | https://www.computerbase.de/artikel/gaming/forza-horizon-5-benchmark-test.78538/ | Forza Horizon 5 PC test: menu paragraph, preset chart, resolution-change hang (page text in browser) | ComputerBase | 2026-10-24 | (c) ComputerBase; link only, reference use |
| S66 | https://support.insomniac.games/hc/en-us/articles/46670895263635-What-Accessibility-options-does-Marvel-s-Spider-Man-Miles-Morales-feature | Official list: Graphics Mode Fidelity 30 FPS / Performance 60 FPS and other visual options (page text in browser) | Insomniac Games (support site) | 2026-10-24 | (c) Insomniac / Sony; link only, reference use |
| S67 | https://support.insomniac.games/hc/en-us/articles/46679539272851-What-is-VRR-support | Official FAQ: VRR and 120 Hz option effects on the mode targets (page text in browser) | Insomniac Games (support site) | 2026-10-24 | (c) Insomniac / Sony; link only, reference use |
| S68 | https://www.gameinformer.com/guide/2023/10/19/heres-the-best-graphics-mode-for-marvels-spider-man-2 | Spider-Man 2 PS5 modes, VRR values, 120 Hz output, recommendation (WebFetch summary) | Game Informer | 2026-10-24 | (c) Game Informer; link only, reference use |
| S69 | https://www.pushsquare.com/news/2021/06/ratchet_and_clank_rift_apart_patch_1_002_adds_40fps_mode_for_120hz_tvs | Rift Apart patch 1.002: 120 Hz Display Mode, 40 FPS Fidelity (WebFetch summary) | Push Square | 2026-10-24 | (c) Push Square; link only, reference use |
| S70 | https://blog.playstation.com/2024/09/24/ps5-pro-developers-share-how-theyre-using-new-tech-to-enhance-their-games/ | PS5 Pro developer notes: "Performance Pro" default, mode names (WebFetch summary) | PlayStation Blog (Sony Interactive Entertainment) | 2026-10-24 | (c) Sony; link only, reference use |
| S71 | https://interfaceingame.com/screenshots/?category=options | Interface In Game screenshot list; Settings filter with 1,905 items; copyright footer (WebFetch + page in browser) | Interface In Game | 2026-10-24 | (c) site and game owners; link only, reference use |
| S72 | https://interfaceingame.com/screenshots/?elements=settings | Settings-filtered list; 24 page slugs read in the browser | Interface In Game | 2026-10-24 | (c) site and game owners; link only, reference use |
| S73 | https://interfaceingame.com/screenshots/alan-wake-ii-graphics/ | Alan Wake II "Graphics" screenshot page; metadata only, image not seen (WebFetch) | Interface In Game | 2026-10-24 | (c) site and game owners; link only, reference use |
| S74 | https://interfaceingame.com/screenshots/doom-eternal-accessibility/ | Doom Eternal "Accessibility" screenshot page; metadata only (WebFetch) | Interface In Game | 2026-10-24 | (c) site and game owners; link only, reference use |
| S75 | https://support.nixxes.com/hc/en-us/articles/49584441309971-What-are-the-PC-features-in-DEATH-STRANDING-2-ON-THE-BEACH | Death Stranding 2 PC feature list: DLSS 4, FSR 4, XeSS 2, mixing of upscaler and frame generation, PICO (page text in browser) | Nixxes Software (support site) | 2026-10-24 | (c) Nixxes / 505 Games / Sony; link only, reference use |

### Not opened (listed so nobody cites them)

| URL | status | note |
|---|---|---|
| https://www.gameuidatabase.com/about.php | HTTP 403 | Game UI Database; blocked in WebFetch. |
| https://www.gameuidatabase.com/index.php?scrn=134 | Cloudflare check page | Game UI Database screen entry; the browser showed only a bot check. Not bypassed. |
| https://www.eurogamer.net/digitalfoundry-2023-the-last-of-us-part-1-pc-tech-review | blocked | Digital Foundry; domain not accessible to the tool. |
| https://www.techspot.com/review/2656-the-last-of-us-gpu-benchmark/ | HTTP 403 | TLoU review; not read. |
| https://www.gameskinny.com/tips/the-last-of-us-part-1-best-pc-graphics-and-performance-settings/ | HTTP 403 | TLoU settings tips; not read. |
| https://overclock3d.net/reviews/software/the_last_of_us_part_1_early_pc_performance_review/5/ | HTTP 403 | TLoU review; not read. |
| https://support.forza.net/hc/en-us/articles/10944080013843-NVIDIA-DLSS-AMD-FSR-and-DirectX-Ray-Tracing-Improvements-in-Forza-Horizon-5 | HTTP 403 | Forza support; not read. |
| https://horizon.fandom.com/wiki/Horizon_Forbidden_West_Updates | HTTP 402 | Patch list; not read. |
| https://support.insomniac.games/hc/en-us/articles/4403977358861-Version-1-002-Release-Notes | HTTP 404 (page gone) | Rift Apart 1.002 notes; replaced by the Push Square story (S69). |
| https://blog.playstation.com/2022/07/13/marvels-spider-man-remastered-pc-features-and-graphics-settings/ | HTTP 404 | PlayStation Blog PC features; not read. |
| YouTube embed of mAzinClQ_1o | player error | The embedded player showed a configuration error; only the watch page (S8) loaded. |
| All other URLs from WebSearch results (Steam forums, guide sites, store pages, retailer pages, other ComputerBase pages) | search result only, not opened | No claim in this report rests on them. |

