# R2 · AAA game iconography: recognisable AND expressive (rules, guidelines, case studies)

Research for the FrontRooms iconography system (平面视觉). Started 2026-10-30.
Question: how do mainstream AAA games (and the platform holders behind them) design HUD icons, state icons,
interaction prompts, reticle states, button glyphs and map/waypoint icons so that they are instantly recognisable
yet carry personality, and which written standards / guidelines do studios and platforms use?
Special focus: state icons for MOVEMENT / STAMINA / NOISE / STEALTH (FrontRooms needs a RUN / GAIT / noise icon).

FrontRooms context: first-person horror in endless 1990 offices; minimal HUD; a key unlocks a stair EXIT; the hunter
(Relay) tracks the player by SOUND; three gaits (slow walk = silent, walk, run = loud / forward-only) plus stamina
dashes. Graphic language = 1990 building-services print (ISO 7010 / ANSI safety signs, evacuation plans, office
forms, CRT screens), Swiss-style sans for signs (TeX Gyre Heros Bold / Cn Bold), Bayon HUD caps, IBM Plex Mono.
Sibling file R3_swiss_type_icon.md covers the Swiss / pictogram tradition (Aicher, Ota, AIGA/DOT, ISO 7010
construction); this file covers games and platform/UX guidelines and only touches that tradition where needed.

Conventions in this file
- Every fact carries a source id [G##] that matches a row in R2_sources.tsv.
- "read" = I read the page (WebFetch); "snippet" = known only from a search-result snippet (treat as weaker).
- INFERENCE = my own reasoning, not stated by the source.
- Verbatim quotes are rare, very short (under 15 words) and in quotation marks; everything else is paraphrase.
- No images or PDFs were downloaded by me; reference images are listed as page URL + image URL only (section 6).

---------------------------------------------------------------------------------------------------------------
Contents (file order = research order): 1 Platform/industry guidelines -> 2 Case studies (2.0-2k) ->
5 Movement/stamina/noise/stealth state icons -> 3 Recognisability techniques -> 4 Personality/expressiveness ->
6 Reference images (25) -> 2l Other titles (brief) -> 7 DESIGN PRINCIPLES FOR FRONTROOMS (15) -> 8 Gaps.
Sources: R2_sources.tsv, ids G01-G74 (52 read, 22 search-snippet).

## 1. Platform and industry guidelines

### 1a. Xbox Accessibility Guidelines (XAG) — the only platform-holder game-UI rules that are fully public

XAG 101 Text display [G01] (Microsoft Learn, ms.date 2022-05-09, page updated 2026-06-17; read)
- Minimum DEFAULT text size, measured as body height (lowest descender to highest ascender, counting only pixels
  that have > 2:1 contrast with the background):
  console 26 px at 1080p / 52 px at 4K; PC & VR 18 px at 1080p / 36 px at 4K;
  mobile & cloud streaming 18 px at 100 DPI, 36 px at 200 DPI, 72 px at 400 DPI (scale linearly).
- ICONS AND GLYPHS: the text inside an icon or button glyph (the "B" inside the B-button) must itself meet the
  minimum text size, and icons/glyphs must scale with text up to 200 % of the default minimum. If the glyph then
  no longer fits the line, enlarge the surrounding text so the glyph sits inside the text line container.
  Their "don't" example: line text and button exterior at 18 px but the X inside the button only 13 px.
- Players must be able to scale text to 200 % without losing content; header text must stay distinct when scaled.
- Offer at least one sans-serif face; if a stylised face is used (their example: blood-dripping horror lettering)
  offer a non-stylised option. Sentence-case option for running lines; 1-2-word labels are exempt from the case rule.
- Shipped example: Immortals Fenyx Rising prompt = white Y-button glyph with black outline + "Collect" in white with
  black outline, readable on any background.

XAG 102 Contrast [G02] (ms.date 2023-06-08; read)
- Standard-size text AND visual elements that carry gameplay information: >= 4.5:1 against their background.
- Large-scale text/visual elements: >= 3:1 (large = console 52 px at 1080p / 104 px at 4K; PC 36 px / 72 px).
- Inactive (locked/disabled) element text: >= 3:1. High-contrast mode: every UI element >= 7:1.
- Over a non-solid background, measure against the LOWEST-contrast area behind the element.
- Ways to hold contrast over a moving 3D scene: optional solid/opaque plate behind text, colour options, a
  high-contrast mode, borders/outlines around text and elements.
- For Honor team symbols: black inner outline + white outer outline, so the same symbol reads on dark walls and
  on bright sky. Forza Horizon 4 map icons: solid yellow fill + black outline + black text.
- The stated purpose of a high-contrast mode is to separate important elements from each other and from
  unimportant ones, not merely to push light against dark.
- Battlefield 2042 lets players recolour HUD icons per colour-blindness type or freely, with a preview in shadowy,
  mid-tone and bright scenes. Logotypes and pure decoration are exempt from contrast rules.

XAG 103 Additional channels for visual and audio cues [G03] (ms.date 2022-05-09; read)
- Colour alone must never carry information; anything critical shown by colour needs at least one more signifier:
  shape, pattern, iconography or a text label (Grounded: red + warning triangle + "Missing ingredients" text).
- Symbol + text label lowers cognitive load; symbols help players who read slowly, labels remove guesswork.
- Any audio content critical to play (their example: gunfire) must also be shown visually, as a graphic that
  indicates the TYPE and the DIRECTION of the sound; haptics only as an extra channel, never the only one.
- Unavailable controls: greying out must be paired with another cue (a lock symbol, a "Not available" label).
- Shipped examples: Grounded red directional damage halo; Gears 5 "Fabricator Ping" (audio ping rises in volume and
  rate with proximity); Assassin's Creed Valhalla Odin's Sight (gold ring + spatial audio on the same item);
  Call of Duty Black Ops Cold War lets players recolour the map triangles for self/allies/enemies/party.
- Colour-blind simulation filters are a dev check only, not a substitute for testing with colour-blind players.
- INFERENCE for FrontRooms: the hunter tracks SOUND, so the player's own loudness is "audio content critical to
  gameplay". XAG 103 therefore makes a visual noise/gait state icon an accessibility requirement for deaf and
  hard-of-hearing players, not only a style choice; it should show type (gait) and level (how loud).
- INFERENCE (size): FrontRooms ships on Mac/Win (PC rule: 18 px body height at 1080p) and iPhone/iPad (DPI rule:
  18 px per 100 DPI, i.e. about 83 px on a ~460 ppi iPhone and about 48 px on a ~264 ppi iPad). A state icon that
  contains letters or numerals (e.g. "RUN", a stamina count) inherits these minimums for the characters inside it.

### 1b. Apple HIG — SF Symbols and interface icons (the clearest "icons are type" system)

SF Symbols [G04] (Apple HIG, read via the HIG JSON feed, 2026)
- 9 weights (ultralight to black), each matching a weight of the San Francisco system font, and 3 scales
  (small / medium = default / large) defined relative to the font's CAP HEIGHT: 27 weight-scale combinations per
  symbol. Symbols align with text automatically in every weight and size. Changing scale changes emphasis WITHOUT
  breaking the weight match with text set at the same point size.
- Scale reference (from the figure descriptions): small = the circle of plus.circle touches cap line and baseline;
  medium = it slightly overshoots both; large = the plus's vertical arm almost touches both lines.
- Variants: OUTLINE is the default and is used wherever a symbol sits beside text (toolbars, lists); FILL adds
  solid areas for more visual emphasis and is used to show SELECTION (tab bars, swipe actions); SLASH shows that an
  item or action is unavailable; ENCLOSED (circle/square/rectangle) improves legibility at small sizes.
- Rendering: up to 3 layers (primary/secondary/tertiary); monochrome, hierarchical (one colour, opacity by layer),
  palette, multicolor (intrinsic meaning colours only, e.g. red for trash.slash).
- VARIABLE COLOR: layers switch on as a value crosses thresholds between 0 and 100 % (capacity, strength);
  canonical example speaker.wave.3, where each wave layer is a sound level and the speaker body opts out.
  Rule: use variable colour to communicate CHANGE, not depth.
- Motion vocabulary with assigned meanings: Bounce = an action happened or must happen; Scale = persistent
  emphasis/selection; Pulse (opacity only) = ongoing activity until a condition is met; Breathe (opacity + size) =
  ongoing status, like recording; Variable color = progress/activity (cumulative or iterative); Replace down-up =
  state change, up-up = forward progression, off-up = next available state; Magic Replace = morph between related
  shapes (slash drawing on/off); Wiggle = call to action or reinforce a direction; Rotate = task in progress or
  real-world behaviour (fan); Draw on/off (SF Symbols 7) = progress or direction along a path.
- Apply animation judiciously; each one must have one clear purpose; match motion to the product's tone.
- Custom symbols: start from the template of the most similar symbol; it governs level of detail, optical weight,
  alignment, position and perspective; draw whole shapes and use an erase layer for gaps; test every animation
  preset because shapes read differently in motion; always supply an alternative text label.

Interface icons [G05] (Apple HIG "Icons", read via JSON feed)
- Highly simplified, recognisable, familiar metaphor tied directly to the action/content.
- One size, level of detail, STROKE WEIGHT and PERSPECTIVE across the whole set; lighter shapes may be drawn
  taller to look balanced.
- Generally MATCH ICON WEIGHT TO ADJACENT TEXT; break the match only to emphasise one of them on purpose.
- Optical centring: asymmetric glyphs (e.g. download arrow, heavy at the bottom) are nudged until they look
  centred; bake that offset into the asset padding, then centre the asset geometrically.
- Text inside icons only when essential; gender-neutral figures; avoid culture-bound images; vector assets.

### 1c. Google Material icons / Material Symbols (the most explicit published construction grid)

Material Design 1 system icons [G06] (Google, 2014-2016 spec, read; superseded but still the clearest numbers)
- 24 dp icon; live area 20 x 20 dp; 2 dp padding inside the 24 dp trim (dense: 20 dp icon, 16 dp live area).
  Content may enter the padding only for extra visual weight; nothing crosses the trim.
- Keyline shapes: square 18 x 18; circle 20 dp diameter; vertical rectangle 16 w x 20 h; horizontal rectangle
  20 w x 16 h. (These give round, square and tall/wide glyphs the same optical size.)
- Stroke 2 dp everywhere (curves, angles, interior and exterior); 1.5 dp allowed for complex or small glyphs.
  Stroke-to-icon ratio = 2/24 = 0.083 (2/20 = 0.10 of the live area). Terminals SQUARE, not rounded.
- Corners: exterior silhouette corners 2 dp radius; strokes of 2 dp or less are not rounded; interior corners square.
- Front-facing, geometric, symmetrical, bold; no tilt, no perspective, no dimensional effects; no organic or
  gestural forms; whole-pixel positions. Human figures: same stroke, square limb ends, never crop limbs.
- State by opacity (black on light / white on dark): active-focused 87 % / 100 %, active-unfocused 54 % / 70 %,
  inactive 38 % / 50 %. Touch target 48 dp around a 24 dp icon (40 dp around 20 dp dense).

Material Symbols variable font [G07] (Google Fonts developer docs, read)
- Axes: FILL 0..1 (meant for state transitions/interaction), weight 100..700 (default 400), GRADE -50..200
  (fine thickness change with almost no size change), optical size 20..48 (auto-adjusts stroke as size changes).
- Grade can be matched to the text face's grade; LOW grade (e.g. -25) reduces glare of light symbols on dark
  backgrounds; HIGH grade (e.g. 200) emphasises a symbol.
- The same glyph set ships in three styles (Outlined / Rounded / Sharp): identical skeletons, different corner
  and terminal treatment. INFERENCE: this is the industry's cleanest proof that personality can live in corners
  and terminals while the recognisable skeleton stays fixed.

### 1d. Game Accessibility Guidelines (gameaccessibilityguidelines.com) [G08] (read: full list + 2 guideline pages)
- Basic (vision): "Provide high contrast between text/UI and background"; "Ensure no essential information is
  conveyed by a fixed colour alone"; "Ensure interactive elements / virtual controls are large and well spaced,
  particularly on small or touch screens" (basic, motor + vision).
- Intermediate (vision): "Avoid placing essential temporary information outside the player's eye-line"
  (persistent HUD can live at the edges because players learn its place; TEMPORARY alerts must appear where the
  player is looking, because peripheral-vision loss is common; God of War 3 QTE prompts cited as a poor example,
  Tony Hawk's HD as good); "Provide a choice of cursor / crosshair colours / designs"; "Allow interfaces to be resized".
- Basic (hearing): "Ensure no essential information is conveyed by sounds alone" — test by having a first-time
  player play muted; anything they miss must be given visually (icon, effect, text). Fortnite's cues cited as best
  practice. Intermediate (hearing): replicate important supplementary audio such as the direction of shots in
  visuals; give visuals for significant background sounds.
- Cognitive: "Give a clear indication that interactive elements are interactive"; advanced option to hide all
  non-interactive elements; "Avoid flickering images and repetitive patterns".
- INFERENCE for FrontRooms: a persistent gait/noise icon may sit at a fixed edge/corner, but a TEMPORARY noise
  event (a door slam, a run burst the hunter heard) must flash near the reticle/eye-line, not only in the corner.

### 1e. Symbol comprehension standards (ISO 7001 / ISO 9186 / ISO 3864 / ANSI Z535.3)

- ISO 9186-1:2014 [G09] (ISO catalogue page, read) specifies a method for testing how well a graphical symbol, or
  a variant of it, conveys its intended message WITHOUT supplementary text; confirmed in 2019, a revision is in
  progress. Part 2 tests perceptual quality, Part 3 tests association with referents [G09 search snippet].
- ISO 7001 public information symbols [G10] (Wikipedia, read): editions 1980, 1990 (+1993 amendment), 2007, 2023;
  177 symbols in 7 categories; maintained by ISO/TC 145/SC 1; symbols passed "extensive testing in several
  countries and different cultures"; design and testing governed by ISO 22727:2007. No sizes or colours are
  prescribed, only clear contrast; ISO 3864 safety colours are discouraged so information symbols are not mistaken
  for safety signs (green+white is reserved for ISO 7010 escape routes); prohibition = red slash over an object or
  red cross over a behaviour.
- Acceptance thresholds: ISO 3864 (safety signs, tested with ISO 9186) uses 67 % correct comprehension; ANSI
  Z535.3-2022 uses 85 % [G11] (Gungor 2023, Heliyon, read). In that study of 22 ISO 7010 signs shown at 2.3 cm,
  only 13/22 met 67 % and 5/22 met 85 % (mean 66.6 %): even standard signs often fail.
- ANSI Z535.3 also caps CRITICAL CONFUSIONS (reading the opposite meaning, e.g. "No fires" read as "Fires allowed")
  at under 5 %; test = show the symbol to representative people one at a time and ask what it means (open answer);
  the common lightning-bolt electrical symbol fails the 85 % test [G12] (IAEI Magazine 2008, read).
- The often-quoted "66 %" figure for ISO 7001 public information symbols could NOT be confirmed in a readable
  source (standards are paywalled); treat "about two-thirds" as the ISO-family bar and 85 % as the ANSI bar.
- INFERENCE for FrontRooms: run a tiny ISO 9186-style open-answer test on the gait/noise icon (show it alone, ask
  "what does this mean?"), count correct answers and CRITICAL CONFUSIONS separately. For a run/noise icon, the
  critical confusion is "I am safe / quiet" when the player is loud (or "exit / escape" from a running-man that
  echoes the ISO 7010 exit pictogram). Target >= 67 % correct and < 5 % critical confusions.

### 1f. PlayStation (Sony) — guidance exists but is not public [G13] (SIE GAAD 2024 post, read)
- PlayStation's developer accessibility guidelines are shared with partners; the public post does not describe
  their content. Public surface = Accessibility Tags on the PS Store in 6 categories (Visual, Audio, Subtitle &
  Caption, Control, Gameplay, Online Communication), 300+ PS5 games tagged by May 2024; a PlayStation Studios UXR
  team dedicated to accessibility; The Last of Us Part II framed as the "suggested playbook" (see 2a).
- INFERENCE: for public, citable console rules, XAG (1a) is the reference; Sony's practice is best read through
  its first-party games (TLOU2, below).

---------------------------------------------------------------------------------------------------------------
## 2. Case studies (what shipped AAA games actually do, and the rules their designers stated)

### 2.0 Vocabulary: diegetic / non-diegetic / spatial / meta
- Fagerholt & Lorentzon, "Beyond the HUD" (Chalmers MSc thesis with EA DICE, 2009) [G14] (record page, read): mapped
  the FPS UI design space and proposed four guidelines: know your design space, know your game (competitive focus
  and fiction change what the UI may be), establish player agency, strengthen the player-avatar perceptual link.
- Marcus Andrews, "Game UI Discoveries: What Players Want" (Gamasutra 2010) [G15] (read) gives the four types:
  DIEGETIC (in the world, seen by the character: Dead Space holograms), NON-DIEGETIC (classic HUD, only the player
  sees it), SPATIAL (placed in 3D space but not part of the fiction: Left 4 Dead teammate outlines), META (fiction-
  linked effects not placed in space: blood on the screen). His rule: whatever you move into the world must still
  deliver the same function ("functionality preservation and translation"). Fully diegetic FPS UI is close to
  unworkable; most games mix. Dead Space's diegetic 3D map failed for navigation, so the floor-projected "locator"
  line was added; Far Cry 2 used a held paper map, GPS and wristwatch.
- INFERENCE for FrontRooms: the 1990 building-services language is a META/DIEGETIC skin (printed placard, form,
  CRT) for information that must still read like a non-diegetic HUD. Function first, skin second.

### 2a. The Last of Us Part II (Naughty Dog, 2020) — Sony's accessibility "playbook" [G16]
Source: Matthew Gallant (Lead Systems Designer), PlayStation Blog, 2020-06-09 (read).
- 60+ accessibility settings. HUD Scale (Default / Large); HUD Background (Default / Light / Darkened); HUD Color
  (White, Yellow, Blue, Red, Green for text and HUD); HUD Colorblind Mode (Protanopia / Deuteranopia / Tritanopia
  palettes for HUD accents); HUD Flashing on/off.
- High Contrast Display (3 presets, touchpad swipe): MUTES the environment colours and adds distinct contrast colours
  to allies, enemies, items and interactive objects (the post does not list the hues).
- Enhanced Listen Mode: scan for items (assigned the CIRCLE shape) or enemies (assigned the SQUARE shape); each scan
  plays a 3D audio cue whose pitch encodes height; range 10-30 m, scan time 1-5 s.
- AWARENESS INDICATOR: a HUD element that warns when enemies are about to spot you AND shows their direction; an
  "Always" option keeps it on in combat, pointing at high-threat enemies. Damage indicators = HUD spikes toward the
  damage source. Persistent centre dot (hidden in cinematics and while aiming). Status shown by full-screen effects.
- Traversal and combat AUDIO cues for gaps, ledges, crouch/prone areas, pickups, breakable glass, grabs, melee.
- Design reading: every visual state has a sound or haptic twin, every colour coding has a shape twin (circle =
  item, square = enemy), and the HUD itself can be scaled, recoloured and backed with a darkened plate.

### 2b. Dead Space (Visceral, 2008; remake Motive 2023) — the diegetic reference [G17][G18][G19]
- Dino Ignacio (lead UI designer), GDC 2013 "Crafting Destruction: The Evolution of the Dead Space User
  Interface" [G17] (GDC Vault abstract + Game Developer recap, read): goal was to remove the player's "wall of
  safety"; health bar, inventory, weapon selector and locator were all moved into the world on the RIG suit.
- Wikipedia (Dead Space 2008 [G18], Isaac Clarke [G19], read): all displays are in-world holographic projections;
  health and stasis on the back of the suit; the health bar is a glowing spine bar that depletes from green to
  red; ammo appears only when the weapon is raised; menus, map, store and video calls are holograms; the game
  keeps running while menus are open. The spine idea came from HUD data on the back of a scuba-diving suit.
  Ignacio: anything that did not belong to Isaac was placed behind him.
- Design reading: a STATE (health) can be a part of the character's equipment, read in peripheral vision from the
  third-person camera; contextual elements (ammo) appear only when relevant.
- INFERENCE: FrontRooms is first-person, so the body-mounted trick needs an object the player sees (a held
  device, the hand, a wrist badge) or a printed artefact; otherwise it is a HUD skinned as an artefact.

### 2c. Alien: Isolation (Creative Assembly, 2014) — lo-fi 1979 tech as UI [G20][G21]
- MCV report from a Develop "Audience With" (2014-02-13) [G20] (read): no traditional HUD, permanent on-screen info
  kept to a minimum; every UI element must stay "in world" and share the worn VHS look; the health bar resembles a
  camcorder battery indicator; menus and maps flicker like worn tape. Lead UI artist Jon McKellan: the motion
  tracker is a deliberately clunky box that feels broken and has to be aimed; tools are lo-fi in gameplay as well
  as looks, and the tracker is an ACTIVE element the player must choose to use, not a passive readout.
- The tracker shows only proximity, not exact position; a still Alien does not appear on it; using it pulls your
  attention (focus shifts to the device, background blurs). Lead designer Clive Lindop: the tracker gives no
  edge, technology will not save you. Hacking became "signal tuning" because hacking did not exist in the 1970s.
- Station screens were filmed off real VHS/Betamax and an old portable TV; a 1970s style guide allowed only
  objects made before 1979 as kit-bash sources.
- Music follows what the player SEES, not the Alien's distance, so the score can never be used as an Alien detector.
- PlayStation Blog first look (2014) [G21] (search snippet): the DualShock 4 light bar flashes in sync with the
  tracker ping, faster as the Alien approaches.
- Design reading for FrontRooms (very close match: era-locked tech, a hunter that reacts to the player):
  (1) era lock applies to UI metaphors too (no "hacking", no hologram); (2) an indicator can be honest but
  INCOMPLETE on purpose (proximity, not position); (3) the HUD's own feedback must not become an exploit
  (music never reveals the Alien).

### 2d. Ghost of Tsushima (Sucker Punch, 2020) — guidance moved into the world [G22][G23]
- Nate Fox (game director), Press Start interview 2020-05-22 [G22] (read): the Guiding Wind replaces on-screen
  waypoints so the screen is not cluttered with UI pointing the way; it cues direction without dominating the view;
  the aim is to keep the player's eyes on the island; birds and foxes also lead the player.
- GDC 2021, Bill Rockenbeck, "Blowing from the West: Simulating Wind in Ghost of Tsushima" [G23] (GDC news page,
  read): the wind is a gust simulation flowing toward the destination, moving cloth, grass and hundreds of
  thousands of particles. A waypoint is still set on the map screen; only the in-world cue changes.
- Accessibility caveat (Can I Play That review, search snippet) [G74]: the white wind streaks can lose contrast
  against terrain. Moving information into the world must still meet contrast (see XAG 102).

### 2e. Control (Remedy, 2019) — bureaucratic modernist typography as world-building [G24][G25]
- Kotaku, Gita Jackson, 2019-08-29 [G24] (read): bold sans area-name titles cross the screen on entering a zone and
  fade; Maintenance signage reads like highway signage, clear, bold, impossible to ignore; the clean UI type itself
  carries the Bureau's authority. Argument: banal government/corporate typography applied to supernatural content
  "defamiliarizes the banal" and is what makes the world creepy.
- Game Informer, Elise Favis, 2019-03-27 [G25] (read): sharp brutalist bureaucratic architecture vs. the fluid
  Hiss; Hiss-possessed employees are marked by an intense RED, the game's one signal colour.
- No first-party Remedy statement on HUD/icon construction was found (Control's UI was built in Coherent Gameface,
  per Coherent Labs). INFERENCE: Control is the closest AAA precedent for FrontRooms' approach (official,
  institutional typography as the source of dread), and its restraint (one signal colour, type doing the work)
  is the transferable rule.

### 2f. Mirror's Edge (DICE, 2008) and Catalyst (2016) — colour as guidance instead of icons [G26][G27]
- Owen O'Brien (senior producer), Gamasutra interview 2008-06-06 [G26] (read): the game works without a HUD because
  the world supplies the information; runner vision lets players read the world as quickly as a runner does;
  first prototypes used static red marker posts, which felt contrived, so it became a dynamic system that tints
  real objects (pipes, poles), tunable by skill level, fade-in speed and fade-in distance so it never becomes a
  breadcrumb trail; no health meter because damage is obvious and health regenerates.
- Catalyst (EA News, engineer Mark Scheurwater) [G27] (read): Runner's Vision has 3 settings (Off / Classic /
  Full View) plus a red trail toward a map waypoint; routing uses A* plus playtest heat-maps; success = players keep
  their speed. Level designer Elisabetta Silli (Engadget GDC 2010 report, search snippet) noted red is an odd
  choice because it means danger or stop; reviewers noted red-saturated rooms can hide the cue.
- Design reading: a single reserved signal colour on a desaturated world works as an icon system; but the colour
  must be reserved (never used for decoration) and backed by an option and a non-colour cue.

### 2g. Amnesia: The Dark Descent (Frictional, 2010) — hide the number, show the feeling [G28]
Thomas Grip (creative director), "Game Design Deep Dive: Amnesia's Sanity Meter", Gamasutra 2014-08-27 (read).
- Early builds printed health and sanity as NUMBERS plus pickup messages; it felt too "gamey" and invited tactical
  planning, while horror works when players react in panic.
- Shipped design: sanity appears only in the inventory as a slowly rotting brain image + one line of text (Grip
  compares it to early Resident Evil / Silent Hill status screens). In play, state is felt through screen pulses
  and a sound (eggshells cracked near a microphone) that worsens the longer sanity stays low; sudden "insanity
  drops" pair a pulse with a specific sound. Effects were toned down so players would not feel physically ill.
- Withholding exact values creates an information gap that makes players feel unsafe; vague systems let players
  invent theories, which increased fear. Risk: an opaque system can feel unfair if it is badly balanced.
- Grip's lesson: making something game-like can destroy the intended feeling.
- Design reading for FrontRooms: a horror state icon should be QUALITATIVE (few discrete steps, no numbers) and
  paired with an audio/visual "feel" channel; but FrontRooms' noise rule is a core survival rule, so (unlike
  Amnesia's sanity) the player must be able to read it reliably: a few honest steps, not a hidden variable.

### 2h. Team Fortress 2 (Valve) and League of Legends (Riot) — the silhouette and hierarchy rules [G29][G30][G31]
- Valve, Gamefest 2008 (Moby Francke, Randy Lundeen), reported by Gamasutra 2008-07-29 [G29] (read): read order is
  TEAM COLOUR, then CLASS SILHOUETTE, then fine DETAIL; silhouettes made distinct through shoes, hats, clothing
  folds and checked in a three-quarter pose; detail concentrated on the chest; rim light separates figure from
  ground; the shape read must hold on black-and-grey models (a 1-bit / greyscale test); playtest confusion (medics
  hard to tell apart; L4D survivors vs zombies) is treated as a design failure and redesigned.
- Mitchell, Francke & Eng, "Illustrative Rendering in Team Fortress 2" (NPAR 2007) [G30] (search snippets of the
  paper/slides): class differentiation is central to play, so the nine silhouettes were designed to be distinct;
  silhouette is solved first, interior shapes after.
- Riot, "Clarity in League" (dev post, 2021-03-12) [G31] (read): clarity = understanding what is happening AND
  responding; keep a hierarchy so the most important moment draws the most attention; minimise noise. Silhouette is
  the most important recognition factor ("how it looks in darkness"); each champion has one primary feature that
  never changes across skins; silhouette should show the source of power and facing direction; ability visuals
  scale with importance (damage, crowd control, dodgeability, impact); reuse established shape language (e.g.
  downward-scrolling lines always mean spell shield); a skin may never be less clear than the base ("clarity
  parity"); transformations only during empowered states.
- Design reading: these are character rules, but they transfer directly to an icon family: one invariant primary
  feature per icon, a shape vocabulary reused for one meaning only, and a hierarchy where the most dangerous state
  is the loudest graphic. "Clarity parity" is the rule for any themed/skinned variant of an icon.

### 2i. Destiny (Bungie, 2014) [G32]
- David Candland (Bungie UI design lead), GDC 2016 "Tenacious Design and The Interface of 'Destiny'" (GDC Vault
  abstract, read; Kotaku 2016-06-20 and Game Developer recap, read): the brief was a UI approachable for newcomers
  yet fast and deep for veterans; topics include the free cursor (how "sticky" each element is when the cursor
  passes over it), time-gated (hold) interactions, localisation valves, visual direction and the ICON CREATION
  PIPELINE. German coverage (Mein-MMO, snippet) reports a large effort spent designing a symbol for every equipment
  piece. No public written icon rules were found; the talk video is the primary source.
- INFERENCE: Destiny's lasting lesson for small HUDs is the HOLD interaction (a ring that fills while a button is
  held): a timed action rendered as a filling shape around the prompt glyph. FrontRooms' key/EXIT door hold could
  reuse the same "fill around the glyph" grammar.

### 2j. Resident Evil 2 (2019), Resident Evil 7 (2017) — no HUD, state in the status screen [G33][G34]
- RE2 (2019) Wikipedia [G33] (read): health is a heart-rate monitor in the inventory screen, not on the HUD; the
  crosshair TIGHTENS while standing still aiming (a reticle state that communicates accuracy and power).
- RE7 (snippets) [G34]: no on-screen HUD; health on a wrist-worn display; producer Masachika Kawata tied first
  person to the commitment to horror. The series' ECG status screen (Fine / Caution / Danger) descends from the 1996
  original (INFERENCE from series knowledge; not confirmed in a read source this session).
- Design reading: horror franchises put precise state behind a deliberate act (open the inventory, look at the
  wrist) and keep only the reticle on screen; the reticle itself changes shape to show a state.

### 2k. Half-Life: Alyx (Valve, 2020) and Firewatch (Campo Santo, 2016) — information on held objects [G35][G36]
- Half-Life: Alyx (VentureBeat/UploadVR AMA coverage, search snippets) [G35]: health and resin/ammo counts are shown on
  the gloves and weapons; Valve said the inventory is designed to keep focus on the environment (over-the-shoulder
  ammo grab, quick weapon select). No Valve statement using "no HUD" was found.
- Firewatch (reviews and Campo Santo's printable-map notes, search snippets) [G36]: no HUD; a paper map Henry
  annotates and a magnetic compass; the "You Are Here" dot is an option ("Show Location On Map") that Campo Santo
  suggests disabling for the printable co-op challenge.
- Design reading: a diegetic object can carry a HUD's information only if the player chooses to look at it; the
  "you are here" option shows even map-as-object games keep a non-diegetic fallback switch.

---------------------------------------------------------------------------------------------------------------
## 5. State icons for MOVEMENT / STAMINA / NOISE / STEALTH (written before 3 and 4 because it is the core question)

### 5a. Visibility (light) meters
- Thief: The Dark Project (Looking Glass, 1998) [G37] (Wikipedia, read): a HUD meter shaped like a GEM; brighter gem =
  easier to see; footsteps are quiet on carpet and grass, loud on metal and ceramic tile; moss arrows lay a muffling
  carpet; a "room database" propagates sound around corners, and sound both tells the player where NPCs are and
  tells NPCs where the player is. Randy Smith: the safe boundary is usually the edge between light and shadow, and
  it keeps moving, so players must keep reassessing. AI (Tom Leonard) has a SUSPICION state between calm and alert.
- Splinter Cell (Ubisoft, 2002) [G38] (Wikipedia, read): a light meter that still works when night/thermal vision
  makes ambient light impossible to judge by eye.
- Skyrim (Bethesda, 2011) [G40] (UESP, read): while sneaking the RETICLE itself becomes the stealth icon: a flat
  line = hidden; slightly open eye = someone suspects; half-open eye = someone is searching; fully open eye = seen.
  "DETECTED" flashes under the reticle when an enemy moves to attack; "HIDDEN" when all have lost you. Noise rules:
  running is louder than walking, moving very slowly makes little sound, heavier armour is louder, Muffle reduces
  footstep noise; light also matters.
- Metro 2033 (4A, 2010) [G39] (Wikipedia, read): minimal HUD by stated intent (fewer distractions); gas-mask filter
  time is read by looking at Artyom's wristwatch; objectives by physically opening the journal; ammo by inspecting
  the weapon; the Ranger mode removes the HUD. (The watch's visibility LED is widely described by players but was not
  confirmed in a read source this session.)
- Design reading: Skyrim is the closest analogue to FrontRooms' three gaits (slow / walk / run = quiet / normal /
  loud) and it puts the stealth state ON THE RETICLE, where the eye already is (cf. GAG eye-line rule, 1d).

### 5b. Noise meters and sound visualisation
- Splinter Cell: Chaos Theory (Ubisoft Montreal, 2005) [G41][G42]: an "aural monitor" measures Sam's noise AND the
  ambient noise of the environment; staying below the ambient level keeps him unheard (Wikipedia, read). A white
  mark on the meter shows the ambient level; machinery or storms raise it and mask movement (GameSpot first look and
  Giant Bomb concept page, search snippets). Noise also raises guards' stress across the level.
  -> The single most relevant precedent for FrontRooms: a noise meter that is RELATIVE to the room, not absolute.
- The Legend of Zelda: Breath of the Wild (Nintendo, 2017) [G43] (guides/TV Tropes/Zelda Wiki, search snippets): a
  permanent noise meter in the bottom-right corner drawn as SOUND WAVES that are small and condensed when quiet and
  spread out when loud; noise-dampening gear and food reduce it (Stealth Up -20 / -40 / -70 %); the "Pro HUD" option
  removes it. Metaphor = the universal speaker/wave glyph (cf. SF Symbols speaker.wave.3 variable colour, 1b).
- Mark of the Ninja (Klei, 2012) [G44][G45]: every noise an enemy can hear is drawn in the world as an expanding
  ring whose edge is the hearing radius; enemies inside the ring hear it, outside do not. Nels Anderson (lead
  designer, Game Informer "Afterwords" 2012-10-15, read): the first version was a faint hint, then one effect of the
  same size for every noise, and playtesters still could not understand why guards reacted; only when the ring was
  tied to the real sound radius did players immediately understand and plan with it. Gamasutra (Leigh Alexander,
  2012-08-02, read): goal was to surface the genre's hidden rules so players plan instead of learning by failure;
  the feedback had to stay unobtrusive.
  -> Lesson: a noise graphic must be HONEST (its size/level must map to the real rule) or players cannot use it.
- Dead by Daylight (Behaviour, 2016) [G46] (DbD wiki.gg, read): the hunter (killer) sees a LOUD NOISE NOTIFICATION:
  an explosion-like dark bubble with red/orange accents plus a bang sound, at the noise's position; off-screen
  noises get red arrows; some variants swap the burst for a 2D picture of the source (generator, crow, survivor
  silhouette). Triggers include a survivor SPRINTING into a locker, pallet or window (48 m range) and failed skill
  checks (64 m); 1 s buffer between repeats.
  -> INFERENCE: DbD's notification is exactly FrontRooms' hunter rule seen from the other side; FrontRooms can show
  the player "the building heard that" with a burst/event mark at the moment of a loud action.
- Sniper Elite series (Rebellion, 2012-2025) [G73] (trophy guides, Edge review, Can I Play That, player guides;
  search snippets): SOUND MASKING: when a loud ambient source (generator, aircraft, speakers) is active, a sound
  indicator appears (top-right in V2; a white waveform at the top of the screen in Resistance) telling the player a
  shot will be masked; a deaf-gamer review of Sniper Elite 4 notes a text cue when enemies HAVE heard the gunshot.
  Edge (2014) judged the masking system poorly implemented. -> Ambient masking can be drawn as a temporary
  "cover" sign, separate from the player's own loudness.
- Hunt: Showdown (Crytek, 2019) [G47] (PlayStation Blog, 2020-02, Füsslin and Schwarz, read): sound pillars =
  feedback (what was it, where, is it dangerous), immersion, emotion; world "sound traps" (crows taking off, dogs,
  glass crunch, startled horses) betray players; a too-quiet match feels suspicious. No HUD noise meter is discussed:
  the WORLD is the noise display.
- Alien: Isolation [G20]: no noise meter; the Alien's states are told by sound (scream = attack; distinct sounds for
  spotted / searching / lost). Players report the Alien hears the tracker's beep at close range [G48] (Steam/Reddit
  threads, search snippets; community claim, not developer-confirmed).
- The Last of Us Part II [G16]: an AWARENESS INDICATOR warns when enemies are about to spot you and shows direction.

### 5c. Detection / alert state of the enemy (how the hunter's state is drawn)
- Metal Gear Solid (Konami, 1998) [G49][G50]: a large red "!" pops over a guard on discovery (MSX Metal Gear used a
  small white bubble; the MSX games used "!!" for reinforcements, per fan wikis, snippet). Kojima (Shmuplations
  interview via GoNintendo 2024-12-18, read): seeing the "!" pop up in a working build is what convinced Konami
  executives the sneaking game would work. MGS1 (Wikipedia, read): Alert mode -> Evasion mode (a counter runs down
  while you stay hidden) -> Infiltration; the Soliton radar (white player cone, red guard cones) is unusable during
  Alert/Evasion: the HUD itself degrades as the state worsens.
- Far Cry 3/4 [G51] (guides, snippets): a white ARROW over the enemy's head fills as he sees you, turns red, flashes
  when full, then he attacks; it drains if you break line of sight. Far Cry 6 (Ubisoft News 2022-03-28, GDC 2022
  talk by Rich Welsh, read) [G52]: the same bar doubles as a SUSPICION meter in non-hostile zones; a delay before
  decay was added because meters flickered when the player hovered at the boundary; restricted zones = sharp red
  map outlines + glowing red border round the minimap + audio cue; footsteps alone trigger investigation, not
  hostility; guards whistle/hum/cough to reveal their positions.
- Dishonored (Arkane, 2012) [G53] (guides/Steam, snippets, partly conflicting): an icon over the guard fills white,
  then red when spotted; Dishonored 2 uses lightning-bolt marks.
- Tenchu: Stealth Assassins (Acquire, 1998) [G54] (Wikipedia/StrategyWiki, snippets): the "Ki meter" combines a
  PROXIMITY number (0-100) with a symbol change for the nearest enemy's awareness.
- Hitman: Absolution (IO, 2012) [G55] (PC Gamer preview, snippet): a circular blot near the hit indicator grows as
  guards grow suspicious.
- Common grammar across these (INFERENCE from the set): punctuation ("?" suspicious, "!" alerted), fill-to-full
  progress, white -> red escalation, a flash at the threshold, and decay when you hide. The meaning is carried by
  shape AND fill AND colour together (XAG 103 redundancy), and the escalation is drawn where the eye already is
  (over the enemy or on the reticle).

### 5d. Stamina conventions
- BotW stamina wheel [G56] (fan/critic analyses, snippets; official rationale not found): radial wheel next to Link,
  shown only when stamina is being used; extra wheels stack as concentric segments. Analysts tie this to an "only
  essential UI" concept reported from Nintendo's CEDEC 2017 UI talk (unverified).
- Red Dead Redemption 2 [G57] (GameSpot/RDR2.org guides, snippets): three cores (health HEART, stamina LIGHTNING BOLT,
  Dead Eye EYE) as circles above the minimap; each has an outer RING (the bar) and an inner CORE (the reserve that
  sets regeneration); fortified = yellow highlight.
- Grounded (Obsidian) via XAG 103 [G03]: stamina gauge = green with a LIGHTNING symbol, next to blue droplet
  (thirst), yellow drumstick (hunger), red heart (health); colour is always doubled by the symbol.
- Convention (INFERENCE from G03/G57/G56): stamina = lightning bolt or a ring/wheel that drains; health = heart; the
  ring form suits a near-reticle or near-character placement because it is compact and direction-free.

### 5e. Stance / gait indicators
- Call of Duty 2 manual (snippet) [G58]: a triangle-shaped stance indicator beside the compass and ammo shows standing,
  crouching or prone. Call of Duty 4 (StrategyWiki, snippet): the stance readout adds a fourth state, carrying a
  partner. Pattern: the SAME human figure redrawn in each posture = one icon family whose members differ only in pose.
- In the sources read, no game shows WALK vs RUN as a persistent icon; speed/gait is normally implied by camera
  motion, FOV and sound (Skyrim folds it into the stealth eye; BotW into the noise waves). INFERENCE: FrontRooms' gait icon (slow / walk / run) has little direct AAA precedent, which is an
  opportunity: borrow the stance-silhouette family (CoD) for gait and the wave/ring family (BotW, Mark of the Ninja,
  Splinter Cell) for loudness, and keep the two dimensions visually separable.

### 5f. Shape vocabulary observed, and why it works (summary table)
| Shape | Games | Measures | Why it reads (INFERENCE unless cited) |
|---|---|---|---|
| Gem / jewel brightness | Thief [G37] | light on you | literal metaphor of being lit |
| Bar + ambient threshold mark | Splinter Cell CT [G41] | own noise vs room noise | comparison needs a reference line |
| Expanding waves (0..n) | BotW [G43]; SF speaker.wave [G04] | own loudness | universal speaker glyph; count = level |
| Ring at true radius | Mark of the Ninja [G44] | who can hear you | spatially honest; playtests failed until honest |
| Eye opening | Skyrim [G40]; RDR2 Dead Eye [G57] | being seen | the watcher's eye; animatable in steps |
| "?" / "!" | MGS [G49]; XAG 103 [G03] | enemy awareness | punctuation is pre-learned, culture-wide |
| Arrow that fills | Far Cry [G51][G52] | detection + direction | one glyph, two variables |
| Burst / explosion bubble | Dead by Daylight [G46] | a loud event was heard | onomatopoeic shape, with source variants |
| Lightning bolt / drain ring | Grounded [G03]; RDR2 [G57]; BotW [G56] | stamina | energy metaphor; ring drains like a clock |
| Temporary waveform "cover" sign | Sniper Elite [G73] | ambient noise masks you now | separates room loudness from own loudness |
| Human figure in poses | Call of Duty [G58] | stance | same figure, different pose = one family |

---------------------------------------------------------------------------------------------------------------
## 3. Recognisability techniques (rules with numbers where sources give them)

### 3a. Silhouette / 1-bit / greyscale test
- Valve checks that the shape read holds on black-and-grey models (no colour) and treats any playtest confusion as
  a redesign trigger [G29]; TF2 solved silhouettes first, interior detail second [G30].
- Riot judges recognisability by how a champion "looks in darkness" (pure silhouette) and keeps one primary feature
  invariant across every skin [G31].
- Logo/icon checkers (e.g. Logo Design Tester, search snippet [G62]) combine blur, greyscale, mirror and down-scale
  checks.
- INFERENCE for icons: fill every glyph solid at its final HUD size (1-bit). If the state cannot be named from the
  blob alone, the silhouette is wrong; colour and inner detail may only add, never carry, the meaning.

### 3b. Squint / blur test
- Squint test for signs (Matt Charboneau, GRAPHICS PRO 2021-05-12) [G62] (read): put the design on screen with white
  space round it, step back about 3 ft, squint until only the dominant elements survive, and check the primary
  element still reads "without hesitation"; design only for arm's length and it fails at other sizes.
- XAG 102's demonstration [G02]: at 74 % decreased sharpness, high-contrast text stays legible while low-contrast text
  disappears; contrast is what survives blur. A Cambridge tool (Clari-Fi, search snippet) calibrates icon blur
  against population acuity survey data.

### 3c. Minimum sizes, distance and stroke-to-size ratios
- XAG 101 [G01]: 26 px (console, 1080p) / 18 px (PC, 1080p) minimum body height for text, INCLUDING the letter inside
  a button glyph; icons scale with text up to 200 %.
- Microsoft "Designing for Xbox and TV" (10-foot experience, ms.date 2020-09-24) [G61] (read): assumes ~10 ft
  viewing; UI is laid out at 960 x 540 effective px and scaled 200 % to 1080p; interactive elements >= 32 epx tall
  (64 px at 1080p); main text >= 15 epx (30 px), supplemental >= 12 epx (24 px); keep essential UI 27 epx from top and
  bottom and 48 epx from left and right (54 / 96 px at 1080p) — only non-essential visuals in the outer 5 %; TVs
  render colour differently, so never rely on subtle colour differences; RGB 16-235 is the classic TV-safe range;
  TV information density should be closer to a phone than a desktop.
- Material [G06]: 2 dp stroke on a 24 dp icon (1/12 of the box, 1/10 of the 20 dp live area); 1.5 dp only for complex
  or very small glyphs; square terminals. Apple [G04][G05]: stroke weight follows the adjacent text weight.
- NN/g (Harley 2014) [G59]: favour basic schematic shapes over realistic detail, because fine detail is not
  distinguishable at small sizes.
- INFERENCE tied to FrontRooms type (stems.tsv in ../tools): TeX Gyre Heros Bold has vertical stem / cap height =
  0.206, Heros Cn Bold 0.171, Bayon 0.169-0.171. An icon set to sit beside Heros Bold caps at cap height should use a
  stroke of about 0.17-0.21 of the icon's cap-height box, i.e. roughly twice Material's relative weight. That is the
  "signage" weight, and it is also what survives blur and TV/phone distance.

### 3d. Filled vs outline, slash, enclosed, opacity: drawing STATES
- Apple [G04]: outline = default/inline with text; fill = emphasis and SELECTION (tab bars); slash = unavailable;
  enclosed (circle/square) = better legibility at small sizes.
- Material Symbols [G07]: the FILL axis (0..1) exists for state transitions; GRADE thickens without resizing (use for
  emphasis or on dark backgrounds). Material 1 [G06]: active 87/100 %, inactive 38/50 % opacity on light/dark.
- XAG 102/103 [G02][G03]: an inactive state may not be shown by greying alone; add a symbol (lock) or a label; inactive
  text still needs 3:1.
- Games: Far Cry/Dishonored fill an outline to show progress toward detection [G51][G53]; RDR2 fills the core icon
  and drains the ring [G57]; Skyrim opens an eye in steps [G40].
- INFERENCE: outline -> filled is the most widely learned "off -> on / low -> high" transition; slash is the
  learned "not available"; a stepped fill (0 / 1 / 2 / 3 segments) reads faster than a continuous bar at small size.

### 3e. Shape grammar: one shape family = one category
- TLOU2 Enhanced Listen Mode assigns CIRCLE = item, SQUARE = enemy [G16]; Riot reuses an established shape language
  for one meaning only (downward lines = spell shield) [G31].
- NN/g [G59]: use an icon for the same function everywhere; even slight drift in meaning undermines reliance.
- ISO 3864 / ISO 7010 safety signs are the strongest real-world shape grammar (geometric surround = category; see
  R3 for construction) and ISO 7001 deliberately avoids safety colours so information symbols are not misread as
  safety signs [G10].
- XAG 103 [G03]: "!" is a near-universal alert signifier, but should still carry a label for players who need it.

### 3f. Redundancy: shape + colour + label (+ sound / haptics)
- XAG 103 [G03]: colour never alone; critical colour-coded content gets shape, pattern, icon or label too; critical
  audio gets a visual with type and direction; haptics never alone.
- GAG [G08]: same rule for sound ("no essential information by sounds alone"), tested by a muted first-time playtest.
- NN/g [G59]: visible text labels even for standard icons, especially modified ones; hover-only labels fail on touch.
- TLOU2 [G16]: every state has at least two channels (shape + colour, visual + audio, visual + vibration).

### 3g. Colour-blind safety
- XAG 102/103 [G02][G03]: avoid red/green for targeting/critical elements; if colour is primary, let players choose
  colours (Black Ops Cold War triangles; Battlefield 2042 custom HUD colours with preview in dark/mid/bright scenes);
  test with colour-blind players, not just simulators.
- TLOU2 [G16]: HUD colour choice (white/yellow/blue/red/green) + colour-blind accent palettes.
- Microsoft TV guidance [G61]: colours shift between TVs, so distinct hues and values, not subtle ones.

### 3h. Consistent perspective and construction
- Material [G06]: front-facing, no tilt, no perspective, geometric, symmetrical, whole-pixel positions.
- Apple [G05]: same size, level of detail, stroke weight and perspective for every icon in the app; optical
  centring for asymmetric glyphs.
- INFERENCE: mixing a 3/4-view object icon with flat front-view pictograms breaks the family faster than any colour
  change; FrontRooms' print/signage language implies strictly flat, front or side views (as ISO pictograms do).

### 3i. Metaphor choice and cultural legibility
- Jakob Nielsen, "Icon Classification" (NN/g 2014-08-17) [G60] (read): RESEMBLANCE icons (picture of the thing) are
  most usable; REFERENCE icons (an object standing for a concept) work only if the analogy is close; ARBITRARY icons
  (warning triangle, "?") work only once standardised: do not invent new arbitrary icons. Meaning drifts over time
  (floppy-disk Save went from resemblance to arbitrary). Cultural failure example: only 9 % of Hungarians read a
  squash-player icon correctly.
- NN/g (Harley 2014) [G59]: only a few icons are truly universal (home, print, magnifier); heart and star are often
  misread; if it takes more than ~5 s to think of an icon for a concept, an icon will probably not communicate it.
- Apple [G05]: avoid images that are hard to recognise across cultures; prefer gender-neutral figures.
- INFERENCE for FrontRooms: "noise" is a reference concept (no physical object), so borrow an already-standardised
  sign (speaker waves / sound arcs, as in BotW and SF Symbols) rather than inventing a new arbitrary mark; "run" is a
  resemblance concept (a running figure) — but the ISO 7010 running man already means EXIT/escape (critical-confusion
  risk, see 1e), so the gait figure must differ clearly from the exit pictogram.

### 3j. Testing methods
- ISO 9186-1 / ANSI Z535.3 [G09][G11][G12]: show the symbol alone, ask open "what does it mean?", score correct and
  CRITICAL CONFUSION separately; bars 67 % (ISO) / 85 % with < 5 % critical confusion (ANSI).
- NN/g [G59]: recognisability test (no label, ask meaning) + memorability test (same people, a couple of weeks later).
- Valve [G29], Klei [G44]: playtest observation; confusion is a design bug. Klei's fix was to make the graphic honest
  to the rule. GAG [G08]: muted playtest for audio-only information. XAG [G03]: test with colour-blind players.
- INFERENCE: for a game icon, add a TIME criterion (correct answer within ~1 s at real HUD size during play), since a
  HUD state is read in a glance, not studied.

---------------------------------------------------------------------------------------------------------------
## 4. Personality and expressiveness without losing recognisability

### 4a. Put personality in CONSTRUCTION (corners, terminals, weight, one signature move), keep the skeleton
- Material Symbols ship one skeleton in three styles (Outlined / Rounded / Sharp) and four variable axes [G07];
  Material 1 fixed square terminals and a 2 dp exterior radius as the house signature [G06]. The style lives in the
  corner and the stroke end; the recognisable skeleton never changes.
- SF Symbols: 9 weights and 3 scales tied to the system font; custom symbols must start from the template of the
  nearest existing symbol so detail, optical weight, alignment and perspective stay in family [G04]. Apple: match
  icon weight to adjacent text unless emphasis is intended [G05].
- Riot: one invariant "primary feature" per character survives every skin; "clarity parity" forbids any variant to
  be less clear than the base [G31]. Valve: the two TF2 teams' worlds differ in construction (warm/angled vs
  cool/orthogonal), not only colour [G29].
- INFERENCE for FrontRooms: derive the icon construction from TeX Gyre Heros Bold / Cn Bold (stroke ~0.17-0.21 of cap
  height, flat horizontal/vertical terminals like Helvetica's, tight apertures), from the 1990 sign grammar (ISO 3864
  surrounds, see R3), and from print (solid ink, no gradients, no glow). Personality = one signature cut used
  everywhere (e.g. a 45-degree chamfer, or a printed-ink overshoot), never a per-icon ornament.

### 4b. Characters and mascots as icon systems
- Vault Boy (Fallout, 1997-) [G64] (Wikipedia, read): created by Leonard Boyarsky as the "skill guy"; one cartoon man
  drawn in many poses ("cards", after Monopoly cards) represents every stat, skill and perk; inspired by 1950s civil-
  defence films (Bert the Turtle, Duck and Cover, 1952): cheery optimism over paranoia; the grin stays even in the
  "immobile" (crippled) card. The Pip-Boy [G63] (Wikipedia, read): Boyarsky wanted old, clunky, hacked-together tech
  so the device itself says the world is not quite working; layout by Anthony Postma.
- Persona 5 (Atlus, 2016) [G65][G66]: director Katsura Hashino (via The Verge, reported by Kotaku 2024-10-07, read):
  every menu is designed (and even runs) as its own piece, which is slow and "really annoying"; the angular black /
  red / white text boxes were hard to read and were fixed by heavy iteration. UI designer Masayoshi Suto (snippet):
  he came to treat UI as graphical entertainment, not just laying out information; CEDEC Award 2019. Critics
  (Inverse, snippet) note moving axes and dynamic baselines cost legibility; Persona 5 Tactica went calmer.
- Design reading: one figure in many poses is an established, characterful state system (Vault Boy, CoD stance);
  the tone comes from the drawing, the legibility from the invariant figure. Maximal style (Persona) works for menus
  read at leisure, not for a HUD state read in a glance under threat.
- INFERENCE for FrontRooms: the 1990 equivalent of Vault Boy's civil-defence irony is the corporate safety-pictogram
  figure (calm, faceless, compliant) shown in states the building should not allow (running in an office). The irony
  is the personality; the ISO-like figure is the legibility.

### 4c. Motion: animate the STATE CHANGE, not the icon
- SF Symbols attaches one meaning to each motion [G04]: bounce (something happened), pulse/breathe (ongoing),
  variable colour (level/progress), replace down-up (state change), wiggle (call to action), draw on/off (direction,
  progress); use sparingly, one purpose per animation, match the product's tone, test shapes in motion.
- Games: MGS pairs the "!" pop with a sting sound, a single event animation [G49]; Skyrim opens the eye in discrete
  steps [G40]; Far Cry fills then FLASHES at the threshold [G51]; Far Cry 6 added a delay before decay to stop meter
  flicker at a boundary [G52]; Amnesia uses a pulse + sound for sanity drops and toned effects down to avoid
  nausea [G28]; Dead by Daylight bursts a bubble at the noise source [G46]; Alien: Isolation's maps and menus flicker
  like worn tape [G20].
- Limits: GAG basic rule "avoid flickering images and repetitive patterns" [G08]; TLOU2 lets players turn HUD
  flashing off [G16]; XAG photosensitivity guidance exists (not read here).
- INFERENCE for FrontRooms: (1) state changes get one short transition (gait step-up = replace/"stamp"; noise spike =
  one pulse), steady states stay still; (2) add hysteresis (a decay delay) so the noise icon does not flicker between
  walk and run; (3) motion can be "print-true" (a rubber-stamp hit, a misregistered overprint) or "CRT-true"
  (scanline roll), but never a soft glow (A24: walls never glow).

### 4d. Diegetic vs non-diegetic placement
- The framework [G14][G15] and the rule that function must survive translation into the world [G15].
- Body/equipment-mounted state: Dead Space spine [G17][G19]; Metro 2033 wristwatch + journal [G39]; Half-Life: Alyx
  gloves [G35]; RE7 wrist [G34]; Fallout Pip-Boy [G63]; Outlast camcorder (battery icon turns red and blinks, night
  vision flickers as it empties; search snippets) [G67].
- In-world guidance: Ghost of Tsushima wind [G22][G23]; Mirror's Edge red objects [G26][G27]; Dead Space floor
  locator [G15]; Firewatch paper map [G36].
- Spatial overlays: Mark of the Ninja sound rings [G44]; Far Cry / MGS marks over enemies [G49][G51].
- Constraints: GAG — temporary info in the eye-line, persistent info may sit at edges [G08]; Ghost of Tsushima's wind
  can lose contrast (accessibility review, snippet) [G22]; Andrews — fully diegetic FPS UI is rarely workable [G15].
- INFERENCE for FrontRooms (first person, no body on screen, phones + desktop): a fully diegetic gait icon is
  impractical (no visible body/device); the best fit is a META treatment: a non-diegetic icon whose MATERIAL is
  fiction-true (printed sign / form stamp / CRT readout), placed by GAG's rule (persistent corner + transient echo at
  the reticle).

### 4e. Materiality (print, CRT, hologram) as personality
- Alien: Isolation filmed its screen graphics off real VHS/Betamax and an old portable TV; health resembles a
  camcorder battery; only pre-1979 objects allowed as references [G20].
- Dead Space: holographic blue projections justify every overlay in-fiction [G18]. Pip-Boy: old clunky tech [G63].
- Control: the authority of clean bureaucratic type, highway-style signage, one signal red [G24][G25].
- INFERENCE for FrontRooms: the era lock (<= 1993) applies to the icon's material as much as to its subject: offset
  or photocopy ink on canary paper (#FCF7CE, per the print rule), stencil/engraved plates, phosphor CRT text. A real
  1990 office has no "noise meter", but the era does have VU-meter needles on tape decks, LED bar-graph level
  ladders on hi-fi and PA equipment, and "QUIET PLEASE" signage: credible metaphors for loudness in the era.

### 4f. How horror games keep icons minimal and fiction-true
- Remove numbers; show qualitative steps; let sound carry the rest (Amnesia [G28]).
- Put precise state behind a deliberate look (RE status screen / wrist [G33][G34]; Metro watch [G39]; Alien tracker
  that must be raised and costs attention [G20]).
- Keep what remains on screen tied to the threat's own sense: Splinter Cell's noise vs ambient [G41], Mark of the
  Ninja's honest ring [G44], Dead by Daylight's "heard" burst [G46], TLOU2's awareness indicator [G16].
- Never let UI feedback become an exploit: Alien: Isolation's music follows what you see, not where the Alien is [G20].
- Give options: TLOU2 HUD scale/colour/background [G16]; BotW Pro HUD [G43]; Metro Ranger mode [G39]; Firewatch
  "show location" toggle [G36].

---------------------------------------------------------------------------------------------------------------
## 6. Candidate reference images (25; NOT downloaded — page URL + direct image URL + what it shows + rights)
Direct URLs were checked with header-only requests on 2026-10-30 (200 image/* unless noted). Rights: Microsoft Learn,
Apple and Google figures are the publishers' copyright (use as internal reference only unless their terms allow);
game screenshots are the publishers' copyright (Wikipedia hosts them as non-free/fair use); ISO 7010 E002 on
Wikimedia Commons is marked public domain by its uploader.

| # | Shows | Page | Direct image URL | Rights / credit |
|---|---|---|---|---|
| R01 | Button glyph anatomy: whole icon vs the text inside it (XAG 101) | [G01] learn.microsoft.com/.../xbox-accessibility-guidelines/101 | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/glyph_size_diagram.png | © Microsoft |
| R02 | "Don't": line text 18 px but the X inside the button 13 px | [G01] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/glyph_container1.png | © Microsoft |
| R03 | "Do": text line enlarged so the glyph fits the line container | [G01] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/glyph_container_3.png | © Microsoft |
| R04 | For Honor team symbols with black inner + white outer outline | [G02] .../102 | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/for-honor-hud.png | © Ubisoft (via Microsoft) |
| R05 | Forza Horizon 4 map icons: yellow fill, black outline, "NEW" label | [G02] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/forza-map.png | © Microsoft / Playground |
| R06 | Contrast ratios 1.2:1 to 21:1, sharp vs 74 % blurred | [G02] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/contrast-ratio-demo.png | © Microsoft |
| R07 | Grounded: symbol + label tabs; HUD gauges lightning (stamina) / droplet / drumstick / heart | [G03] .../103 | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/grounded-craft-menu.png | © Obsidian / Xbox |
| R08 | Black Ops Cold War: player-chosen colours for map triangles | [G03] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/blops-colorblind.png | © Activision |
| R09 | Sea of Thieves locked item: greyed + chains + lock symbol (not grey alone) | [G03] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/sot-merchant.png | © Rare / Xbox |
| R10 | Battlefield 2042 custom colour-blind HUD colour pickers | [G02] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/battlefield_color_selection.png | © EA DICE |
| R11 | Immortals Fenyx Rising: white arrow with black outline toward attacker | [G02] | https://learn.microsoft.com/en-us/gaming/images/gaming-accessibility/fenyx_hc_arrow.png | © Ubisoft |
| R12 | TV-unsafe area diagram (10-foot UI) | [G61] | https://learn.microsoft.com/en-us/previous-versions/windows/uwp/xbox-apps/images/designing-for-tv/tv-unsafe-area.png | © Microsoft |
| R13 | SF Symbols: one glyph in 9 weights x 3 scales | [G04] | https://developer.apple.com/tutorials/images/com.apple.HIG/sf-scales-weights@2x.png | © Apple |
| R14 | SF Symbols: medium scale between cap line and baseline | [G04] | https://developer.apple.com/tutorials/images/com.apple.HIG/sf-symbol-scale-medium@2x.png | © Apple |
| R15 | SF Symbols variable colour: speaker.wave.3 with 0-3 waves lit (closest model for a noise level icon) | [G04] | https://developer.apple.com/tutorials/images/com.apple.HIG/sf-variable-color@2x.png | © Apple |
| R16 | SF Symbols variants: outline / fill / slash / circle / square / rectangle | [G04] | https://developer.apple.com/tutorials/images/com.apple.HIG/sf-variants@2x.png | © Apple |
| R17 | Material 1: 20 dp live area inside 24 dp | [G06] | https://storage.googleapis.com/material-io-design/m1/assets/0Bx4BSt6jniD7X2dma3g0Z3lZRG8/style-icons-system-grid-area-live.png | © Google |
| R18 | Material 1: circle keyline 20 dp | [G06] | https://storage.googleapis.com/material-io-design/m1/assets/0Bx4BSt6jniD7bUlaTVBxVkVkYk0/style-icons-system-grid-shapes-circle.png | © Google |
| R19 | Material 1: consistent 2 dp stroke | [G06] | https://storage.googleapis.com/material-io-design/m1/assets/0Bx4BSt6jniD7TkF3OXNxNGczcGc/style-icons-system-anatomy-strokes-consistency.png | © Google |
| R20 | Material 1: 2 dp exterior corner radius | [G06] | https://storage.googleapis.com/material-io-design/m1/assets/0Bx4BSt6jniD7alFGeDJYR1RlRDQ/style-icons-system-anatomy-corner-exterior.png | © Google |
| R21 | Mark of the Ninja: noise of a thrown item drawn as a circle | https://en.wikipedia.org/wiki/Mark_of_the_Ninja | https://upload.wikimedia.org/wikipedia/en/e/ef/Markoftheninja_screenshot.png | © Klei; non-free (Wikipedia fair use) |
| R22 | Dead by Daylight loud noise notification (generic burst bubble) | [G46] | https://deadbydaylight.wiki.gg/images/thumb/LoudNoiseNotification_Generic.jpg/800px-LoudNoiseNotification_Generic.jpg (as listed; scripts get 403) | © Behaviour; wiki text CC BY-NC-SA 3.0 |
| R23 | Dead by Daylight source-specific variant (generator completion) | [G46] | https://deadbydaylight.wiki.gg/images/thumb/LoudNoiseNotification_GeneratorCompletion.jpg/800px-LoudNoiseNotification_GeneratorCompletion.jpg (as listed; scripts get 403) | © Behaviour |
| R24 | ISO 7010 E002 emergency exit (running figure): the shape the RUN icon must NOT be confused with | https://commons.wikimedia.org/wiki/File:ISO_7010_E002.svg | https://upload.wikimedia.org/wikipedia/commons/3/35/ISO_7010_E002.svg | Public domain (uploader MaxxL, PD-self) |
| R25 | Riot: champion silhouettes in darkness (silhouette test) | [G31] | https://cmsassets.rgpub.io/sanity/images/dsfx7636/news_live/b64db0be9772e8d6b28eb16e09ab34af8a4573b6-1920x650.jpg | © Riot Games |
Also useful but not counted: Thief carpet-and-tile corridor screenshot (noise surfaces), https://upload.wikimedia.org/wikipedia/en/2/2a/Baffscrn1.jpg
(© Eidos/Looking Glass, non-free); TF2 silhouette slides (PDF, page only): https://cdn.fastly.steamstatic.com/apps/valve/2007/NPAR07_IllustrativeRenderingInTeamFortress2_Slides.pdf

---------------------------------------------------------------------------------------------------------------
## 2l. Other titles from the brief (thin public record; brief notes so nothing is lost)
- Death Stranding (Kojima Productions, 2019) [G68]: Masahiro Sakurai (Famitsu column #593, Source Gaming translation
  2020-03-16, read) found the button prompts and text too small to see and noted the many long button holds. No
  first-party UI rationale found. Lesson: even a top-tier AAA HUD can fail the minimum-size rule (XAG 101).
- Hades (Supergiant, 2020) [G69]: Kotaku "The Art of Hades" (Luke Plunkett, 2020-10-22, read): Supergiant's update
  credits a resident UI/VFX artist for the menu animations since Transistor and calls menu screens a chance to draw
  the player into the world. No boon-icon construction rules were found.
- Doom Eternal (id Software, 2020) [G70] (search snippets only): Hugo Martin / Marty Stratton cited the 1993
  pickups, whose shapes were recognisable at a distance for snap decisions; colour-blind options were promoted at
  launch; critics found the bright HUD effects heavy.
- Overwatch / Overwatch 2 (Blizzard) [G71] (search snippets only): strong hero silhouettes; enemy red / ally blue
  outlines and name tags; OW2 lets players recolour UI/HUD elements and outlines but not ability effects (forum
  report). No official icon-construction document found. Valorant: no Riot write-up on ability icons was found; the
  LoL "Clarity" rules [G31] are Riot's public statement of principle.
- Dead Space remake (Motive, 2023) [G72] (search snippets only): keeps the diegetic RIG (health and stasis on the
  suit, ammo on the weapon); no Motive UI interview was found.
- Hitman (IO, 2016-): only Absolution's growing suspicion blot is documented in what was read [G55]; the 2016 HUD
  was not covered by any readable source this session.

---------------------------------------------------------------------------------------------------------------
## 7. Design principles distilled for FrontRooms (each tied to sources; INFERENCE = my synthesis)

1. FUNCTION FIRST, SKIN SECOND. Every icon must pass a 1-bit silhouette test and a squint/blur test at its smallest
   real size (PC 1080p corner, iPhone corner) BEFORE any print, stamp or CRT texture is applied; texture may add
   character but may never carry meaning. [G29 greyscale check][G31 silhouette in darkness][G62 squint][G02 blur demo]

2. ONE SHAPE FAMILY = ONE MEANING, FOR THE WHOLE GAME. Fix the families once: GAIT = one human figure;
   LOUDNESS = sound arcs; "THE BUILDING HEARD YOU" = a burst/punctuation mark; KEY and EXIT = the existing signage
   language. Never reuse a family for a second meaning, and never let a decorative element borrow one.
   [G16 TLOU2 circle=item, square=enemy][G31 reuse shape language for one meaning][G59 same icon = same function]
   [G10 ISO 7001 keeps clear of safety-sign colours; R3 for ISO 3864 surrounds]

3. GAIT = ONE FIGURE, THREE POSES. Draw slow-walk / walk / run as the same figure (same head, stroke, proportions =
   the invariant "primary feature"), changing only stride and lean, the way stance icons and Vault Boy do. RUN must
   not read as the ISO 7010 E002 emergency-exit running man (no door, no arrow, no exit green, different stride and
   lean): in this game "exit" vs "you are loud" is a CRITICAL CONFUSION. [G58 CoD stance family][G64 Vault Boy
   poses][G31 invariant primary feature][G12 critical confusion][R24 ISO 7010 E002]

4. LOUDNESS IS A LEVEL WITH FEW HONEST STEPS. Show noise with the pre-learned speaker/wave metaphor in discrete steps
   (silent 0 arcs, walk 1, run 2-3), not a continuous bar and never a number; each step must equal what Relay can
   actually hear (Mark of the Ninja's playtests failed until the ring matched the real radius). If the hunter's
   hearing depends on room ambience, show the player's level against a mark for the room's ambient level, as
   Splinter Cell: Chaos Theory did (Sniper Elite shows the masking moment as a separate temporary sign).
   [G04 variable colour speaker.wave.3][G43 BotW waves][G44][G45 honest ring][G41][G42 ambient mark]
   [G73 sound masking][G28 no numbers in horror]

5. KEEP THE THREE VARIABLES SEPARABLE: what you DO (gait figure), what the building HEARS (arcs), what you HAVE
   (stamina). Stamina uses a third, different form (a draining ring or 2-3 segments) shown only while dashing or
   recovering, like BotW's wheel; health-like icons are not needed. INFERENCE from [G56 BotW][G57 RDR2 ring + core]
   [G03 Grounded lightning = stamina].

6. REDUNDANCY IS MANDATORY BECAUSE THE THREAT IS AUDIO. Each state = shape + fill/weight + (optional) one label in
   Heros Cn Bold caps; colour is only the third channel; the visual is echoed by sound and haptics. The player's
   loudness is critical audio information, so deaf and hard-of-hearing players need it visually (XAG 103, GAG
   "nothing by sound alone"). [G03][G08][G16 TLOU2 every state has two channels][G59 visible labels]

7. PLACE BY THE EYE-LINE RULE. Persistent state (gait + arcs) lives at one fixed corner inside the safe area (Microsoft
   10-foot: essential UI 27 epx from top/bottom and 48 epx from the sides at 960x540; FrontRooms' phone rule of 16 pt
   from rounded corners). Transient events ("heard", stamina empty) echo for about a second where the eye already
   is, ideally by transforming the existing reticle dot rather than overlaying a new element (Skyrim's eye-reticle;
   RE2's tightening crosshair; the project's own rule of animating the crosshair dot, never overlaying).
   [G08 temporary info in eye-line][G61 safe area][G40 Skyrim][G33 RE2 crosshair]

8. SIZE AND WEIGHT COME FROM THE TYPE. Draw icons at the cap height of the label they sit beside (SF Symbols'
   "medium" logic: just overshooting cap line and baseline) and match stroke to the face: TeX Gyre Heros Bold
   stem/cap = 0.206, Heros Cn Bold 0.171, Bayon 0.17 (stems.tsv). That is about twice Material's relative stroke
   (2 dp on 24 dp), i.e. signage weight, which is also what survives blur and phone distance. Letters or numerals
   inside an icon obey XAG minimums (PC 18 px body height at 1080p; mobile about 72 px at 400 DPI) and scale to
   200 %. [G04 weights/scales tied to font][G05 match icon weight to text][G06 2 dp/24 dp][G01 glyph text minimums]

9. CONTRAST BY KEYLINE, NOT BY BOX. Meet 4.5:1 against the LOWEST-contrast part of the moving background (7:1 in a
   high-contrast mode). Over 3D scenes, use a double keyline (ink inside, paper outside) like For Honor's symbols,
   which also fits the project rule of keylines instead of dark backing cards; avoid red/green pairs; on TVs avoid
   subtle hue differences. [G02 4.5/3/7:1, For Honor double outline][G03 colour-blind][G61 TV colour variance]

10. ONE STATE GRAMMAR. Outline = available/idle; filled = active/selected; slash = blocked/unavailable (e.g. dash
   refused while exhausted); enclosed = extra legibility at small size; greying is never used alone (pair with a
   slash or a word). Prefer stepped fills (0/1/2/3) to continuous bars at HUD size. [G04 outline/fill/slash/enclosed]
   [G07 FILL axis for state transitions][G06 opacity states][G03 grey + lock][G40 Skyrim stepped eye]

11. MOVE ONLY ON CHANGE, AND NEVER FLICKER. One short transition per state change (a rubber-stamp "replace" when the
   gait steps up; one pulse when Relay hears a spike); steady states are still. Add a decay delay (hysteresis) so the
   icon does not chatter at the walk/run boundary; no glow (in line with the project's "walls never glow" rule);
   offer "HUD flashing off".
   [G04 one meaning per animation][G52 Far Cry 6 decay delay][G08 avoid flicker][G16 HUD flashing option]
   [G28 pulse + sound, toned down]

12. PERSONALITY THROUGH CONSTRUCTION, MATERIAL AND IRONY, NOT ORNAMENT. Pick ONE signature construction move (from the
   type: flat Helvetica terminals, one chamfer or one ink-trap) and apply it to every icon; use era-true material
   (offset ink on canary paper, engraved plate, phosphor text) and era-true metaphors (VU-meter needle, LED level
   ladder, QUIET sign; no holograms, no glow). The calm, compliant corporate pictogram figure caught running in a 1990
   office is the system's dark joke, as Vault Boy's grin is Fallout's. Every themed variant must be at least as
   clear as the base ("clarity parity"). [G07 one skeleton, three styles][G31 clarity parity][G64 Vault Boy irony]
   [G63 Pip-Boy clunky tech][G20 Alien era lock, VHS][G24 Control bureaucratic type]

13. HORROR RESTRAINT WITHOUT DISHONESTY. No numbers, few qualitative steps, precise detail behind a deliberate look
   (the evacuation placard, a form); but the noise rule is a survival rule, so its icon must be honest and
   predictable (Mark of the Ninja) rather than vague (Amnesia's sanity). Never let UI feedback become an exploit or a
   detector the fiction does not allow (Alien: Isolation's music follows what you see, not the Alien).
   [G28][G44][G20][G41]

14. TEST IT LIKE A SAFETY SIGN, THEN LIKE A GAME. (a) ISO 9186-style open-answer test of each icon alone: >= 67 %
   correct (aim for ANSI's 85 % on the run/noise icon) and < 5 % critical confusions ("quiet/safe" read for loud;
   "exit" read for run). (b) In-play glance test: named correctly within about 1 s at real size. (c) Memorability
   re-test after a gap. (d) Muted playtest and a colour-blind check with real players. [G09][G11 67 %/85 %]
   [G12 critical confusion][G59 recognisability + memorability][G08 muted test][G03 test with colour-blind players]

15. GIVE THE PLAYER THE DIALS. HUD scale to 200 %, a high-contrast/colour option, and a HUD-off ("Ranger"/"Pro HUD")
   mode where the same information falls back to sound and the world; this matches what Naughty Dog, Nintendo and 4A
   shipped and FrontRooms' existing settings menu. [G01 200 % scaling][G16 TLOU2 HUD options][G43 BotW Pro HUD]
   [G39 Metro Ranger mode][G36 Firewatch location toggle]

---------------------------------------------------------------------------------------------------------------
## 8. Gaps, weak spots and how to read this file
- Section order is 1, 2, 5, 3, 4, 6, 2l, 7, 8 (written incrementally; section numbers follow the brief).
- Paywalled/unreachable: ISO 9186-1 / ISO 7001 / ISO 22727 / ANSI Z535.3 full texts (thresholds come from secondary
  sources); m3.material.io and Apple HIG HTML (Material 3 read via Google Fonts docs + Material 1 spec; Apple via
  the HIG JSON feed); Game UI Database, Giant Bomb, TV Tropes, Zelda wikis, StrategyWiki returned 403 to the fetcher.
- No first-party written icon-construction guideline from a game studio was found in public (Bungie's icon pipeline
  exists only as a GDC talk video; Remedy, Blizzard, Riot (Valorant), Supergiant, Kojima Productions, IO have no
  public icon spec). The closest public studio rules are Riot's "Clarity in League" and Valve's TF2 readability talks.
- Facts marked "search-snippet" in R2_sources.tsv (Splinter Cell CT ambient mark, BotW noise waves, Far Cry arrow,
  Dishonored, Tenchu, RDR2, Doom, Overwatch, Outlast, Dead Space remake, Alien hearing the tracker) should be checked
  against footage before being quoted in a deck.
- Verbatim quotes are kept under 15 words (stricter than the brief's 25) to respect copyright; everything else is
  paraphrase.
- WebFetch automatically cached one PDF it could not parse (IIID Symbol Comprehension Testing Protocol) in this
  session's tool-results folder; it was not opened or used.
