# R3 · Accessibility guidance and component choice for a game settings menu

Project: FRONTROOMSSS (first-person horror, Unity 6 URP; Mac, Windows, iPhone, iPad, foldable iPhone, Android; controller later).
Scope: research only. No design decisions are made here.

## Method

- The material was gathered on 2026-10-24, 07:27 to 07:50 PDT, by the original research agent. That agent stopped (account limit) before writing anything.
- This report was written up from the agent's saved fetches on 2026-10-24. No research was redone for the write-up. New WebFetch calls made for the write-up: 0 of the 6 allowed.
- Source of truth: `raw/R3_access_raw.md`. Its blocks marked FETCH are pages that were opened (188 distinct URLs). No NAV or BROWSER block exists: the original agent used no browser. One BATCH call was empty and failed. Blocks marked SEARCH are search result lists only.
- The raw file keeps only the first 2,500 characters of each fetch. Where that cut off the useful text (the Microsoft Learn XAG pages, several Apple HIG data feeds, the Windows app docs), the full text of the same fetch was read from the original agent's saved session log. That log is the same material, not new research.
- Most fetches returned a short summary written by the fetch tool, not the raw page. Those are paraphrased here. Direct quotes appear only where the fetch gave verbatim text or the full page was available. Quotes are under 15 words.
- Rule used here: every claim cites a URL that was opened. A URL seen only in a search result is listed as "search result only, not opened" and supports no claim.
- Pages that returned only a title, a 403/404/405 error, a refused connection or a limit message were opened but give nothing. They sit in the SOURCES table with a note and support no claim. Anything not confirmed in a fetched page is marked "unverified".
- Nothing was downloaded. All sources are links only. Video links were read as page titles only; no video was watched.
- Source numbers like [S12] point to the SOURCES table at the end. Section B labels its cases A1, A2 and so on, as the spec asked. "Inference" marks a point drawn by this write-up from the sources, not stated by them.

## A. Guidance that bears on a settings menu

### A.1 Game Accessibility Guidelines (GAG)

The site sorts guidelines into Basic, Intermediate and Advanced levels [S1]. The full list has about 150 titles and none of them is titled "presets" [S2]. The preset advice sits inside one guideline (row G4 below).

| # | Guideline title (exact; cut where over 14 words) | Level · category | What it says that matters for the menu | FrontRooms row it touches |
|---|---|---|---|---|
| G1 | Ensure that all settings are saved/remembered | Basic · General | Remember settings across sessions, per game or per platform, so players customise once. [S4] | All rows |
| G2 | Provide details of accessibility features in-game | Basic · General | Players rarely go looking for features. Show the settings menu before play, teach features in the opening tutorial, mention them on loading screens. [S3] | First-launch prompt |
| G3 | Allow the game to be started without the need to navigate through… (title cut here; full title in the URL) | Basic · Cognitive | Offer a quick start. Deep menus block players with short-term memory or dyspraxia. [S5] | Menu depth |
| G4 | Allow gameplay to be fine-tuned by exposing as many variables as possible | Intermediate · General | Expose existing variables. Warns of an "overwhelming array of options". Quote: "Presets and grouping can help greatly with this." [S8] | Grouping, presets |
| G5 | Allow settings to be saved to different profiles, at either game or platform level | Advanced · General | Shared hardware means shared settings. Profiles let each player keep their own. [S10] | Later, not now |
| G6 | Include an option to adjust the sensitivity of controls | Basic · Motor | Give a wide range, inside the game, for every input type (mouse, touch, tilt, stick). Lower sensitivity can also reduce simulation sickness. [S7] | LOOK SPEED, GYRO LOOK |
| G7 | Include toggle/slider for any haptics | Basic · Motor (also Cognitive, Intermediate) | A toggle at minimum, a strength slider is better. If haptics serve several purposes, give each its own control. [S20] | HAPTICS · GAMEPLAY, HAPTICS · CONTROLS |
| G8 | Ensure that all areas of the user interface can be accessed using… (title cut; full title in the URL) | Basic · Motor | If gameplay works by keyboard or pad, the menus must too. Common failure: keyboard gameplay with mouse-only menus. [S11] | Every component |
| G9 | Ensure interactive elements / virtual controls are large and well spaced, particularly on… (title cut; full title in the URL) | Basic · Motor, Vision | About 2.4 cm is ideal on touch screens. Minimum on phones is 0.96 cm. These are floors, not targets. [S12] | Touch targets |
| G10 | Make interactive elements that require accuracy (eg. cursor/touch controlled menu options) stationary | Intermediate · Motor | Menus whose targets move under the cursor or finger are hard to hit. [S18] | Moving or animated rows |
| G11 | Avoid / provide alternatives to requiring buttons to be held down | Intermediate · Motor | Let players pick hold or toggle for sprint-like actions. Toggles are a little slower; auto-holds give less control; so offer the choice. [S23] | BREAK GLASS (hold/tap), RUN (stick/button) |
| G12 | Include a cool-down period (post acceptance delay) of 0.5 seconds between inputs | Advanced · Motor | After an accepted input, ignore further input briefly. Helps players with tremor. [S24] | Possible tension with hold-to-repeat (see C.2) |
| G13 | Allow interfaces to be resized | Intermediate · Motor, Vision | Give a good default, then a setting. Some players want smaller UI that blocks less of the view. [S25] | CONTROLS SIZE |
| G14 | Support more than one input device | Intermediate · Motor | No device is more accessible than another. Players may switch for fatigue. [S28] | Keyboard, mouse, touch, pad parity |
| G15 | Allow controls to be remapped / reconfigured | Basic · Motor | Offer both presets and a custom option. In-game remapping updates on-screen prompts. [S16] | Controller support later |
| G16 | Use an easily readable default font size | Basic · Vision, Cognitive | Cites a 28 px minimum at 1080p as a floor (an Amazon 10-foot UI figure). [S17] | Row and description text |
| G17 | Allow the font size to be adjusted | Advanced · Vision | One default size cannot suit every screen and distance. [S13] | Text size row (not in today's list) |
| G18 | Provide high contrast between text/UI and background | Basic · Vision | Cites a 4.5:1 minimum ratio. Use plain backgrounds, or outlines and shadows. [S29] | Menu skin |
| G19 | Provide an option to adjust contrast | Intermediate · Vision | A high-contrast mode via a filter, alternate assets or a tintable layer. [S26] | Not in today's list |
| G20 | Ensure subtitles/captions are or can be turned on before any sound is played | Intermediate · Hearing | Opening cinematics are the most common gap. Default on, or let players enable before the first sound. [S14] | CAPTIONS, first launch |
| G21 | Allow subtitle/caption presentation to be customised | Intermediate · Hearing | Text size, background opacity slider, font style, speaker names and colour, captions. [S15] | CAPTIONS |
| G22 | Avoid flickering images and repetitive patterns | Basic · Cognitive | Warn clearly and offer an option to turn triggers off. Never call a setting "epilepsy safe". See A.5. [S9] | REDUCE FLASHING |
| G23 | Avoid (or provide option to disable) any difference between controller movement and camera movement | Intermediate · Vision | Bob, shake, tilt, smoothing and motion blur can make a game unplayable. Include for immersion, but offer an off switch. [S19] | CAMERA MOTION |
| G24 | Provide an option to turn off / hide background movement | Intermediate · Cognitive, Vision | Moving backgrounds distract players, for example with ADHD. [S6] | Animated menu backdrop |
| G25 | Avoid any sudden unexpected movement or events | Advanced · Cognitive | Sudden shocks can force an autistic player to stop at once. Avoid them where the mechanic does not need them. [S22] | Horror pacing; no setting today |
| G26 | Give a clear indication that interactive elements are interactive | Intermediate · Cognitive, Vision | Use consistent style or contrast. Mentions a hover effect; says nothing about focus highlights. [S21] | Row affordance |
| G27 | Use simple clear language | Basic · Cognitive | Use the most direct wording. Reading ability is the most common barrier in games, the page says. [S150] | Option names and descriptions |
| G28 | Provide a choice of text colour, low/high contrast choice as a minimum | Intermediate · Cognitive | Very high contrast can hurt some readers with dyslexia; offer a choice. [S148] | Not in today's list |
| G29 | Provide separate volume controls or mutes for effects, speech and background / music | Basic · Hearing | One slider per audio class. [S27] | Sound rows (not in today's list) |
| G30 | Offer a wide choice of difficulty levels | Basic · General | Naming must not demean lower levels. [S151] | RELAY READOUT is an assist; see case A11 (Celeste) |
| G31 | Allow difficulty level to be altered during gameplay, either through settings or adaptive difficulty | Intermediate · General | Players cannot know the right level before they try. [S149] | Settings reachable from pause |

Notes on this table.
- The "Include an option to adjust the game speed" title appears in the full list as Basic under Motor and Intermediate under Cognitive. Its page was not opened [S2].
- Other titles in the full list that touch menu content (titles and levels only, from the list; their pages were not opened) [S2]: a field-of-view adjustment option (Intermediate, Vision) and an appropriate field-of-view default (Basic, Vision); "Include assist modes such as auto-aim and assisted steering" (Intermediate, General); "Include contextual in-game help / guidance / tips" (Intermediate, Cognitive); "Ensure no essential information is conveyed by a fixed colour alone" (Basic, Vision); "Provide a stereo/mono toggle" (Intermediate, Hearing); "Provide an option to disable blood and gore" (Advanced, Cognitive); "Provide an option to turn off / hide all non interactive elements" (Advanced, Cognitive).
- Row G2 and row G3 pull in opposite directions: show accessibility options early, yet keep the way into play short. Inference: a single skippable first-launch screen satisfies both.
- Row G4 is the only opened GAG page that mentions presets or grouping.

### A.2 Xbox Accessibility Guidelines (XAG)

The Microsoft Learn index says version 3.2 was published on Jun 8, 2023. It says the XAGs are not a compliance checklist. [S30] A second index lists XAG 101 to 123: Text display, Contrast, Additional cues, Subtitles and captions, Audio accessibility, Screen narration, Input, Difficulty options, Object clarity, Haptic feedback, Audio descriptions, UI navigation, UI focus handling, UI context, Error messages, Time limits, Visual distractions and motion, Photosensitivity, Chat, Communication experiences, Accessible feature documentation, Accessible customer support, Mental health best practices. [S36]

The pages below were opened in full. XAG 109, 111, 115, 119, 120, 122 and 123 were not opened (titles only, from the index).

| XAG | Topic | Requirements that bear on settings (paraphrase) | FrontRooms rows it touches |
|---|---|---|---|
| 101 | Text display | Console text at least 26 px at 1080p, PC/VR 18 px at 1080p, mobile 18 px at 100 DPI (scales linearly). Text must scale to 200% without loss of content or two-direction scrolling. Offer a sans serif option; if a stylised font is used, offer a plain one. Allow sentence case instead of all caps for lines of text; one or two word labels are exempt. [S31] | Row labels, option descriptions, CONTROLS SIZE |
| 102 | Contrast | Standard text and key elements 4.5:1. Large text 3:1. Inactive text 3:1. Offer a high contrast mode at 7:1. Read the platform contrast setting at first launch. Do not rely on colour alone. [S32] | Focus colour, disabled rows |
| 103 | Additional cues | Critical visual info needs a second channel (audio, haptic, narration). Info given by colour needs shape, pattern or text too. If colour is the main signal, let players choose the colours. [S33] | RELAY READOUT, state of toggles |
| 104 | Subtitles and captions | Subtitles on by default, or adjustable before play starts. Toggle caption types independently. Describe each caption setting plainly rather than make players guess. Background opacity 0 to 100%. Size up to 200%. At most two lines. Show a preview of the current look. [S34] | CAPTIONS |
| 105 | Audio accessibility | Independent volume or mute for music, voice-over, active effects, ambience, narration. Offer mono. [S45] | Sound rows (not in today's list) |
| 106 | Screen narration | Menu elements that are lists, tabs or sliders announce their type, value and position, for example "slider, 38%, 6 of 9". A switch to turn the count off can be provided. [S46] | Every component (screen readers) |
| 107 | Input | Menus work by digital input alone and by single, non-simultaneous presses. Everything remappable in game. Activate on the up event, and let players cancel by moving away before release. Offer alternatives to multi-point and path-based gestures. Sensitivity adjustable by at least 50% of default. Touch: targets at least 15 mm (phones) or 24 mm (tablets); size, spacing and position adjustable; preset layouts; customisation screen itself accessible; portrait and landscape. [S43] | Every component, touch rows, LOOK SPEED |
| 108 | Difficulty options | Ideally four or more difficulty presets, changeable at any time without losing progress, with discrete assists for single mechanics. Names must not insult the player. [S47] | RELAY READOUT as an assist |
| 110 | Haptic feedback | Players can turn feedback off, adjust strength, and get other channels as well. [S44] | HAPTICS · GAMEPLAY, HAPTICS · CONTROLS |
| 112 | UI navigation | From first launch, every route to the accessibility settings is accessible. Either prompt for accessibility settings as the first screen, or meet the XAGs by default. Same order and same prompts on every screen. Fully navigable by keyboard and by controller digital input. Linear menus loop from last item to first; developers are encouraged to offer a looping on/off option. Persistent link back. Tabs by LB/RB are cited as a consistent mechanic. [S37] | Menu skeleton |
| 113 | UI focus handling | Every element shows a highly visible focus indicator (border, size or weight change, combinations). Focus is always visible and never moves offscreen. In a dialog, focus moves to its first control. [S38] | Focus style on every row |
| 114 | UI context | Labels sit next to their element. Context-sensitive help for each element where needed. Provide a visual simulation of what a setting does, ideally in a real game context. Menu text at lower secondary reading level. Context changes not started by the player need a notice. [S39] | Description panel, previews |
| 116 | Time limits | For UI time limits: use only if essential; let players turn them off or extend them to at least ten times; give at least 20 seconds and a simple action to extend. [S40] | Confirm-or-revert timers (see C.7) |
| 117 | Visual distractions and motion | Provide a way to pause, hide or disable moving or blinking content. Avoid camera shake, bobbing and blur or offer an off switch. Examples cite sliders from 0 to 100% (Halo Infinite) and head bob intensity sliders (Elder Scrolls Online). Offer field of view and camera sensitivity settings. [S41] | CAMERA MOTION, REDUCE FLASHING |
| 118 | Photosensitivity | Flash = 10% luminance change, darker side below 0.8. Fail if more than about three flashes a second over about 20% of the screen. Red flashes and high-contrast bands are tested separately. [S42] | REDUCE FLASHING |
| 121 | Accessible feature documentation | Describe accessibility features in easy-to-find online documentation, and in the in-game help if there is one. [S48] | Page text, store page |

Short quotes that carry weight (all from the pages above):
- On focus order: "The movement of focus should always be predictable." [S37]
- On subtitle settings: the page says detailed descriptions help players who would otherwise learn "by trial and error". [S34]
- On first launch (paraphrase): the game may prompt for accessibility as its first screen; Minecraft Dungeons is the cited example, and it reads the Xbox platform setting so the screen can read itself aloud. [S37]

Differences between sources, noted so the design is not built on one number:
- XAG 118 uses "about 20 percent" of the screen. GAG uses 25% for flashes and 40% for static patterns [S9]. WCAG uses a 0.006 steradian area inside a 10 degree field, about a quarter of it [S84].
- Touch target: Apple 44 x 44 pt default [S58], XAG 15 mm on phones [S43], GAG 0.96 cm minimum [S12], Material 48 dp default for slider thumbs [S72]. They use different units (pt, mm, dp). Converting needs each device's pixel density, which no opened page gives, so whether they agree is unverified. The synthesis lists them side by side.

### A.3 Component rules of thumb (when each control fits)

Apple's HIG pages (sliders, steppers, toggles) returned only their titles when fetched as web pages. Their text was read from Apple's HIG data feed (`.json`) for the same page. Material 3 pages (`m3.material.io`) also returned only titles; the Material Components docs for Android and the Android Compose page were used instead. Material 3 rules themselves are therefore unverified [S69, S68, S67].

| Component | Rule of thumb, with sources (paraphrase; short quotes marked) |
|---|---|
| Slider | Use for approximate values. NN/g: "Use sliders only for approximate values." Avoid when an exact value matters; offer tap or type as an alternative. Keep ranges narrow and sparse. Put labels above or beside the track, not below, because a finger hides them. [S79] |
| Slider (range and count) | Microsoft: use a slider for contiguous or discrete values the user thinks of as a relative quantity, and only when there are four or more values; with fewer, use radio buttons. If most users pick one of a few representative values, use only those as steps. [S179] |
| Slider (precision and feedback) | Pair the coarse slider with a numeric box for fine control. A slider needs live feedback; if the change takes more than about 0.1 s to show, a slider is a poor fit. Mark the default on the track. [S82] Apple: live feedback, tick marks, optional labelled ends, hover tooltip with the value; wide ranges can pair slider, text field and stepper. [S52] |
| Slider (tvOS) | Apple lists sliders as not supported on tvOS. Inference: a slider on a TV-style or controller-only menu needs another pattern (stepper or discrete segments). [S52] |
| Stepper | NN/g: fits numeric fields with a clear most-common value that users adjust only slightly. Avoid for wide ranges or exact amounts. Buttons large, laid out horizontally (minus left, plus right), press-and-hold repeats, optional text field for far values. [S77] Apple: a stepper shows no value itself, so pair it with a value; fine alone for small changes that take a few taps; on macOS support Shift-click for ten times the step. [S53] |
| Toggle / switch | NN/g: apply the change immediately, with no Save button; if that is not possible, use radio buttons or a single checkbox. Short, concise labels that describe what the switch does when on. [S78] Microsoft: for binary operations that take effect right after flipping; label with one or two words, preferably nouns. [S182] Apple: two opposing states; do not rely on colour alone to show state; on iOS the switch style is for list rows only. [S57] Material (Android library): a switch stands for one binary state, controls one item, and its effect should start at once [S71]. NN/g's video page says the same: toggles take effect immediately [S177]. |
| Switch label | W3C ARIA: the label stays the same when the state changes; the state is carried by `aria-checked`. Space toggles (Enter optional). [S130] |
| Segmented control | Apple: closely related choices that affect an object, state or view. Limit to about five on iPhone and five to seven on wide screens. Keep segments equal in size and use text or images, not both. [S55] On tvOS the segment becomes selected as soon as focus reaches it, so keep other focusable items away from it. [S55] Android Compose: single-select for one choice from a small side-by-side set; multi-select works for two to five items; use chips beyond five. [S74] |
| Segmented control (Android library) | Multiple selection is the default. Set single selection when one option must be active, and "selection required" so the group cannot end up empty. The page notes segmented buttons are being deprecated in Material 3 Expressive in favour of connected button groups. [S75] |
| Radio buttons | Microsoft: when users need to see all options before choosing; for exactly two options use a toggle or checkbox; up to eight options, beyond that a combo box; for a range of values use a slider. [S181] Microsoft settings page: "up to 5" mutually exclusive options. [S180] Apple (macOS): two to five; beyond about five use a pop-up button. [S57] |
| Pop-up / dropdown | Apple: a flat list of mutually exclusive options, with a useful default and an optional Custom item. [S54] NN/g: fewer than about 5 options, use radio buttons; about 5 to 10 is the dropdown range; defaults can be a trap because users keep them. "Treat dropdown lists as a tradeoff, not a default." [S80] |
| Picker (long list) | Apple: for medium-to-long lists, shown in context, not in a new view. For a short list a pull-down is better. [S56] |
| List rows | Apple: option lists give brief feedback, then show a checkmark. Keep row text short. [S65] |
| Disclosure group | Apple: keep advanced features hidden by default; label the group by what it reveals ("Advanced Options"); at most one disclosure control per view. [S63] Microsoft: do not nest expanders deeper than one level. [S180] |
| Tabs / sections | W3C ARIA: arrow keys move between tabs and wrap; activate on focus only if panels appear with no delay. [S134] Apple: a segmented control suits closely related subviews; for separate app sections use a tab bar. [S55] |
| Reset button | NN/g: most Reset buttons should be removed. Acceptable in complex dialogs with good default values, where it restores defaults. [S76] |
| Haptics control | Apple: let people turn haptics off and make sure the experience works without them. [S152] GAG and XAG 110: a toggle at minimum, strength if possible, one control per use. [S20, S44] |
| Motion option | Apple: make motion optional; do not use motion as the only way to carry important information. [S60] Apple's Accessibility page: when the system Reduce Motion setting is on, reduce automatic and repetitive animation (zooming, scaling, peripheral motion); ways include fades in place of x, y and z transitions and no animation into or out of blurs; respect Dim Flashing Lights for video. [S58] XAG 117 cites 0 to 100% sliders for blur, shake and speed-line effects. [S41] Inference: read the system Reduce Motion value as the starting default for CAMERA MOTION, as Apple advises using the player's accessibility settings for defaults [S61]. |
| Text size and contrast (Apple) | Apple's Accessibility page: let people enlarge text or icons by at least 200%; type defaults and minimums are 17 and 11 pt on iOS and iPadOS, 13 and 10 on macOS (the games page repeats the table); contrast to WCAG AA (4.5:1 for text up to 17 pt, 3:1 from 18 pt, 3:1 for bold); offer a higher-contrast scheme when the system Increase Contrast is on; do not rely on colour alone; make core functions reachable by more than one input type, and support Full Keyboard Access and Switch Control. [S58] [S61] |

Size and spacing for touch and pointer, all from opened pages:
- Apple: default control 44 x 44 pt on iOS and iPadOS, minimum 28 x 28 pt; about 12 pt of padding around elements that have a bezel, about 24 pt around those that do not. [S58]
- Apple buttons: hit region at least 44 x 44 pt. [S153]
- Apple game controls: at least 44 x 44 pt for frequent controls, 28 x 28 pt for less important ones. [S62]
- Material (Android) slider thumb: 48 dp minimum touch target by default. [S72]
- XAG 107: 15 mm on phones, 24 mm on tablets (scaled by DPI). [S43] GAG: 0.96 cm minimum on phones. [S12]
- These figures use different units (pt, mm, dp). Converting them needs each device's pixel density, which no opened page gives. Whether they agree is unverified. For the foldable (outer 678 x 466 pt, inner 951 x 669 pt) check the real physical size of a 44 pt target on the device before choosing a floor.

### A.4 Settings-page structure, defaults, presets and adaptive layout

Platform and trade guidance on how a settings screen as a whole should behave.

| Topic | What the sources say | Sources |
|---|---|---|
| Defaults | Apple: good defaults mean people may not need to change anything; optimise performance for the device rather than ask. Use device information for defaults: display resolution, paired controllers, the player's accessibility settings. | [S59, S61] |
| Fewer settings | Apple: too many options make an experience less approachable and any one setting harder to find. Microsoft's app-settings page says to keep a page to "a maximum of four or five" settings and group related ones under section headers. Inference: that figure is for simple app pages; a game with 20 or more rows needs groups, and each group should stay short. | [S59, S180] |
| Access point | Apple: the conventional route to settings in games is the Escape key. Apple's controller table gives the Menu button the job of opening settings or pausing. GAG and the Game Developer checklist both want settings reachable before a game starts. | [S59, S62, S3, S144] |
| Do not ask for what can be detected | Apple: a game can recognise a connected controller, so it should not ask. Do not duplicate system-wide settings such as accessibility. Tension: GAG and XAG want in-game accessibility options and want platform settings read as defaults. Reading the platform value as the starting default satisfies both. | [S59, S32] |
| Row anatomy | Microsoft's settings cards have a header, an optional description, an optional icon and one action control aligned to the right. Related rows sit under a section header; sub-options hide in an expander; single column, scrolling top to bottom, with a constrained maximum width. | [S180] |
| Apply at once | Microsoft: when a user changes a setting, the app should immediately reflect the change. NN/g: toggles apply immediately; checkboxes and radio groups apply on a confirming button. | [S180, S78, S81] |
| Checklist for game settings | A Game Developer article lists: settings reachable before a game starts; consistent menu controls; mouse, thumbstick and d-pad support; Back goes up one level only; warn about unsaved changes; one quality option with at least four preset levels plus individual quality settings; live preview of screen effects; ability to disable intrusive effects; separate volume channels; separate inverts; separate mouse X and Y sensitivity; easy reset of key bindings. (Author and date not captured.) | [S144] |
| Quality presets | Unity: quality levels are named presets; a matrix assigns levels to platforms, one default per platform; each level carries the render pipeline asset, pixel light count, anti-aliasing, vsync and shadow settings, among others. Whether levels switch at run time is not stated on that page. | [S143] |
| Frame rate and motion defaults | Apple: default to smooth performance, usually 30 to 60 fps, with good default graphics settings per platform; let people optimise for performance or battery. | [S60] |
| Display modes as access | AbleGamers "Flexible Displays": let players trade frame rate against resolution (Nioh, Onrush cited). | [S126] |
| Keep settings | AbleGamers "Leave It There": save display, control and interface settings between sessions; Metal Gear Solid 5 keeps display settings and asks the player to confirm changes before applying them. | [S127] (index of all 11 patterns: [S125]) |
| Personal interface | AbleGamers: let players show, hide, resize, pin and rearrange interface parts; Fortnite Mobile changes button size and icon scale independently; Horizon Zero Dawn offers "Always on", "Dynamic" and "Always off" for HUD elements. | [S128] |
| Improved precision | AbleGamers: sliders for sensitivity; PUBG has general, vertical and targeting sensitivity; Battlefield 1 has per-zoom sliders from 10% to 200%. | [S129] |
| Adaptive menus | Apple: keep in-game menus legible across aspect ratios (16:10, 19.5:9, 4:3) and in both orientations without covering essential content; use relative constraints. | [S61] |
| List and detail on a wide window | Android: a settings screen is named as one possible use of list-detail. Large windows show list and detail side by side; small windows show one pane at a time, and Back returns to the list. | [S185] |
| Size classes, not device types | Android: do not decide layout from "tablet" or from physical size; foldables and resizable windows break that. Width classes: compact under 600 dp, medium 600 to under 840, expanded 840 to under 1200, large 1200 to under 1600, extra-large from 1600. | [S186, S187] |
| Split view on iPhone | Apple: use split views in a regular horizontal environment, not a compact one; on iPad test narrow, compact and intermediate widths and keep pane movement logical. | [S184] |

Notes.
- XAG 112 sets a bar for first launch: either prompt for accessibility settings first, or meet the XAGs by default. Its examples of "by default" include captions on and visual distractions off. [S37]
- The Android size classes are in dp, not in the point sizes of the foldable (outer 678 x 466 pt, inner 951 x 669 pt). Mapping pt to dp for that device is unverified. The classes were not applied to the foldable in the sources.
- A Windows settings guide, a Game Developer checklist and platform HIGs are advice, not rules for games. Where a game-specific source disagrees, the game source is closer.

### A.5 Photosensitive epilepsy: facts for the REDUCE FLASHING description

| Source | Facts that matter | Link |
|---|---|---|
| WCAG 2.2 SC 2.3.1 (Level A) | No more than three flashes in any one second, or flashing below the general and red flash thresholds. A general flash is a rise and fall in relative luminance of at least 10% of maximum, with the darker state below 0.80. A red flash is an opposing pair of transitions involving saturated red. Area test: 0.006 steradians inside a 10 degree field, about a quarter of it. A 341 x 256 px block on a 1024 x 768 screen approximates 10 degrees. The thresholds were adapted from broadcast guidelines. | [S84] |
| WCAG 2.2 SC 2.3.2 (Level AAA) | No more than three flashes in one second, with no area or red exception. | [S89] |
| WCAG 2.2 glossary entry | The fetch was cut off after the opening clause. The exact thresholds from the glossary were not read there. Unverified beyond the 2.3.1 page. | [S83] |
| XAG 118 | Same luminance definition (10%, darker side under 0.8). Fail when flashes exceed about three a second and cover about 20% of the screen; low-intensity flashing for long periods can also fail. Red flash: saturated red, R/(R+G+B) of 0.8 or more. Spatial pattern: alternating bands with more than 10% contrast over about 20% of the screen. Fixes: lower contrast, frequency or size; desaturate red. | [S42] |
| GAG "Avoid flickering images and repetitive patterns" | Risk cannot be removed entirely. Recommends clear warnings and an option to turn triggers off. Thresholds on the page: more than 3 flashes in one second over 25% or more of the screen; moving patterns 25%; static patterns 40%; sudden brightness changes, fast cuts and changes to or from red as triggers; extra care in VR. The page recommends the Harding test and says it does not guarantee safety. It does not mention ITU or WCAG. | [S9] |
| GAG wording rules | Quotes: "ensure that adequate and obvious warning is given"; "the term 'epilepsy safe' must never be used." A setting called that risks harm to players and legal action. | [S9] |
| ITU-R BT.1702 | Title: "Guidance for the reduction of photosensitive epileptic seizures caused by television". BT.1702-3 (11/2023) is in force. The page gave no scope text, so the content is unverified. | [S86] |
| Harding test | An automated check for television. Built by Cambridge Research Systems from Graham Harding's research; implements Ofcom guidelines; logs luminance flashes, red flashes and spatial patterns frame by frame. Its own site refused the connection, so product detail is unverified. | [S87] [S85] |
| Epilepsy Society | About 3% of people with epilepsy have photosensitive epilepsy. Seizures are commonly triggered at 3 to 30 flashes per second; 16 to 25 is most likely. Risk rises with tiredness, excitement, effects that fill the field of view, fast changing light and dark patterns, and flashing against a dark background such as a screen in a dark room. Long play sessions add risk. Advice: sit well back, take breaks, turn down brightness. | [S90] |
| Alan Wake II (patch notes via Can I Play That) | Added a lower intensity choice for horror flashes, "low" or "normal", covering visual and audio intensity. The reviewer calls its main value player safety and quotes a player: "you don't have to have epilepsy to experience a seizure." | [S176] |

What the sources imply for the option text (inference):
- Say what is reduced (flashes, strobing lamps, flash brightness), not that the player is made safe.
- Do not use the words "epilepsy safe" or "safe mode".
- Put a plain warning where players first meet the flashing content, and make the option reachable at first launch and from pause.
- The Epilepsy Society names excitement, a dark room and a close screen as risk factors. A horror game played in the dark on a phone can combine all three. This is an inference from that list.

## B. Cases: accessible settings menus in shipped games

How to read this section.
- Each case says only what an opened page says. "Not stated" means the opened pages are silent, not that the game lacks the feature.
- Most games' official pages are feature lists, not menu walk-throughs. No screenshot or video file was opened. Footage links are collected after the cases and were read as titles only.
- Cases A1 to A14 are single games. A15 gathers the games that Microsoft's XAG pages describe in detail.

### A1 · The Last of Us Part II
- Sources: [S92] [S93] [S95] [S112]
- Structure: settings grouped under Input Remap, Holds and Presses, Assistance, Magnification and Visual Aids, Motion Sickness, Navigation and Traversal, Text-to-Speech and Audio Cues, Audio Volumes, Combat Accessibility, HUD, Subtitles, Game Difficulty. The studio says "more than 60 accessibility settings". Difficulty is split into separate sliders for player, enemies, allies, stealth and resources.
- Explaining options: each option has a name, its values (On/Off, a list, or a numeric slider) and a short written description. Some add who the option suits or how it interacts with another. Some have demonstration videos. An Audio Cue Glossary lets players browse and hear the cues. In-menu previews are not described.
- Presets: three (vision, hearing, motor). Each is a single On/Off switch that turns on a recommended group of settings spread over several menus. Individual options can be changed afterwards.
- First launch: the official page and the studio post do not say. A third-party database lists this game first among games that ask players on first launch to confirm control, navigation and accessibility options. [S172]
- Reset to default, focus, tab switching: not stated.
- Footage: the official page mentions demonstration videos; none was opened.
- Lesson for FrontRooms: a preset can be a plain on/off row that sets a bundle, yet leave every row editable. FrontRooms' groups can borrow the same plain names (holds and presses, motion, haptics).

### A2 · The Last of Us Part I
- Sources: [S96] [S97]
- Structure: themed sections with three presets at the top that switch on recommended settings for vision, hearing and motor needs. Each option sits in a table with its settings and a short explanation; some note who they are recommended for or use sliders.
- First launch: Push Square says the first thing on starting is a set of accessibility options, not a main menu or a cutscene. The article does not say whether the screen can be skipped or whether it holds presets. The PlayStation Blog does not mention a first-launch prompt, and says the screen reader turns on automatically for English players who have the console reader enabled.
- Reset, focus, tab switching: not stated.
- Lesson for FrontRooms: a first-launch screen can be a simple list of the few rows that block play (captions, flashing, motion). Inference: the choice between "ask first" and "default safe" (XAG 112) can be both: default safe, then offer the screen.

### A3 · God of War Ragnarök
- Sources: [S91] [S94] [S113] [S114] [S116] [S170] [S171]
- Structure: four preset groups (Vision, Hearing, Motion reduction, Motor) above eleven categories: Alternative controls, Hold and presses, Assistance, Subtitles and captions, Visual aids, Motion reduction, Navigation and traversal, Audio cues and screen reader, Audio tuning, HUD, Game difficulty. The page claims over 70 features. Motion reduction covers camera shake, sway, blur, grain and a centre dot. Visual aids include flash reduction.
- Presets: each preset has levels. The PlayStation page says Off, "some" and "full". The Can I Play That review says two levels, Some and Full. Both agree if "Off" is the cleared state. Options changed by a preset show in blue so players can spot them. Applying Off, or holding the reset-all button, restores defaults. The Can I Play That reviewer says the preset list has no previews, that presets rarely matched their own needs, and that the option count is overwhelming.
- Explaining options: the full menu has sub-menus; every option has an explainer and many have preview images (Can I Play That).
- First launch: Can I Play That says players can start unchanged or open the presets first. Ross Minor says the game speaks a prompt to press Square to turn on the screen reader, and the main menu offers the presets at once. He adds that some options also appear in the menus where they fit best, as well as in the dedicated menu.
- Criticism: the blue marker relies on colour alone (the reviewer suggests an icon too); the screen reader does not cover most in-game menus (skills, map, codex).
- Controller navigation and tab switching: not stated in any opened page. The GGRecon guide opened does not cover them either.
- Lesson for FrontRooms: show changed-from-default rows with a mark that is not colour alone; offer a hold-to-reset-all; give "Off / Some / Full" style levels only where the bundle really has levels.

### A4 · Forza Motorsport (2023)
- Sources: [S101] [S178]
- Both pages are the same Xbox Wire announcement, published before launch. The en-us fetch says the features are planned, not confirmed as shipped.
- Menu features announced: an accessibility menu where players read detailed descriptions of each audio cue and preview it before driving. Screen narration with adjustable volume, pitch, speed and voice, plus verbosity settings for list positions, navigation keywords and button hints, to avoid overload. Several contrast levels for UI and HUD. Menu text in several sizes; some HUD resizing. Subtitle size and background opacity.
- First launch, presets, reset, tab switching: not stated.
- Related XAG examples from Forza Horizon 4: focus order that follows the layout [S37]; LB and RB for tabs used everywhere, a lock icon plus dimmed look plus "Not Available" text for restricted items, and context help text under the focused item [S39].
- Lesson for FrontRooms: where an option changes something the player cannot judge from words (a sound, a flash), let them preview it in the menu. A narration verbosity switch is the model for a "description detail" setting.

### A5 · Gears 5
- Sources: [S102] [S100]. The older address [S98] only redirected.
- Features listed: colourblind mode; font size (the page says it enlarges some UI text, not all); subtitles with a dark backing, speaker names and size; narrator for menus on Xbox One, Windows 10 and Steam, with coverage that varies by screen and known bugs listed; separate sliders for master, music, effects, dialogue, cinematics and environment audio; tap challenges replaced by holds; aim toggle; single stick movement; left-handed layouts; invert axes; vibration toggles; camera shake off in the campaign for some difficulty levels.
- Not mentioned on the page: high contrast, a first-launch accessibility screen, presets, game speed.
- Lesson for FrontRooms: say plainly where a setting does not reach (the page admits font size does not affect all text). Aim for a setting that reaches every screen, or label its limits.

### A6 · Marvel's Spider-Man 2
- Sources: [S109]. The official PlayStation page returned 404 and the Insomniac support page 403. [S105] [S106]
- Only one article was readable, and it does not describe menu structure, presets, descriptions or first launch. It lists: challenge-level modifiers (enemy health and damage, stealth difficulty, dodge and parry timing, auto aim, puzzle simplification, chase assist), game speed settings, audio frequency controls (high cut, low cut, a notch filter) and planned later additions (audio description, screen reader, captions).
- Lesson for FrontRooms: assists can be granular, one per mechanic. This matches XAG 108's advice to adjust the difficulty of single mechanics one at a time, and suits RELAY READOUT. Menu structure is unverified.

### A7 · Ratchet & Clank: Rift Apart
- Sources: [S110]. The official PlayStation page (404) and the Insomniac support page (403) gave nothing. [S103] [S107]
- Controller remapping lets players customise or "choose between presets". Shader options can be applied as presets or set one by one. Vibration can be set to experimental, functional or off, with adjustable intensity; adaptive triggers have the same three modes; the controller speaker has its own volume.
- Menu layout, previews, reset and first launch: not stated.
- Lesson for FrontRooms: a three-mode haptics row (Off / Functional / Full) with a separate intensity matches GAG and XAG 110: off, strength, and other channels as well. [S20] [S44]

### A8 · Assassin's Creed Valhalla
- Sources: [S174] [S173]
- First launch: a screen before the main menu. It opens with an option to turn on menu narration, then content settings (blood, dismemberment), controls, icon size, text size, and basic subtitle and caption settings. The review does not say how to return to it. The full options menu is separate and offers more, including presets and sliders. The review's criticisms concern in-game cues, not this screen.
- Announced features (not tested by the reviewer): narration of settings and most in-game menus with adjustable pace and volume; menu font size and icon size, three profiles each, added because the earlier game's menu text was too small; subtitles in three sizes with an optional background; HUD elements chosen one by one.
- Lesson for FrontRooms: the first-launch screen asks about what the player cannot recover from alone: narration, content, text size. Keep it to a handful of rows and keep the full menu one step away.

### A9 · Assassin's Creed Mirage
- Sources: [S118] [S119]
- First launch: subtitles and menu narration turn on automatically; narration becomes audible once the title screen loads. A shortcut in the bottom-right corner of the title screen turns on menu narration.
- Findability: no dedicated accessibility tab; options are spread over Controls, Sound, Interface and Gameplay. The reviewer lists this as a weakness. The Controls tab shows a preview beside the options and flags binding conflicts.
- Ubisoft's spotlight: d-pad menu navigation with shortcuts, text size, icon size, HUD widget background and opacity, colourblind presets, audio dynamic range presets, menu hold timing. It does not mention a first-launch setup or a way to preview changes.
- Lesson for FrontRooms: defaults on for narration-type aids and a visible shortcut on the title screen; avoid scattering related rows across tabs. A single "Accessibility" group, even if rows also appear elsewhere, helps findability (compare A3).

### A10 · Senua's Saga: Hellblade II
- Source: [S117]
- Almost every category has its own accessibility sub-menu, so options are not lumped into one long list. Motor options sit under Settings, then Gameplay, then Accessibility. The reviewer found the structure kept the list from feeling overwhelming. The description of the highlighted setting shows on the right of the screen. A colourblind filter has an intensity slider. Subtitles have small and large presets.
- Criticisms are about the game, not the menu. First launch, reset, previews and tab switching: not stated.
- Lesson for FrontRooms: a description panel on the right that follows focus, with accessibility rows grouped inside each category, is a ready pattern for the wide layouts. On the narrow layouts the description has to move (see C.5).

### A11 · Celeste (Assist Mode wording)
- Sources: [S121] [S124]. A university blog page was opened and says nothing about the screen. [S123]
- The intro text acknowledges that players differ and presents the mode as a way to keep enjoying a hard game. The Game Accessibility Guidelines case study calls this wording non-patronising. Vice reports the intro describes the game as "challenging, but accessible", suggests first-time players skip Assist Mode, and says every player is different. The developer says the mode is optional, not the default, and that he wants players to move between settings without judgement.
- Options named: game speed, stamina, air dashes, and the ability to die. Step sizes, access path and warnings are not stated in the opened pages.
- Lesson for FrontRooms: RELAY READOUT is an assist that shows the enemy's state. Describe it as a choice about how much to see, in the game's own voice, without saying it is easier or weaker.

### A12 · Hades (God Mode)
- Sources: [S122] [S120]
- Players can turn God Mode on or off at any time in the settings menu (Inverse). It starts at 20% damage reduction and adds 2% per death, up to 80% (Inverse; the Can I Play That page confirms the 20% and 2% but not the cap). The in-game wording was not captured. The creative director says the name fits the game's theme of a powerful character, and the gradual rise lets players keep learning from their deaths.
- Lesson for FrontRooms: name an assist in the fiction's terms, state the numbers, and allow switching at any time.

### A13 · Returnal
- Sources: [S111] [S108]. The official PlayStation page returned 404. [S104]
- The launch menu is sparse with only a few options; the reviewer was first worried, then found much more in the full settings (subtitle, audio, aim, control, display). Two control schemes serve as presets. Holds can become toggles for sprint, scanning and "risky interactions". Haptic and trigger intensity can be lowered or turned off.
- Criticisms: raising subtitle size does not change the in-game menu text; menu text is small; the minimap cannot be enlarged. How options are explained: no text or tooltips described.
- Lesson for FrontRooms: one text-size setting must reach the menu itself, and a thin first menu makes players worry that nothing else exists. Say where the rest is.

### A14 · Alan Wake II (horror flash option)
- Source: [S176] (a Can I Play That news item on the patch).
- A patch added a lower intensity for horror flashes, "low" or "normal", covering both visual and audio intensity. The article does not give the exact label or where it sits in the menus. The reviewer says its main value is player safety, and pushes back on reading it as a way to make the game less scary.
- A second page on this game's settings returned HTTP 405. [S175]
- Lesson for FrontRooms: a horror game already shipped a flash-intensity choice with two values and a safety framing. REDUCE FLASHING can be written the same way (see A.5).

### A15 · Games that Microsoft's XAG pages describe in detail

These come from the XAG pages, which describe menus screen by screen. They show how focus, tabs and previews work in shipped menus.

| Game | What the XAG page shows | Lesson for FrontRooms | Page |
|---|---|---|---|
| Minecraft Dungeons | The first screen on first launch is an accessibility menu: text-to-speech on/off, subtitles on/off, enemy outline colour, chat wheel type. It reads the Xbox platform setting and, if "Let Games Read to Me" is on, reads itself aloud. | A first-launch screen of four rows; read the platform setting for the starting value. | [S37] |
| Sea of Thieves | Select and back prompts sit in the same bottom-left place on every screen. A selects, B goes back, LT and RT move through pages, RB and LB move through tabs. Menus are mostly linear. | Fix the prompt strip's position; use the same four verbs everywhere. | [S37] |
| Grounded (PC) | Menus work by keyboard alone (Tab, arrows, Space), including a Quit entry; also by controller and mouse. A tooltip for the "Arachnophobia Safe Mode" preview is narrated with its control hint. | Keyboard-only parity; give every control a short purpose line. | [S37] [S39] |
| Forza Horizon 4 | A single-column difficulty list loops from last to first. A tiled menu does not loop and follows the visual layout. Context help under the focused item ("Change[s] the name of your adventure"). Groups of options carry labelled headings. | Loop only linear lists; put a one-line help text under the focused row. | [S37] [S39] |
| The Outer Worlds | Focus shows as an outline, a contrasting block fill and a heavier font weight together. A label and its slider sit inside one focus box. At the largest text size the layout reflows with no two-way scrolling. | Combine fill, outline and weight for focus; make the row, not the thumb, the focus target. | [S38] [S39] [S37] |
| Fallout 76 (pause menu) | Focus is a high-contrast yellow backdrop plus a short animated character to the left of the item. | An extra non-colour marker beside the row. | [S38] |
| Immortals Fenyx Rising | Navigating to certain options shows an in-game preview of how each choice changes the player's UI. | Preview for text size, controls size, opacity (the FrontRooms touch rows). | [S39] |
| Halo Infinite | Sliders from 0 to 100% for radial blur, screen shake, full-screen effects and speed lines. Field of view has explanatory text about the "fishbowl" effect. Look sensitivity is set per axis. | CAMERA MOTION could be a 0 to 100% slider, not three fixed steps; say what each end does. | [S41] |
| Cyberpunk 2077, Elder Scrolls Online | Amount of extra camera movement adjustable; head bob intensity on a sliding scale. | Same as above: intensity, not only on/off. | [S41] |
| HyperDot | An "Animate Background" setting makes the moving menu backdrop static. | Offer a still menu background if the title menu animates. | [S41] |
| Call of Duty: Mobile | Players resize and reposition each touch target and save layouts. Swipe sensitivity is set per camera movement type. Options include "always sprint". | Matches CONTROLS SIZE, LOOK SPEED and RUN (stick/button). | [S43] |
| Fortnite (mobile) | Three preset HUD layouts, labelled by the play they suit. Three ways to fire. | Preset layouts with plain names beside size and opacity sliders. | [S43] |
| Diablo Immortal | Two ways to cancel a skill press: slide out of range, or drag to a Cancel icon. | Touch rows need a way to cancel a drag (see C.4). | [S43] |

### Cross-case comparison (what the opened pages state)

| Game | First-launch accessibility screen | Presets | Reset to default | How options are explained | Findability notes |
|---|---|---|---|---|---|
| The Last of Us Part II | Listed by a third-party database as asking on first launch; official pages silent | 3 (vision, hearing, motor), single On/Off, editable after | Not stated | Name, values, short description; demo videos for some | 60+ settings in 12 groups |
| The Last of Us Part I | Accessibility options are the first thing shown | 3, same as above | Not stated | Table with short explanation per option | Themed sections |
| God of War Ragnarök | Start unchanged or open presets; spoken prompt for the screen reader | 4 groups x Off/some/full | Off preset or hold reset-all | Explainer per option; preview images for many | 11 categories; options also appear in context; blue marks changed rows |
| Forza Motorsport | Not stated | Not stated | Not stated | Audio-cue descriptions with previews (announced) | Narration verbosity settings (announced) |
| Gears 5 | Not mentioned on the page | Not mentioned | Not stated | Not stated | Feature list only |
| AC Valhalla | Screen before the main menu (narration, content, controls, sizes, subtitles) | In the full menu (review) | Not stated | Not stated | Return path to first-launch screen not stated |
| AC Mirage | Subtitles and narration on by default; title-screen narration shortcut | Colourblind and audio presets (Ubisoft) | Not stated | Preview beside controls | No dedicated accessibility tab (a stated weakness) |
| Hellblade II | Not stated | Subtitle size presets | Not stated | Description panel on the right | Accessibility sub-menu in every category |
| Returnal | Minimal launch menu | Two control schemes | Not stated | Not described | Reviewer worried by thin launch menu |
| Minecraft Dungeons | Accessibility menu is the first screen; reads platform setting | Not stated | Not stated | Not stated | 4 rows only |

Sources for the table are the case entries above. Cells that say "Not stated" mean the opened pages are silent.

### Footage links collected from the XAG pages (titles read, videos not watched)

Microsoft's XAG pages link short YouTube clips. Each URL was opened and only the video title came back. They are the nearest thing to menu footage in the opened material. The "Supports" column is read from the title and the XAG page that links the clip, not from the video.

| Video title | URL | Supports |
|---|---|---|
| XAG 104 - Gears 5: Subtitles on Initial Launch | https://www.youtube.com/watch?v=LeGSqMo3c3U [S159] | Captions available at first launch |
| XAG 107 - Gears 5: Control Remapping | https://www.youtube.com/watch?v=tJRrUNQSqnU [S161] | Remap screen |
| XAG 107 - Minecraft: UI Navigation Using Single, Non-simultaneous Key Presses | https://www.youtube.com/watch?v=2gQcoi2qNqw [S156] | Keyboard menu navigation |
| XAG 107 - Call of Duty Mobile Customizable Touch Controls | https://www.youtube.com/watch?v=Wdgqe4Ja_WU [S168] | Touch layout editing |
| XAG 112 - Sea of Thieves: Consistent UI Navigation | https://www.youtube.com/watch?v=tfo7_Cwzt_c [S162] | LB/RB tabs, prompt strip |
| XAG 112 - Grounded: Keyboard-only Navigation | https://www.youtube.com/watch?v=AZWR7Yp30k4 [S167] | Keyboard-only menus |
| XAG 112 - Grounded: Multi-modal UI Navigation | https://www.youtube.com/watch?v=TyG0yE0Dl3Q [S165] | Controller, mouse and keyboard parity |
| XAG 106 - Forza Horizon 4: Focus Order Aligned to Meaning / Operation of UI | https://www.youtube.com/watch?v=sBxvuq1iVNY [S164] | Focus order |
| XAG 106 - Forza Horizon 4: Narration of Value Change | https://www.youtube.com/watch?v=PTHDC3q1Y6Q [S155] | Value announced on change (slider-like rows) |
| XAG 106 - Forza Horizon 4: Narration of Interactable UI Elements | https://www.youtube.com/watch?v=sTn6e0eV9eE [S160] | Control type announced |
| XAG 106 - Assassin's Creed Menu Narration Pace | https://www.youtube.com/watch?v=X5j5zMA3tRA [S163] | Narration pace (from the title; content unverified) |
| XAG 106 - Assassin's Creed UI Refocus | https://www.youtube.com/watch?v=k1M44e4XV0k [S154] | Narration when focus returns (from the title; content unverified) |
| XAG 114 - Fenyx Immortals Rising Settings Preview | https://www.youtube.com/watch?v=uwov8u-E8r0 [S157] | In-menu preview |
| XAG 114 - Grounded: Tool Tip Context | https://www.youtube.com/watch?v=ManAQvvrIDw [S166] | Tooltip text |
| XAG 117 - Hyperdot: Disabling Background Animations | https://www.youtube.com/watch?v=3HLhVbCzwOY [S158] | Still menu backdrop |
| XAG 103 - Forza Horizon 4: Filters for Color Blindness | https://www.youtube.com/watch?v=70_fZ3oyd_4 [S169] | Filter options |

Screenshots: the XAG pages describe screenshots (for example the Minecraft Dungeons first-launch screen and a Sea of Thieves menu) in alt text, but the image addresses were not captured in the fetches. No screenshot URL is cited.

### Gaps in section B
- No first-hand menu walk-through was read for any game. The official PlayStation pages for Spider-Man 2, Ratchet & Clank and Returnal returned 404, and Insomniac's support pages returned 403. Spider-Man 2 and Ratchet & Clank rest on one partial article each.
- Controller tab switching in God of War Ragnarök and The Last of Us is not described in any opened page. The best evidence for LB/RB is in XAG examples (Sea of Thieves, Forza Horizon 4).
- Reset to default is documented only for God of War Ragnarök.
- Hellblade II, AC Mirage, Returnal, AC Valhalla and God of War Ragnarök are covered through Can I Play That reviews; those were the only reviews that described menus.

## C. Input behaviour patterns in game menus

Each point says what the cited page states. Points marked "unverified" have no opened page behind them.

### C.1 Switching tabs and groups

- Controller: bumpers switch tabs. Sea of Thieves uses "RB and LB for moving through tabs" and LT and RT for pages. [S37] Forza Horizon 4 uses LB and RB between menu tabs on every screen, which the page names as consistent use. [S39]
- Apple's controller table for UI outside gameplay: A activates; B cancels or goes back; left and right shoulder go to the previous or next screen or section; thumbsticks and D-pad move selection; Menu opens settings or pauses; X, Y and triggers have no defined menu behaviour. [S62]
- Microsoft's UWP accelerator table: triggers page up and down, bumpers page left and right, View opens or closes the navigation pane, Y is search, Menu opens a context menu. Triggers therefore differ between Apple (undefined) and Microsoft (paging). [S135]
- Apple: label the buttons for the controller in use, and show symbols rather than text. [S62] XAG 112: prompts sit in the same place and order on every screen. [S37]
- Keyboard: W3C tabs pattern uses Left and Right arrows between tabs, wrapping at the ends, Home and End optional; Tab moves into the tab list and then on to the panel. Activate on focus only when the panel appears with no delay. [S134] Apple (iPadOS): Tab moves between focus groups; arrow keys move within a group. [S66]
- Q and E as keyboard tab keys: unverified. Only forum and feature-request pages came up in search, and none was opened.
- Pointer and touch: click or tap the tab. Apple: a segmented control suits closely related subviews; for separate app sections use a tab bar. [S55]
- Wrap-around: XAG 112 wants linear lists to loop from last to first, and encourages an on/off option for looping. Grids should not loop. [S37]
- Unity: the UI Toolkit TabView page lists no keyboard or gamepad behaviour; focus order follows `focusable` and `tabindex`. [S138] Navigation events (Move, Submit, Cancel) are tied to input actions, not devices. [S139]

### C.2 Value rows with a d-pad, stick or arrow keys

- Engagement. Microsoft: a slider on a gamepad traps focus, because Left and Right move the thumb instead of moving to the next control. Two fixes: stack controls vertically so Left and Right only mean "change value", or require the player to press A to engage the slider and B to leave it. Slider engagement is on by default in that framework; list-like controls are off by default. [S135]
- Step size. Unity UI Toolkit slider: arrow keys move 1/100 of the range, Shift moves 1/10, Home and End jump to the ends, Page Up and Page Down move by the page size. [S137] W3C slider: arrows move one step; Page Up and Page Down move more than one; Home and End jump; no numbers given. [S131] W3C spin button: arrows one step, Page keys a larger step, with an example of 1 and 10. [S133]
- Hold to repeat. NN/g steppers: let users hold a button to change the value continuously. [S77] Apple (macOS): support Shift-click for ten times the step. [S53] Unity Input System UI module has Move Repeat Delay (initial pause) and Move Repeat Rate; the page gives no defaults and notes frame rate caps the rate. [S140] A game forum post reports a menu where keyboard repeat works but a held controller direction does not repeat; the developer agreed to fix it. [S146]
- Hold to accelerate (speed rising the longer you hold): no opened source describes it. Unverified.
- Tension to resolve (inference): GAG advises a 0.5 s post-acceptance delay between inputs for tremor, as an Advanced item [S24], while sliders benefit from fast repeat. Sources do not say how to reconcile them; a repeat-delay setting is one way, but no source proposes it.
- Coarse and fine. NN/g: pair a coarse slider with a numeric box; mark the default with a tick. [S82] Microsoft: use discrete steps when users pick from a few representative values; show tick marks when step points are not obvious. [S179]
- tvOS: sliders and steppers are listed as not supported. [S52] [S53]
- Screen reader: a slider row announces "slider, 38%, 6 of 9" style text (type, value, position); the page allows an option to turn the count off. [S46]
- Segment rows on controller: on tvOS a segment becomes selected as soon as focus reaches it. Keep other focusable controls away. [S55] Inference: for rows with side effects (a graphics preset), move focus first and apply on A.

### C.3 Mouse

- Drag the thumb; click the track. Unity: `page-size` sets how far the thumb jumps on a track click; 0 moves it straight to the pointer. A numeric input field is optional. [S137] Microsoft: the thumb moves by tapping the track or scrubbing. [S179]
- Hover. Apple: a tooltip with the thumb value on hover. [S52] GAG: a hover effect shows interactivity, but the page says nothing about focus highlights. [S21]
- Activation timing. XAG 107: activate on the up event, and let the player cancel by moving away before release; essential down-event actions are exempt. [S43]
- Mouse wheel on sliders and lists: unverified. The one page that documents it (Qt's slider class) was not read; the fetch hit the account limit. [S188]
- Unity pitfall: the UI Input Module clears the selection on a background click by default. Turn this off so keyboard and gamepad navigation keep working after a click. Do not route a gamepad to navigation and to a Virtual Mouse at once, or input is processed twice. In UI Toolkit, a gamepad submit raises a navigation event, not a click. [S140]
- Gamepad pointer: Microsoft recommends turning off mouse mode on Xbox and using focus navigation. [S135] Apple (tvOS): avoid a free-moving pointer for menus. [S66]
- One input must reach everything: GAG, XAG 107 and XAG 112 all say so. [S11] [S43] [S37]

### C.4 Touch

- Target size: see A.3. Apple 44 x 44 pt default, 28 x 28 pt minimum, 12 pt padding. XAG 15 mm. GAG 0.96 cm. [S58] [S43] [S12]
- Sliders. NN/g: expect imprecise, one-handed use; long thin tracks cause accidental nudges; keep labels above or beside, not below; offer tap or type alternatives. [S79] Microsoft: the slider's touch target is large; keep it away from the display edge; if the page pans one way, orient the slider the other way so a swipe does not change the value. [S179]
- Dragging is a path-based gesture. XAG 107 asks for a way to operate content without path-based gestures. Inference: a slider row needs a second way to change the value (tap to step, plus and minus buttons, or a stepper). [S43]
- Cancel. XAG 107: act on touch-end, and give a way to cancel a press by sliding away. Diablo Immortal's slide-out is the cited example. [S43]
- Switch style: on iOS use the switch only inside list rows; outside lists use a toggle-style button. [S57]
- Segment count on phones: no more than about five. [S55] Android Compose: use chips beyond five. [S74]
- Safe areas and corners: keep controls inside safe areas and clear of the home indicator and the camera cut-out. [S62]
- Customisation screens must themselves be accessible. XAG 107 names resizing, moving and preset layouts. [S43]
- Haptic tick per step (inference): Apple warns that a haptic that is fine once can tire when repeated, and says to let people turn haptics off. [S152] This applies to a tick on every slider step, and relates to the HAPTICS · CONTROLS row.

### C.5 Description panel that follows focus

- Hellblade II shows the description of the highlighted setting on the right of the screen. [S117]
- The Last of Us Part II puts a short description under each option; God of War Ragnarök has an explainer per option and preview images for many. [S93] [S94]
- XAG 114 asks for context-sensitive help for each element, and a visual simulation of the setting. Forza Horizon 4 puts the help under the focused button. [S39]
- Microsoft's settings card carries a header and an optional description per row. [S180]
- Wide window: list on one side, detail on the other. Narrow window: one pane at a time, Back returns to the list. [S185] Apple: split views belong to regular widths, not compact. [S184]
- Pointer and keyboard should give the same text: Apple uses a hover tooltip on sliders, and XAG 114 wants tooltips narrated. [S52] [S39]
- Wording: simple, direct language. XAG 114 sets a lower secondary reading level for settings text; GAG says to use the most direct wording. [S39] [S150]
- Where the description goes on a narrow phone layout is not decided by any opened source. Inference: under the focused row, or as a footer strip.

### C.6 Reset to default

- NN/g: most Reset buttons should go. Reset is acceptable in complex dialogs with good default values, where it restores defaults. [S76] The page gives no guidance on placement or confirmation.
- God of War Ragnarök: an Off preset and a reset-all hold button restore every altered value; changed values show in blue. [S91] [S114]
- Game Developer checklist: key bindings should be easy to reset. [S144]
- Marking the default on the track: NN/g suggests a tick at the default value. [S82]
- Apple's Settings page was checked for reset guidance and has none. [S59]
- Confirmation before reset-all: no opened source covers it. Inference: Apple asks for confirmation on destructive pull-down items, which is the closest rule. [S64]
- Per-row reset (for example on Y or in a context menu): no source describes it. Microsoft lists Menu for a context menu and Y for search on the gamepad. Unverified. [S135]

### C.7 Apply, confirm and revert

- Immediate versus applied. NN/g: toggles apply at once; checkboxes and radio buttons apply when a confirming button is pressed. [S78] [S81] Microsoft: the app should immediately reflect a changed setting. [S180]
- Unsaved changes: warn before leaving the menu. Back goes up one level only. [S144]
- Metal Gear Solid 5 asks the player to confirm display changes before applying them. [S127]
- Engine support: Unreal's `UGameUserSettings` has `ConfirmVideoMode` (mark the mode confirmed), `RevertVideoMode` (go back to the last confirmed mode), `ApplySettings` (apply and save) and `IsDirty` (any setting differs). [S141]
- Wording of a keep-or-revert dialog: GNOME's display dialog has "Keep changes" and "Revert settings". A bug had the buttons doing the opposite; the page names no countdown and no default button. [S142]
- Countdown length: unverified. A claimed 15 second Windows timer came only from search results that were not opened.
- Time-limit rules apply (inference): XAG 116 and WCAG 2.2.1 say UI time limits should be adjustable, extendable with at least 20 seconds notice, or switchable off, unless essential. A revert timer that rescues a player from an unusable screen may count as essential, but neither source says so. [S40] [S136]
- Custom state: Apple lets a pop-up button carry a Custom item. A preset row can show "Custom" once a detail option diverges. [S54] (inference for the preset use)
- Unity quality levels are named presets assigned per platform. [S143]

### C.8 Focus display and focus movement

- XAG 113: every element shows a highly visible focus indicator (border, size, or weight, or a mix); focus is never invisible, never offscreen; in a dialog it moves to the first control. [S38]
- Microsoft: keep one focus visual on screen at all times, put initial focus top-left, and have a focusable item present at all times. Focus shows by default for gamepad but not for keyboard in that framework. [S135] Inference: in a game menu that takes keyboard, show focus for keyboard as well, as XAG 113 requires of all elements.
- Apple: do not move focus without the player's input, except when a focused item disappears under discrete input. Five visual states on tvOS: unfocused, focused, highlighted, selected, unavailable. [S66]
- XAG 102: unavailable text still needs 3:1 contrast. [S32] XAG 103: do not signal "unavailable" by greying alone. [S33]
- XAG 112: if a control takes over input (a remap mode), say clearly how to leave it. [S37]

### C.9 Phones, tablets and the foldable

- Layout follows the window, not the device: Android says to use window size classes and not hardware values, because foldables and resizable windows break device-type assumptions. [S186] The class table is in A.4. [S187]
- Apple: keep menus legible across aspect ratios and both orientations, using relative layout. [S61] XAG 107: support portrait and landscape. [S43]
- Android's list-detail page does not cover foldable behaviour; it only names `HingeInfo` and `Posture` as building blocks. [S185] No opened source describes a settings menu on a foldable. Unverified for the 678 x 466 pt and 951 x 669 pt screens.

## Synthesis: rule table (row type, component, behaviour per input, accessibility notes)

How to use it. Each rule names the FrontRooms rows it would cover. "(inf.)" marks a step this write-up infers from the cited sources. Where sources conflict or are silent the cell says so. These are rules found in sources, not design decisions.

| # | Row type (FrontRooms rows) | Component | Keyboard | Mouse | Controller | Touch | Accessibility notes | Sources |
|---|---|---|---|---|---|---|---|---|
| 1 | On/off, applies at once (HDR RENDER, REDUCE FLASHING, CAPTIONS, INVERT LOOK, LEFT-HANDED, STICK HINTS, RELAY READOUT) | Toggle switch inside a list row | Space toggles; Enter optional | Click the row or the switch | A toggles in place | Tap the whole row; switch style only in list rows on iOS | Label does not change with state. Label says what ON does, in one or two words. State not by colour alone. Announced as on or off. | [S78] [S182] [S57] [S130] [S62] [S71, S177] |
| 2 | Two named options that are not "on" and "off" (BREAK GLASS hold or tap, RUN stick or button, STICK floating or fixed) | Two-segment control, equal width, text labels | Left and Right move and select; wraps | Click a segment | Left and Right change it (inf.: apply on A if the change has side effects, because tvOS segments select on focus) | Tap a segment | Show both options at once. Use a toggle only when On and Off describe the action. Hold-or-toggle choices meet GAG's "alternatives to holding". | [S55] [S182] [S181] [S132] [S23] |
| 3 | Three to five named levels (CAMERA MOTION off, 50, 100; GYRO LOOK; haptics modes; graphics preset) | Segmented control, up to about five; more than five becomes a pop-up list; a numeric scale of four or more values can be a slider with stops | Left and Right; arrows wrap in a radio group | Click | Left and Right | Tap; no more than about five on a phone | Text or icons, not both. Fewer than four values: Microsoft says use radio buttons, not a slider. | [S55] [S74] [S179] [S132] |
| 4 | Approximate amount with a live effect (LOOK SPEED 60-150%, CONTROLS SIZE 80-140%, CONTROLS OPACITY 20-65%, haptic strength, caption background opacity) | Slider with a value label, tick at the default, a sensible step, and the live effect visible | Left and Right one step; Shift or Page keys larger step; Home and End to the ends | Drag; click track (jump to pointer or step by page size); hover shows value; wheel unverified | Left and Right step; held direction repeats; press A to engage if neighbours would trap focus, or stack rows vertically so Left and Right only change the value | Drag, plus tap-to-step or plus and minus buttons (inf.: dragging is a path-based gesture); label above, not below; keep off the screen edge | Announce type, value and position. Value with units. Track and thumb at least touch size. Not for exact values. | [S79] [S82] [S179] [S137] [S131] [S135] [S46] [S43] |
| 5 | Small exact changes from a clear default (none today; fits text size steps or a numeric sensitivity) | Stepper with minus left and plus right, a visible value, optional typed value | Up and Down arrows one step; Page keys larger (spin button) | Click; hold repeats | Left and Right; hold repeats; tvOS lists steppers as unsupported | Large buttons, horizontal | Pair with the value; add a field when the target is far from the default. | [S77] [S53] [S133] |
| 6 | Long or open list (resolution, display mode, language) and a preset row that can become "Custom" | Pop-up list or picker, shown in context; useful default; optional Custom item | Open, arrow through, confirm, cancel (standard behaviour; no opened page lists keys) | Click to open | A opens, B closes; focus goes to the first item in the open list | Large rows; stay on the same screen | About 5 to 10 options is the dropdown range; fewer: radio; more than about 15: searchable list. | [S54] [S80] [S56] [S38] |
| 7 | Groups and categories (all rows; today only small section labels) | Section headers; a few rows per group; advanced rows behind one disclosure level; category tabs on wide layouts; category list then page on narrow ones | Tab between groups, arrows within; Left and Right on tabs | Click tab or header | LB and RB for tabs; B goes up one level; list loops | Tap category; Back to list | Same order on every screen. Persistent way back. Grouping and presets keep options from overwhelming. Microsoft says keep a settings page to about four or five items; that is for simple apps (inf.: groups of that size, not a whole menu). | [S8] [S37] [S63] [S180] [S134] [S62] [S185] |
| 8 | Description and preview (every row) | One-line description that follows focus; preview for visual or audio effects | Shows on focus | Shows on hover and on focus | Shows on focus | Under the row or as a footer strip (inf.) | Plain language, lower secondary reading level. Tooltip text is narrated. Preview in a realistic context. | [S117] [S39] [S34] [S150] |
| 9 | Safety rows (REDUCE FLASHING, CAMERA MOTION) | REDUCE FLASHING: toggle, or two levels as Alan Wake II did. CAMERA MOTION: intensity control; sources show 0 to 100% sliders | As rows 1 and 4 | As rows 1 and 4 | As rows 1 and 4 | As rows 1 and 4 | Say what is reduced. Never "epilepsy safe". Warn where flashing is met. Reachable at first launch and from pause. Name the effects (bob, shake, blur). | [S9] [S42] [S176] [S41] [S19] |
| 10 | Haptics and on-screen control rows (HAPTICS · GAMEPLAY, HAPTICS · CONTROLS, CONTROLS SIZE, CONTROLS OPACITY, LEFT-HANDED, STICK) | Toggle plus strength slider per haptic use; size and opacity sliders with the real on-screen controls as the preview; preset layouts | As rows 1 and 4 | As rows 1 and 4 | As rows 1 and 4 | As rows 1 and 4; the layout editor must itself be usable | One control per haptic purpose. Experience works without haptics. Presets and a resizable layout meet XAG 107. | [S20] [S44] [S152] [S43] [S128] [S25] |
| 11 | Reset to default | Per-group reset or hold-to-reset-all; mark changed rows with more than colour; tick at the default on sliders | Hold a key (inf.) | Click a labelled Reset in each group | Hold a button (God of War uses a hold) | Reset row at the end of a group | Most standalone Reset buttons should go; keep it where good defaults exist. Confirm destructive resets (inf., from Apple's rule for destructive items). | [S76] [S91] [S94] [S82] [S64] |
| 12 | Apply, confirm and revert (HDR RENDER, graphics preset, resolution) | Most rows apply at once. Display changes apply, then offer keep or revert | Enter keeps, Esc reverts (inf.) | Click Keep or Revert | A keeps, B reverts (inf.) | Two large buttons | A countdown is a time limit: let it be extended or turned off unless essential (inf.). Countdown length unverified. Warn on leaving unapplied changes. | [S78] [S144] [S141] [S142] [S40] [S136] |
| 13 | First launch and findability | One short screen: captions, flashing and motion, text size; defaults read from the platform; skippable; reachable again from the title and pause menus | Same keys as the menu | Same | Same | Same | Subtitles and narration on by default or settable before play. Visual distractions off by default. Do not scatter related rows across tabs. | [S37] [S3] [S14] [S174] [S118] [S170] |
| 14 | All rows: focus and navigation | One highly visible focus style (fill, outline and weight); focus never lost or offscreen; keys and buttons work in every part of the menu | Tab for groups, arrows within | Hover feedback; click activates on release, can cancel by moving away | D-pad or stick moves; A, B, LB, RB as in row 7; prompts in one fixed place | Targets at least the sizes in A.3 | Same input method as gameplay. No focus change without input. Focus shown for keyboard too (inf.). | [S38] [S66] [S11] [S43] [S37] |

### What the sources do not settle
- Hold-to-accelerate on a d-pad slider, and the exact repeat delay and rate: no opened source gives numbers. Unity's UI module has the two settings without defaults. [S140]
- Mouse wheel on sliders; Q and E as tab keys: unverified.
- Length of a keep-or-revert countdown: unverified. The time-limit rules in XAG 116 and WCAG 2.2.1 are the only opened guidance.
- Material 3 rules for sliders, switches and segmented buttons: unverified (pages returned titles only).
- ITU-R BT.1702 content and the Harding product: unverified (title only; site refused the connection).
- A settings menu on a foldable: no source. Only Android and Apple adaptive-layout pages were opened.
- Controller tab switching in The Last of Us and God of War Ragnarök: not stated in any opened page.
- Spider-Man 2 and Ratchet & Clank menu structure: official pages failed; thin evidence.

## SOURCES

All URLs below were opened by the original agent (FETCH blocks) between 07:27 and 07:50 PDT on 2026-10-24. The "accessed" column gives the date; per-page times were not logged. Pages that returned nothing usable are kept so the record is complete. Order follows first fetch.

| # | URL | What it shows | Owner | Accessed | Usage note |
|---|---|---|---|---|---|
| S1 | https://gameaccessibilityguidelines.com | GAG home page: the three level pages and site links | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. |
| S2 | https://gameaccessibilityguidelines.com/full-list/ | GAG full list: about 150 guideline titles by category and level (two fetches, same URL) | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. |
| S3 | https://gameaccessibilityguidelines.com/provide-details-of-accessibility-features-in-game/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, A.4, Synthesis. Guideline row G2. |
| S4 | https://gameaccessibilityguidelines.com/ensure-that-all-settings-are-savedremembered/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G1. |
| S5 | https://gameaccessibilityguidelines.com/allow-the-game-to-be-started-without-the-need-to-navigate-through-multiple-levels-of-menus/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G3. |
| S6 | https://gameaccessibilityguidelines.com/provide-an-option-to-turn-off-hide-background-movement/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G24. |
| S7 | https://gameaccessibilityguidelines.com/include-an-option-to-adjust-the-sensitivity-of-controls/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G6. |
| S8 | https://gameaccessibilityguidelines.com/allow-gameplay-to-be-fine-tuned-by-exposing-as-many-variables-as-possible/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, Synthesis. Guideline row G4. |
| S9 | https://gameaccessibilityguidelines.com/avoid-flickering-images-and-repetitive-patterns/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, A.2, A.5, Synthesis. Guideline row G22. |
| S10 | https://gameaccessibilityguidelines.com/allow-settings-to-be-saved-to-different-profiles-at-either-game-or-platform-level/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G5. |
| S11 | https://gameaccessibilityguidelines.com/ensure-that-all-areas-of-the-user-interface-can-be-accessed-using-the-same-input-method-as-the-gameplay/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, C.3, Synthesis. Guideline row G8. |
| S12 | https://gameaccessibilityguidelines.com/ensure-interactive-elements-virtual-controls-are-large-and-well-spaced-particularly-on-small-or-touch-screens/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in Method, A.1, A.2, A.3, C.4. Guideline row G9. |
| S13 | https://gameaccessibilityguidelines.com/allow-the-font-size-to-be-adjusted/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G17. |
| S14 | https://gameaccessibilityguidelines.com/ensure-subtitles-captions-are-or-can-be-turned-on-before-any-sound-is-played/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, Synthesis. Guideline row G20. |
| S15 | https://gameaccessibilityguidelines.com/allow-subtitle-caption-presentation-to-be-customised/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G21. |
| S16 | https://gameaccessibilityguidelines.com/allow-controls-to-be-remapped-reconfigured/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G15. |
| S17 | https://gameaccessibilityguidelines.com/use-an-easily-readable-default-font-size/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G16. |
| S18 | https://gameaccessibilityguidelines.com/make-interactive-elements-that-require-accuracy-eg-cursor-touch-controlled-menu-options-stationary/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G10. |
| S19 | https://gameaccessibilityguidelines.com/avoid-or-provide-option-to-disable-any-difference-between-controller-movement-and-camera-movement/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, Synthesis. Guideline row G23. |
| S20 | https://gameaccessibilityguidelines.com/include-toggle-slider-for-any-haptics/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, A.3, B (A7), Synthesis. Guideline row G7. |
| S21 | https://gameaccessibilityguidelines.com/give-a-clear-indication-that-interactive-elements-are-interactive/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, C.3. Guideline row G26. |
| S22 | https://gameaccessibilityguidelines.com/avoid-any-sudden-unexpected-movement-or-events/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G25. |
| S23 | https://gameaccessibilityguidelines.com/avoid-provide-alternatives-to-requiring-buttons-to-be-held-down/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, Synthesis. Guideline row G11. |
| S24 | https://gameaccessibilityguidelines.com/include-a-cool-down-period-post-acceptance-delay-of-0-5-seconds-between-inputs/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, C.2. Guideline row G12. |
| S25 | https://gameaccessibilityguidelines.com/allow-interfaces-to-be-resized/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, Synthesis. Guideline row G13. |
| S26 | https://gameaccessibilityguidelines.com/provide-an-option-to-adjust-contrast/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G19. |
| S27 | https://gameaccessibilityguidelines.com/provide-separate-volume-controls-or-mutes-for-effects-speech-and-background-music/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G29. |
| S28 | https://gameaccessibilityguidelines.com/support-more-than-one-input-device/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G14. |
| S29 | https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G18. |
| S30 | https://learn.microsoft.com/en-us/gaming/accessibility/guidelines | Microsoft Learn XAG index: version 3.2 (8 Jun 2023) and intro | Microsoft | 2026-10-24 | Used in A.2. |
| S31 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/101 | Microsoft Learn XAG 101 Text display: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2. |
| S32 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/102 | Microsoft Learn XAG 102 Contrast: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, A.4, C.8. |
| S33 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/103 | Microsoft Learn XAG 103 Additional cues: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, C.8. |
| S34 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/104 | Microsoft Learn XAG 104 Subtitles and captions: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, Synthesis. |
| S35 | https://learn.microsoft.com/en-us/xbox/accessibility/xag-version-history | Microsoft Learn XAG version history | Microsoft | 2026-10-24 | Opened, no usable content: output too large; only a header preview survives in the raw file; not read, not used. |
| S36 | https://devdocs.xbox.com/build/game-principles/accessibility/xbox-accessibility-guidelines | Xbox devdocs index listing XAG 101 to 123 with titles | Microsoft (Xbox) | 2026-10-24 | Used in A.2. |
| S37 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/112 | Microsoft Learn XAG 112 UI navigation: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, A.4, B (A4), B (A15), C.1, C.3, C.8, Synthesis. |
| S38 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/113 | Microsoft Learn XAG 113 UI focus handling: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, B (A15), C.8, Synthesis. |
| S39 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/114 | Microsoft Learn XAG 114 UI context: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, B (A4), B (A15), C.1, C.5, Synthesis. |
| S40 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/116 | Microsoft Learn XAG 116 Time limits: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, C.7, Synthesis. |
| S41 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117 | Microsoft Learn XAG 117 Visual distractions and motion: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, A.3, B (A15), Synthesis. |
| S42 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/118 | Microsoft Learn XAG 118 Photosensitivity: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, A.5, Synthesis. |
| S43 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107 | Microsoft Learn XAG 107 Input: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, A.3, B (A15), C.3, C.4, C.9, Synthesis. |
| S44 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/110 | Microsoft Learn XAG 110 Haptic feedback: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, A.3, B (A7), Synthesis. |
| S45 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/105 | Microsoft Learn XAG 105 Audio accessibility: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2. |
| S46 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/106 | Microsoft Learn XAG 106 Screen narration: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2, C.2, Synthesis. |
| S47 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/108 | Microsoft Learn XAG 108 Difficulty options: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2. |
| S48 | https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/121 | Microsoft Learn XAG 121 Accessible feature documentation: goal and implementation guidelines (full page) | Microsoft | 2026-10-24 | Used in A.2. |
| S49 | https://developer.apple.com/design/human-interface-guidelines/sliders | Apple HIG web page | Apple | 2026-10-24 | Opened, no usable content: returned only the page title; content was read from the matching .json feed instead. |
| S50 | https://developer.apple.com/design/human-interface-guidelines/steppers | Apple HIG web page | Apple | 2026-10-24 | Opened, no usable content: returned only the page title; content was read from the matching .json feed instead. |
| S51 | https://developer.apple.com/design/human-interface-guidelines/toggles | Apple HIG web page | Apple | 2026-10-24 | Opened, no usable content: returned only the page title; content was read from the matching .json feed instead. |
| S52 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/sliders.json | Apple HIG data feed, Sliders: abstract and best practices | Apple | 2026-10-24 | Used in A.3, C.2, C.3, C.5. |
| S53 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/steppers.json | Apple HIG data feed, Steppers: abstract and best practices | Apple | 2026-10-24 | Used in A.3, C.2, Synthesis. |
| S54 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/pop-up-buttons.json | Apple HIG data feed, Pop-up buttons: abstract and best practices | Apple | 2026-10-24 | Used in A.3, C.7, Synthesis. |
| S55 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/segmented-controls.json | Apple HIG data feed, Segmented controls: abstract and best practices | Apple | 2026-10-24 | Used in A.3, C.1, C.2, C.4, Synthesis. |
| S56 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/pickers.json | Apple HIG data feed, Pickers: abstract and best practices | Apple | 2026-10-24 | Used in A.3, Synthesis. |
| S57 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/toggles.json | Apple HIG data feed, Toggles: abstract and best practices | Apple | 2026-10-24 | Used in A.3, C.4, Synthesis. |
| S58 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/accessibility.json | Apple HIG data feed, Accessibility: abstract and best practices | Apple | 2026-10-24 | Used in A.2, A.3, C.4. |
| S59 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/settings.json | Apple HIG data feed, Settings: abstract and best practices | Apple | 2026-10-24 | Used in A.4, C.6. |
| S60 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/motion.json | Apple HIG data feed, Motion: abstract and best practices | Apple | 2026-10-24 | Used in A.3, A.4. |
| S61 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/designing-for-games.json | Apple HIG data feed, Designing for games: abstract and best practices | Apple | 2026-10-24 | Used in A.3, A.4, C.9. |
| S62 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/game-controls.json | Apple HIG data feed, Game controls: abstract and best practices | Apple | 2026-10-24 | Used in A.3, A.4, C.1, C.4, Synthesis. |
| S63 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/disclosure-controls.json | Apple HIG data feed, Disclosure controls: abstract and best practices | Apple | 2026-10-24 | Used in A.3, Synthesis. |
| S64 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/pull-down-buttons.json | Apple HIG data feed, Pull-down buttons: abstract and best practices | Apple | 2026-10-24 | Used in C.6, Synthesis. |
| S65 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/lists-and-tables.json | Apple HIG data feed, Lists and tables: abstract and best practices | Apple | 2026-10-24 | Used in A.3. |
| S66 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/focus-and-selection.json | Apple HIG data feed, Focus and selection: abstract and best practices | Apple | 2026-10-24 | Used in C.1, C.3, C.8, Synthesis. |
| S67 | https://m3.material.io/components/segmented-buttons/guidelines | Material Design 3 page | Google (Material Design) | 2026-10-24 | Opened, no usable content: returned only the page title; not used for claims (Material 3 rules unverified). Cited only to record this. |
| S68 | https://m3.material.io/components/switch/guidelines | Material Design 3 page | Google (Material Design) | 2026-10-24 | Opened, no usable content: returned only the page title; not used for claims (Material 3 rules unverified). Cited only to record this. |
| S69 | https://m3.material.io/components/sliders/guidelines | Material Design 3 page | Google (Material Design) | 2026-10-24 | Opened, no usable content: returned only the page title; not used for claims (Material 3 rules unverified). Cited only to record this. |
| S70 | https://raw.githubusercontent.com/material-components/material-components-android/master/docs/components/SegmentedButton.md | Material Components Android doc: SegmentedButton | Google (Material Components for Android) | 2026-10-24 | Opened, no usable content: HTTP 404. |
| S71 | https://raw.githubusercontent.com/material-components/material-components-android/master/docs/components/Switch.md | Material Components Android doc: Switch (when to use, accessibility) | Google (Material Components for Android) | 2026-10-24 | Used in A.3, Synthesis. |
| S72 | https://raw.githubusercontent.com/material-components/material-components-android/master/docs/components/Slider.md | Material Components Android doc: Slider (types, 48 dp touch target) | Google (Material Components for Android) | 2026-10-24 | Used in A.2, A.3. |
| S73 | https://raw.githubusercontent.com/material-components/material-components-android/master/docs/components/Button.md | Material Components Android doc: Button | Google (Material Components for Android) | 2026-10-24 | Opened, no usable content: no segmented-button guidance on the page; not used. |
| S74 | https://developer.android.com/develop/ui/compose/components/segmented-button | Android Compose: single and multi-select segmented buttons; chips beyond five | Google (Android) | 2026-10-24 | Used in A.3, C.4, Synthesis. |
| S75 | https://raw.githubusercontent.com/material-components/material-components-android/master/docs/components/ToggleButtonGroup.md | Material Components Android doc: toggle button groups (single select, selection required) | Google (Material Components for Android) | 2026-10-24 | Used in A.3. |
| S76 | https://www.nngroup.com/articles/reset-and-cancel-buttons/ | NN/g: when to avoid Reset | Nielsen Norman Group | 2026-10-24 | Used in A.3, C.6, Synthesis. |
| S77 | https://www.nngroup.com/articles/input-steppers/ | NN/g: stepper guidelines | Nielsen Norman Group | 2026-10-24 | Used in A.3, C.2, Synthesis. |
| S78 | https://www.nngroup.com/articles/toggle-switch-guidelines/ | NN/g: toggle-switch guidelines | Nielsen Norman Group | 2026-10-24 | Used in A.3, A.4, C.7, Synthesis. |
| S79 | https://www.nngroup.com/articles/gui-slider-controls/ | NN/g: slider rules of thumb | Nielsen Norman Group | 2026-10-24 | Used in A.3, C.4, Synthesis. |
| S80 | https://www.nngroup.com/articles/dropdown-list/ | NN/g: when dropdowns fit | Nielsen Norman Group | 2026-10-24 | Used in A.3, Synthesis. |
| S81 | https://www.nngroup.com/articles/checkboxes-vs-radio-buttons/ | NN/g: checkboxes vs radio buttons | Nielsen Norman Group | 2026-10-24 | Used in A.4, C.7. |
| S82 | https://www.nngroup.com/articles/sliders-knobs/ | NN/g: sliders, knobs and exact entry | Nielsen Norman Group | 2026-10-24 | Used in A.3, C.2, C.6, Synthesis. |
| S83 | https://www.w3.org/TR/WCAG22/#dfn-general-flash-and-red-flash-thresholds | WCAG 2.2 glossary: general flash and red flash thresholds | W3C | 2026-10-24 | Opened, no usable content: fetch cut off after the opening clause; thresholds not read here. Cited only to record this. |
| S84 | https://www.w3.org/WAI/WCAG22/Understanding/three-flashes-or-below-threshold.html | WCAG 2.2 SC 2.3.1 Understanding page | W3C | 2026-10-24 | Used in A.2, A.5. |
| S85 | https://www.hardingfpa.com/ | Harding FPA website | Harding FPA site | 2026-10-24 | Opened, no usable content: connection refused. Cited only to record this. |
| S86 | https://www.itu.int/rec/R-REC-BT.1702/en | ITU-R BT.1702 record page: title and version dates | ITU | 2026-10-24 | Partly useful: no scope text; content unverified. Used in A.5. |
| S87 | https://en.wikipedia.org/wiki/Harding_test | Wikipedia: Harding test | Wikipedia | 2026-10-24 | Used in A.5. |
| S88 | https://epilepsysociety.org.uk/sites/default/files/2025-04/PhotosensitiveepilepsyApril2025.pdf | Epilepsy Society PDF | Epilepsy Society | 2026-10-24 | Opened, no usable content: PDF text unreadable in the fetch. |
| S89 | https://www.w3.org/WAI/WCAG22/Understanding/three-flashes.html | WCAG 2.2 SC 2.3.2 Understanding page | W3C | 2026-10-24 | Used in A.5. |
| S90 | https://epilepsysociety.org.uk/about-epilepsy/epileptic-seizures/seizure-triggers/photosensitive-epilepsy | Epilepsy Society: photosensitive epilepsy facts | Epilepsy Society | 2026-10-24 | Used in A.5. |
| S91 | https://www.playstation.com/en-us/games/god-of-war-ragnarok/accessibility/ | PlayStation.com: God of War Ragnarok accessibility page (presets, categories) | Sony Interactive Entertainment | 2026-10-24 | Used in B (A3), C.6, Synthesis. |
| S92 | https://www.playstation.com/en-us/games/the-last-of-us-part-ii/accessibility/ | PlayStation.com: The Last of Us Part II accessibility page (presets, categories) | Sony Interactive Entertainment | 2026-10-24 | Used in B (A1). |
| S93 | https://naughtydog.com/blog/THE_LAST_OF_US_PART_II_ACCESSIBILITY_FEATURES_DETAILED | Naughty Dog blog: TLOU II accessibility features detailed | Naughty Dog | 2026-10-24 | Used in B (A1), C.5. |
| S94 | https://caniplaythat.com/?p=10258 | Can I Play That? review: God of War Ragnarok accessibility | Can I Play That? | 2026-10-24 | Used in B (A3), C.5, Synthesis. |
| S95 | https://caniplaythat.com/2020/06/18/the-last-of-us-part-ii-accessibility-review/ | Can I Play That? review: TLOU II accessibility | Can I Play That? | 2026-10-24 | Partly useful: lists settings by ability; no menu description. Used in B (A1). |
| S96 | https://www.pushsquare.com/features/how-accessibility-options-help-disabled-gamers-beat-the-last-of-us-part-i | Push Square: TLOU Part I first-boot accessibility options | Push Square | 2026-10-24 | Used in B (A2). |
| S97 | https://blog.playstation.com/2022/08/26/the-last-of-us-part-i-full-list-of-accessibility-features/ | PlayStation Blog: TLOU Part I accessibility list | Sony Interactive Entertainment | 2026-10-24 | Used in B (A2). |
| S98 | https://www.gears5.com/accessibility/ | gears5.com accessibility address | Gears of War site | 2026-10-24 | Opened, no usable content: 301 redirect, not followed. Cited only to record this. |
| S99 | https://news.xbox.com/en-us/2023/04/28/blind-driving-assists-forza-motorsport/ | Xbox Wire (Forza Motorsport) | Microsoft (Xbox Wire) | 2026-10-24 | Opened, no usable content: HTTP 404. |
| S100 | https://www.gearsofwar.com/games/gears-5 | Gears of War site: Gears 5 page, links to accessibility page | Gears of War site | 2026-10-24 | Used in B (A5). |
| S101 | https://news.xbox.com/de-de/2023/04/28/blind-driving-assists-forza-motorsport/ | Xbox Wire (German address): Forza Motorsport accessibility, summarised in English | Microsoft (Xbox Wire) | 2026-10-24 | Used in B (A4). |
| S102 | https://www.gearsofwar.com/en-us/games/gears-5/accessibility/ | Gears of War site: Gears 5 accessibility feature list | Gears of War site | 2026-10-24 | Used in B (A5). |
| S103 | https://www.playstation.com/en-us/games/ratchet-and-clank-rift-apart/accessibility/ | PlayStation.com: Ratchet & Clank accessibility page | Sony Interactive Entertainment | 2026-10-24 | Opened, no usable content: HTTP 404. Cited only to record this. |
| S104 | https://www.playstation.com/en-us/games/returnal/accessibility/ | PlayStation.com: Returnal accessibility page | Sony Interactive Entertainment | 2026-10-24 | Opened, no usable content: HTTP 404. Cited only to record this. |
| S105 | https://www.playstation.com/en-us/games/marvels-spider-man-2/accessibility/ | PlayStation.com: Spider-Man 2 accessibility page | Sony Interactive Entertainment | 2026-10-24 | Opened, no usable content: HTTP 404. Cited only to record this. |
| S106 | https://support.insomniac.games/hc/en-us/articles/46730041467027-What-Accessibility-options-does-Marvel-s-Spider-Man-2-feature | Insomniac support article | Insomniac Games | 2026-10-24 | Opened, no usable content: HTTP 403. Cited only to record this. |
| S107 | https://support.insomniac.games/hc/en-us/articles/46716208107795-What-Accessibility-options-does-Ratchet-Clank-Rift-Apart-feature | Insomniac support article | Insomniac Games | 2026-10-24 | Opened, no usable content: HTTP 403. Cited only to record this. |
| S108 | https://caniplaythat.com/2021/04/29/returnal-can-i-play-that-accessibility-review-ps5/ | Can I Play That? Returnal review | Can I Play That? | 2026-10-24 | Partly useful: no menu description; cited for that absence. Used in B (A13). |
| S109 | https://www.pushsquare.com/news/2023/09/marvels-spider-man-2-details-in-depth-difficulty-modifiers-and-impressive-accessibility-settings-on-ps5 | Push Square: Spider-Man 2 difficulty and accessibility settings | Push Square | 2026-10-24 | Partly useful: no menu structure; feature list only. Used in B (A6). |
| S110 | https://caniplaythat.com/2021/05/20/ratchet-and-clank-rift-apart-accessibility-features-revealed/ | Can I Play That? Ratchet & Clank features | Can I Play That? | 2026-10-24 | Partly useful: settings listed; no menu layout. Used in B (A7). |
| S111 | https://caniplaythat.com/2021/04/21/returnal-can-i-play-that-accessibility-preview/ | Can I Play That? Returnal accessibility preview | Can I Play That? | 2026-10-24 | Used in B (A13). |
| S112 | https://www.avclub.com/the-last-of-us-part-ii-accessibility-1 | A.V. Club: TLOU II accessibility article | The A.V. Club | 2026-10-24 | Partly useful: feature list; no menu description. Used in B (A1). |
| S113 | https://ggrecon.com/guides/god-of-war-ragnarok-accessibility-options | GGRecon: God of War Ragnarok accessibility options guide | GGRecon | 2026-10-24 | Partly useful: presets and options; no menu navigation. Used in B (A3). |
| S114 | https://onemoregame.ph/2022/11/god-of-war-ragnarok-all-accessibility-options/ | OneMoreGame.ph: God of War Ragnarok accessibility options | OneMoreGame.ph | 2026-10-24 | Used in B (A3), C.6. |
| S115 | https://egmnow.com/?p=23010 | EGM article | EGM | 2026-10-24 | Opened, no usable content: HTTP 404. |
| S116 | https://gamerguides.com/god-of-war-ragnarok/guide/gameplay/introduction/accessibility-options | Gamer Guides: God of War Ragnarok accessibility options | Gamer Guides | 2026-10-24 | Partly useful: preset contents; no navigation. Used in B (A3). |
| S117 | https://caniplaythat.com/?p=13902 | Can I Play That? review: Hellblade II accessibility | Can I Play That? | 2026-10-24 | Used in B (A10), C.5, Synthesis. |
| S118 | https://caniplaythat.com/?p=13147 | Can I Play That? review: Assassin's Creed Mirage accessibility | Can I Play That? | 2026-10-24 | Used in B (A9), Synthesis. |
| S119 | https://news.ubisoft.com/en-us/article/4gDuMRKW25Z4eOLVSZBrvx/assassins-creed-mirage-accessibility-spotlight | Ubisoft: Assassin's Creed Mirage accessibility spotlight | Ubisoft | 2026-10-24 | Used in B (A9). |
| S120 | https://caniplaythat.com/2021/08/11/hades-god-mode-explained-by-supergiant-games | Can I Play That?: Hades God Mode explained by Supergiant | Can I Play That? | 2026-10-24 | Used in B (A12). |
| S121 | https://gameaccessibilityguidelines.com/celeste-assist-mode | GAG case study: Celeste Assist Mode wording and lessons | Game Accessibility Guidelines project | 2026-10-24 | Used in B (A11). |
| S122 | https://inverse.com/gaming/hades-god-mode-interview | Inverse: Hades God Mode interview | Inverse | 2026-10-24 | Used in B (A12). |
| S123 | https://blogs.tuni.fi/playlab/features/too-steep-a-hill-to-die-on-celeste-assist-mode-and-accessibility/ | Tampere University blog on Celeste Assist Mode | Tampere University | 2026-10-24 | Opened, no usable content: does not describe the Assist Mode screen. Cited only to record this. |
| S124 | https://www.vice.com/en/article/d3w887/celeste-difficulty-assist-mode | Vice: Celeste difficulty and Assist Mode | Vice | 2026-10-24 | Used in B (A11). |
| S125 | https://accessible.games/accessible-player-experiences/access-patterns/ | AbleGamers APX access-pattern index (11 patterns, 3 described) | AbleGamers | 2026-10-24 | Used in A.4. |
| S126 | https://accessible.games/accessible-player-experiences/access-patterns/flexible-displays/ | AbleGamers APX pattern: Flexible Displays | AbleGamers | 2026-10-24 | Used in A.4. |
| S127 | https://accessible.games/accessible-player-experiences/access-patterns/leave-it-there/ | AbleGamers APX pattern: Leave It There | AbleGamers | 2026-10-24 | Used in A.4, C.7. |
| S128 | https://accessible.games/accessible-player-experiences/access-patterns/personal-interface/ | AbleGamers APX pattern: Personal Interface | AbleGamers | 2026-10-24 | Used in A.4, Synthesis. |
| S129 | https://accessible.games/accessible-player-experiences/access-patterns/improved-precision/ | AbleGamers APX pattern: Improved Precision | AbleGamers | 2026-10-24 | Used in A.4. |
| S130 | https://www.w3.org/WAI/ARIA/apg/patterns/switch/ | W3C ARIA Authoring Practices: switch pattern | W3C | 2026-10-24 | Used in A.3, Synthesis. |
| S131 | https://www.w3.org/WAI/ARIA/apg/patterns/slider/ | W3C ARIA Authoring Practices: slider pattern (keys) | W3C | 2026-10-24 | Used in C.2, Synthesis. |
| S132 | https://www.w3.org/WAI/ARIA/apg/patterns/radio/ | W3C ARIA Authoring Practices: radio group pattern | W3C | 2026-10-24 | Used in Synthesis. |
| S133 | https://www.w3.org/WAI/ARIA/apg/patterns/spinbutton/ | W3C ARIA Authoring Practices: spin button pattern | W3C | 2026-10-24 | Used in C.2, Synthesis. |
| S134 | https://www.w3.org/WAI/ARIA/apg/patterns/tabs/ | W3C ARIA Authoring Practices: tabs pattern | W3C | 2026-10-24 | Used in A.3, C.1, Synthesis. |
| S135 | https://learn.microsoft.com/en-us/windows/apps/design/input/gamepad-and-remote-interactions | Microsoft Learn: gamepad and remote interactions (focus, engagement, accelerators) | Microsoft | 2026-10-24 | Used in C.1, C.2, C.3, C.6, C.8, Synthesis. |
| S136 | https://www.w3.org/WAI/WCAG22/Understanding/timing-adjustable.html | WCAG 2.2 SC 2.2.1 Understanding page | W3C | 2026-10-24 | Used in C.7, Synthesis. |
| S137 | https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-uxml-element-Slider.html | Unity 6 UI Toolkit: Slider element (keys, page-size) | Unity Technologies | 2026-10-24 | Used in C.2, C.3, Synthesis. |
| S138 | https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-uxml-element-TabView.html | Unity 6 UI Toolkit: TabView element | Unity Technologies | 2026-10-24 | Partly useful: no keyboard or gamepad behaviour described. Used in C.1. |
| S139 | https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-Navigation-Events.html | Unity 6 UI Toolkit: navigation events | Unity Technologies | 2026-10-24 | Used in C.1. |
| S140 | https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/UISupport.html | Unity Input System: UI support (repeat delay and rate, navigation notes) | Unity Technologies | 2026-10-24 | Used in C.2, C.3, Synthesis. |
| S141 | https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/UGameUserSettings | Unreal Engine API: UGameUserSettings (Confirm and Revert video mode) | Epic Games | 2026-10-24 | Used in C.7, Synthesis. |
| S142 | https://gitlab.gnome.org/GNOME/mutter/-/issues/365 | GNOME Mutter issue 365: Keep changes / Revert settings dialog bug | GNOME Project | 2026-10-24 | Used in C.7, Synthesis. |
| S143 | https://docs.unity3d.com/Manual/class-QualitySettings.html | Unity manual: quality levels as presets | Unity Technologies | 2026-10-24 | Used in A.4, C.7. |
| S144 | https://www.gamedeveloper.com/design/create-better-game-settings-options-handy-checklist- | Game Developer article: settings and options checklist | Game Developer | 2026-10-24 | Partly useful: author and date not captured. Used in A.4, C.6, C.7, Synthesis. |
| S145 | https://issues.unrealengine.com/issue/UE-209110 | Unreal issue tracker UE-209110 | Epic Games | 2026-10-24 | Opened, no usable content: HTTP 403. |
| S146 | https://itch.io/post/1997887/view-in-topic | itch.io forum post on controller versus keyboard repeat in a slider menu | itch.io (community post) | 2026-10-24 | Used in C.2. |
| S147 | https://partner.steamgames.com/doc/features/steam_controller/activators | Steamworks: Steam Input activator types | Valve (Steamworks) | 2026-10-24 | Partly useful: about Steam Input binding; not relevant to menu design. Not cited. |
| S148 | https://gameaccessibilityguidelines.com/provide-a-choice-of-text-colour-low-high-contrast-choice-as-a-minimum/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G28. |
| S149 | https://gameaccessibilityguidelines.com/allow-difficulty-level-to-be-altered-during-gameplay-either-through-settings-or-adaptive-difficulty/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G31. |
| S150 | https://gameaccessibilityguidelines.com/use-simple-clear-language/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1, C.5, Synthesis. Guideline row G27. |
| S151 | https://gameaccessibilityguidelines.com/offer-a-wide-choice-of-difficulty-levels/ | GAG guideline page: title, level, category, summary, example games | Game Accessibility Guidelines project | 2026-10-24 | Used in A.1. Guideline row G30. |
| S152 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/playing-haptics.json | Apple HIG data feed, Playing haptics: abstract and best practices | Apple | 2026-10-24 | Used in A.3, C.4, Synthesis. |
| S153 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/buttons.json | Apple HIG data feed, Buttons: abstract and best practices | Apple | 2026-10-24 | Used in A.3. |
| S154 | https://www.youtube.com/watch?v=k1M44e4XV0k | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S155 | https://www.youtube.com/watch?v=PTHDC3q1Y6Q | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S156 | https://www.youtube.com/watch?v=2gQcoi2qNqw | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S157 | https://www.youtube.com/watch?v=uwov8u-E8r0 | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S158 | https://www.youtube.com/watch?v=3HLhVbCzwOY | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S159 | https://www.youtube.com/watch?v=LeGSqMo3c3U | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S160 | https://www.youtube.com/watch?v=sTn6e0eV9eE | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S161 | https://www.youtube.com/watch?v=tJRrUNQSqnU | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S162 | https://www.youtube.com/watch?v=tfo7_Cwzt_c | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S163 | https://www.youtube.com/watch?v=X5j5zMA3tRA | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S164 | https://www.youtube.com/watch?v=sBxvuq1iVNY | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S165 | https://www.youtube.com/watch?v=TyG0yE0Dl3Q | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S166 | https://www.youtube.com/watch?v=ManAQvvrIDw | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S167 | https://www.youtube.com/watch?v=AZWR7Yp30k4 | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S168 | https://www.youtube.com/watch?v=Wdgqe4Ja_WU | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S169 | https://www.youtube.com/watch?v=70_fZ3oyd_4 | YouTube video linked from a Microsoft XAG page | YouTube (uploader not captured) | 2026-10-24 | Page title only; video not watched. Used in B. |
| S170 | https://rossminor.com/2022/11/22/blind-accessibility-review-of-god-of-war-ragnarok-another-game-to-add-to-the-list/ | Ross Minor: blind accessibility review of God of War Ragnarok (first launch) | rossminor.com (author site) | 2026-10-24 | Used in B (A3), Synthesis. |
| S171 | https://www.familygamingdatabase.com/accessibility/God+of+War+Ragnark | Family Gaming Database: God of War Ragnarok onboarding flags | Family Gaming Database | 2026-10-24 | Used in B (A3). |
| S172 | https://www.familygamingdatabase.com/search/accessibility/190-Onboarding | Family Gaming Database: Onboarding flag definition and first games listed | Family Gaming Database | 2026-10-24 | Used in B (A1). |
| S173 | https://caniplaythat.com/?p=4242 | Can I Play That?: AC Valhalla announced accessibility features | Can I Play That? | 2026-10-24 | Used in B (A8). |
| S174 | https://caniplaythat.com/?p=4394 | Can I Play That?: AC Valhalla first-launch accessibility screen | Can I Play That? | 2026-10-24 | Used in B (A8), Synthesis. |
| S175 | https://www.sportskeeda.com/esports/alan-wake-2-accessibility-settings-explored | Sportskeeda: Alan Wake 2 accessibility settings | Sportskeeda | 2026-10-24 | Opened, no usable content: HTTP 405. Cited only to record this. |
| S176 | https://caniplaythat.com/?p=13295 | Can I Play That?: Alan Wake II horror flash option | Can I Play That? | 2026-10-24 | Used in A.5, B (A14), Synthesis. |
| S177 | https://www.nngroup.com/videos/checkboxes-vs-switches-forms/ | NN/g video page: checkboxes vs switches (short text only) | Nielsen Norman Group | 2026-10-24 | Used in A.3, Synthesis. |
| S178 | https://news.xbox.com/en-us/2023/04/27/forza-motorsport-accessibility-features-blind-driving/ | Xbox Wire (en-us): Forza Motorsport accessibility, announced features | Microsoft (Xbox Wire) | 2026-10-24 | Used in B (A4). |
| S179 | https://learn.microsoft.com/en-us/windows/apps/design/controls/slider | Microsoft Learn: slider guidance for Windows apps | Microsoft | 2026-10-24 | Used in A.3, C.2, C.3, C.4, Synthesis. |
| S180 | https://learn.microsoft.com/en-us/windows/apps/design/app-settings/guidelines-for-app-settings | Microsoft Learn: guidelines for app settings | Microsoft | 2026-10-24 | Used in A.3, A.4, C.5, C.7, Synthesis. |
| S181 | https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/radio-button | Microsoft Learn: radio button guidance | Microsoft | 2026-10-24 | Used in A.3, Synthesis. |
| S182 | https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/toggles | Microsoft Learn: toggle switch guidance | Microsoft | 2026-10-24 | Used in A.3, Synthesis. |
| S183 | https://developer.android.com/design/ui/large-screens/canonical-layouts | Android developers: desktop experiences overview | Google (Android) | 2026-10-24 | Opened, no usable content: not the list-detail page; nothing usable. |
| S184 | https://developer.apple.com/tutorials/data/design/human-interface-guidelines/split-views.json | Apple HIG data feed, Split views: abstract and best practices | Apple | 2026-10-24 | Used in A.4, C.5. |
| S185 | https://developer.android.com/develop/ui/compose/layouts/adaptive/list-detail | Android Compose: list-detail layout | Google (Android) | 2026-10-24 | Used in A.4, C.5, C.9, Synthesis. |
| S186 | https://developer.android.com/develop/ui/compose/layouts/adaptive/support-different-display-sizes | Android Compose: design for window size classes | Google (Android) | 2026-10-24 | Used in A.4, C.9. |
| S187 | https://developer.android.com/develop/ui/compose/layouts/adaptive/use-window-size-classes | Android Compose: window size class breakpoints | Google (Android) | 2026-10-24 | Used in A.4, C.9. |
| S188 | https://doc.qt.io/qt-6/qabstractslider.html | Qt documentation: QAbstractSlider | The Qt Company | 2026-10-24 | Opened, no usable content: fetch returned the weekly-limit message; no content. Cited only to record this. |

### Search results seen but not opened

These URLs appeared in search results only. They support no claim in this report. They are listed as places to look next.

| URL | Result title | Why it may be worth opening | Status |
|---|---|---|---|
| https://windowscentral.com/gaming/forza-motorsport-2023-is-taking-accessibility-seriously-with-blind-driving-assists | Forza Motorsport (2023) is taking accessibility seriously with Blind Driving Assists | Second report on the Forza menu features (case A4) | search result only, not opened |
| https://primagames.com/tips/marvels-spider-man-2-all-accessibility-options-listed | Marvel's Spider-Man 2 - All Accessibility Options, Listed | Menu list for Spider-Man 2 (case A6 is thin) | search result only, not opened |
| https://primagames.com/news/insomniac-shares-accessibility-options-ratchet-clank-ahead-release | Insomniac Shares Accessibility Options for Ratchet & Clank Ahead of Release | Menu list for Ratchet & Clank (case A7 is thin) | search result only, not opened |
| https://mp1st.com/news/full-list-of-the-last-of-us-part-i-accessibility-features-revealed | Full list of The Last of Us Part I accessibility features | Second list for case A2 | search result only, not opened |
| https://caniplaythat.com/?p=10837 | The Last of Us: Part II accessibility review (Can I Play That?) | Alternate address of the TLOU II review | search result only, not opened |
| https://caniplaythat.com/2020/12/08/assassins-creed-valhalla-accessibility-review/ | Assassin's Creed Valhalla accessibility review | Full accessibility review for case A8 | search result only, not opened |
| https://caniplaythat.com/?p=14400 | Alan Wake II update adds accessibility improvements | More on case A14 | search result only, not opened |
| https://game-lover.org/en/assassins-creed-mirage-our-accessibility-review | Assassin's Creed Mirage - Our Accessibility Review | Second review for case A9 | search result only, not opened |
| https://www.dexerto.com/gaming/how-to-make-hades-2-easier-god-mode-explained-2695050/ | How to make Hades 2 easier: God Mode explained | In-game wording of God Mode (case A12) | search result only, not opened |
| https://en.wikipedia.org/wiki/Celeste_(video_game) | Celeste (video game) | Assist Mode details (case A11) | search result only, not opened |
| https://www.nngroup.com/articles/state-switch-buttons/ | State-Switch Controls: The Case of the Mute Button (NN/g) | Toggle versus state-switch button wording | search result only, not opened |
| https://www.nngroup.com/articles/checkboxes-design-guidelines/ | Checkboxes: Design Guidelines (NN/g) | Checkbox rules for rows 1 and 2 | search result only, not opened |
| https://www.nngroup.com/articles/listbox-dropdown/ | Listboxes vs. Dropdown Lists (NN/g) | Row 6 component choice | search result only, not opened |
| https://www.nngroup.com/articles/steering-law/ | Accot-Zhai Steering Law: Implications for UI Design (NN/g) | Why thin slider tracks are hard to drag | search result only, not opened |
| https://backlothelp.netflix.com/hc/en-us/articles/360057622133-Photosensitive-Epilepsy-Guidelines-What-Can-I-Do-If-My-Content-Fails | Netflix: Photosensitive Epilepsy Guidelines, what to do if content fails | Industry practice on PSE testing (A.5) | search result only, not opened |
| https://www.epilepsysociety.org.uk/sites/default/files/2020-08/PhotosensitiveepilepsyNovember2016.pdf | Epilepsy Society photosensitive epilepsy PDF (2016 edition) | Older edition of the fact sheet (A.5) | search result only, not opened |
| https://forum.orkframework.com/discussion/6053/changing-menu-screens-with-l-and-r | Changing menu screens with L and R (forum) | Shoulder buttons for menu screens (C.1) | search result only, not opened |
| https://icarus.featureupvote.com/suggestions/213615/suggestion-q-and-e-to-quickly-change-tabs-in-tab-menu | Q and E to quickly change tabs in Tab Menu (feature request) | Q and E as tab keys (C.1, unverified) | search result only, not opened |
| https://github.com/MrEliptik/slider_gamepad | slider_gamepad (GitHub) | Gamepad slider behaviour (C.2) | search result only, not opened |
| https://answers.unrealengine.com/t/umg-gamepad-repeat-speed/393197 | UMG gamepad repeat speed (Unreal answers) | Hold-to-repeat rate on a gamepad (C.2) | search result only, not opened |
| https://docs.unrealengine.com/4.26/en-US/API/Runtime/Engine/GameFramework/UGameUserSettings/ | UGameUserSettings (UE 4.26 API) | Older copy of the confirm/revert API (C.7) | search result only, not opened |
| https://forums.unrealengine.com/t/ugameusersettings-revertvideomode-problem/114357 | UGameUserSettings::RevertVideoMode problem (forum) | How revert is used in practice (C.7) | search result only, not opened |
| https://support.microsoft.com/en-us/windows/hardware/display-graphics/change-your-screen-resolution-and-layout-in-windows | Change your screen resolution and layout in Windows | Keep-changes wording and timer length (C.7, unverified) | search result only, not opened |
| https://gitlab.gnome.org/GNOME/mutter/-/issues/2015 | Change monitor configuration without popup dialog (GNOME Mutter) | Argument against confirm dialogs (C.7) | search result only, not opened |
| https://partner.steamgames.com/doc/steamhardware/compat | Steam Deck compatibility (Steamworks) | Controller and text rules for Steam Deck Verified | search result only, not opened |
| https://design.gitlab.com/components/segmented-control | Segmented control (GitLab Pajamas design system) | Third-party segmented-control rules (row 3) | search result only, not opened |
| https://designsystem.michelin.com/components/segmented-buttons | Segmented buttons (Michelin design system) | Third-party segmented-button rules (row 3) | search result only, not opened |
| https://www.familygamingdatabase.com/search/accessibility/192-Low+Vision+Onboarding | Games with Low Vision Onboarding (Family Gaming Database) | Games whose first launch defaults to high-contrast, large text | search result only, not opened |
| https://docs.unity3d.com/Packages/com.unity.inputsystem@1.15//manual/UISupport.html | UI support (Unity Input System 1.15) | Newer copy of the UI module page (C.2, C.3) | search result only, not opened |
