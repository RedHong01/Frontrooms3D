# AI, pacing and core-system diagrams in horror and stealth games (notes for FrontRooms)

*Researched 2026-10-15. Every finding carries its source link. Slide and figure numbers point at the original images inside the linked PDFs and articles, so research frames can cite the originals. Nothing was saved into the project except this file. Almost everything is paraphrased; the only verbatim prose is one short Booth line in Q1.*

---

## Q1. What exactly does the Left 4 Dead intensity graph show (axes, phases), and how did Valve present the AI Director?

### Takeaway
Booth's "intensity graph" is not a single plotted curve. It is a stacked swimlane chart on one shared, unlabeled left-to-right axis with no numeric scales. The SURVIVOR INTENSITY trace has horizontal threshold lines and dark-red RELAX windows. Under it, an ACTUAL POPULATION strip of enemy icons goes empty inside those windows. The final build adds a DESIRED POPULATION strip on top, so the audience can see which planned threats the Director removed. The four phases (Build Up, Sustain Peak, Peak Fade, Relax) are defined in bullet text; only RELAX is drawn on the chart.

### Cited Findings

**Primary sources and how the talk is organised**
- Valve hosts both of Michael Booth's 2009 decks as PDFs:
  - "The AI Systems of Left 4 Dead" (AIIDE 2009, 95 slides): [Booth, AIIDE 2009 slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
  - "Replayable Cooperative Game Design: Left 4 Dead" (GDC 2009): [Booth, GDC 2009 slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
  - Valve's publications index: [Valve publications](https://www.valvesoftware.com/en/publications).
- The AIIDE deck introduces L4D as a replayable, cooperative survival-horror game (slide 2). It is organised under four goals, and the goals slide is reused as a section divider. The goals are: Deliver Robust Behavior Performances; Provide Competent Human Player Proxies; Promote Replayability; Generate Dramatic Game Pacing (slide 77 and the dividers) — [AIIDE slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).

**The pacing rules (text slides 78–82 and 92 of the AIIDE deck, repeated in the GDC deck)**
- **Why:** pacing is adjusted on the fly to maximise drama. The model is Counter-Strike's spiky rhythm: quiet tension broken by sudden fights. Constant combat is fatiguing and long inactivity is boring — [AIIDE slide 78](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
- **Algorithm:** estimate each Survivor's emotional intensity and track the maximum of the four. If it is too high, remove major threats for a while; otherwise create an interesting threat population — [AIIDE slide 79](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
- **Survivor Intensity** is a single floating-point value.
  - It rises with damage (proportionally), incapacitation, being pulled or pushed off a ledge, and nearby Infected deaths (inversely proportional to distance).
  - It decays toward zero, but does not decay while Infected are actively engaging the Survivor.
  - Sources: [AIIDE slide 80](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
- **The four phases** — [AIIDE slide 81](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); full wording in the [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf):
  - **Build Up:** full threat population until intensity crosses the peak threshold.
  - **Sustain Peak:** keep the full population for 3–5 s after the peak.
  - **Peak Fade:** switch to the minimal population and wait for intensity to decay out of the peak range. This phase exists so the current fight can play out without using up the Relax time, and Relax does not start until there is a natural break in the action.
  - **Relax:** keep the minimal population for 30–45 s, or until the team has travelled far enough toward the next safe room, then return to Build Up.
- **Full vs. minimal population:**
  - Full (Build Up) means wanderers, mobs and Special Infected.
  - Minimal (Relax) means no mobs and no new Specials, though existing Specials may still attack. The GDC deck adds: no wanderers until the team is calm.
  - Boss encounters (Tank/Witch) are deliberately left out of adaptive pacing, because they exist to change the pacing themselves.
  - Sources: [AIIDE slide 82](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
- **Conclusion slide:**
  - The system changes pacing, not difficulty: "Amplitude (difficulty) is not changed, frequency (pacing) is".
  - Peaks arrive at different times and places on each run.
  - Booth admits the intensity estimate is crude, but the resulting pacing works.
  - Source: [AIIDE slide 92](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
- **Future work:** give the Director more verbs — [AIIDE slide 93](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).

**The graph as drawn (read directly from the slide images in the AIIDE PDF)**
- **Slide 83, "An example procedurally generated population":**
  - A single left-to-right arrow with enemy icons placed along it.
  - Many small white figures, scattered and in clusters; a few single coloured figures (red, orange, blue); one large green figure.
  - Source: [AIIDE slide 83](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
- **Slides 84–90** build a two-lane chart one step at a time. Each is titled as showing how the Director modulates the population based on the team's "emotional intensity".
  - **Top lane, SURVIVOR INTENSITY:** a thin white trace that rises and falls. There are horizontal reference lines: red and yellow near the top, green near the baseline.
  - **RELAX windows:** dark-red shaded bands labelled RELAX. Each starts at a vertical red marker where the trace reaches the top band and ends at a later vertical marker.
  - **Bottom lane, ACTUAL POPULATION:** the same icon strip, with most icons gone inside the RELAX windows.
  - Source: [AIIDE slides 84–90](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
- **Slide 91, "Comparing population after modulation by the AI Director":**
  - Three lanes on one axis: DESIRED POPULATION (top), SURVIVOR INTENSITY (middle), ACTUAL POPULATION (bottom). The vertical markers run through all three lanes.
  - Icons present in the desired strip are missing from the actual strip during RELAX.
  - A red cluster appears in the actual strip late in the run that is not in the desired strip.
  - Source: [AIIDE slide 91](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
- **What the chart leaves out:** neither axis has numbers or units, and the x-axis is an unlabeled arrow. Build Up, Sustain Peak and Peak Fade are not labelled; only RELAX is — [AIIDE slides 84–91](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
- **Slide 81 also has an in-game debug readout:** four vertical intensity bars, one above each Survivor's portrait (one bar high and yellow-olive, three low and green), with the Director state printed underneath as BUILD_UP — [AIIDE slide 81](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).

**Other diagram grammar in the two decks**
- **Population rules** are shown as bullet slides plus level and nav-mesh images. Source for all bullets below: [AIIDE slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
  - **Active Area Set:** the nav areas around the team. The Director creates and destroys population as this set moves.
  - **Structured unpredictability:** randomness bounded by the designer. Example: mobs every 90–180 s on Normal, with no mobs in Relax. Boomer vomit forces a mob and resets the timer.
  - **Wanderers:** a count per area, zeroed when the area becomes visible to a Survivor or the Director is in Relax.
  - **Specials:** spawned only in areas the team cannot currently see.
  - **Bosses:** dealt from a shuffled Tank/Witch/Nothing deck, with no repeats in a row.
  - **Items:** weapon caches and item groups are chosen by the Director from candidates the designer placed.
- **Agent architecture** is drawn as a component tree.
  - Actor → Locomotion, Body, Vision, Intention. Intention holds concurrent Behaviors (HFSM + stack); each Behavior holds Actions, which can hold child Actions.
  - Events such as OnSight, OnLostSight, OnSound and OnInjured pass from the innermost Action outward.
  - Source: [AIIDE behaviour slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf). The GDC deck calls this a custom HCSM system with integrated event processing: [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
- **The GDC deck names the drama the Director serves.** Source: [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
  - **Dramatic Anticipation:** event X signals that event Y is coming shortly. Examples:
    - the Tank's music, distant yelling and heavy footfalls;
    - the Witch's sobbing, especially when you can't tell where she is;
    - Infected breaking through doors, which Booth calls a classic horror-film moment;
    - Boomer bile.
  - Booth's summary rule is that an exciting event becomes more exciting if it announces its arrival.
  - **Structured Unpredictability:** low probability plus high drama makes a moment memorable.

**A second shipped example of the same graph: Warframe's AI Director**
- **Intensity measure:** damage raises it, kills raise it in proportion to distance, it holds while the player is targeted, and otherwise it decays. Brewer cites Booth as the model — [Brewer, "Managing Pacing in Procedural Levels in Warframe", Game AI Pro Online 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter07_Managing_Pacing_in_Procedural_Levels_in_Warframe.pdf).
- **Figure 4** is an intensity-over-time line chart. Its labels mark the rise during combat, a peak (intensity 100), a no-spawn decay window after the peak, and the restart of spawns. The text calls the target shape a rollercoaster — [Brewer 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter07_Managing_Pacing_in_Procedural_Levels_in_Warframe.pdf).
- **Figure 5** is a population graph.
  - Axes: the share of the total population spawned (y) against progress through the level (x).
  - The curve is piecewise linear with two peaks. Variants for Exterminate and Crossfire missions are shown, plus an adjusted curve whose peaks are shifted to match the generated layout.
  - Source: [Brewer 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter07_Managing_Pacing_in_Procedural_Levels_in_Warframe.pdf).
- **Figure 3** compares the TacMap (the Director's graph of areas) with the navmesh and the level block. Other details from the chapter — [Brewer 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter07_Managing_Pacing_in_Procedural_Levels_in_Warframe.pdf):
  - A Dijkstra "distance map" flooded from the objective gives the players' progress and direction of travel.
  - Spawns go ahead of the players and out of sight, except at special visible spawn points used to stage an arrival.
  - Spawning more enemies when players head toward the objective gently leads them to it.

### Inferences
- **Diagram card: stacked pacing timeline (swimlanes)**
  - *Shows:* cause (player stress) and effect (threat presence) on one time axis.
  - *Grammar:* a shared arrow axis, one lane per signal, shaded phase windows, vertical event markers through every lane, icons for discrete entities, no numeric scale.
  - *Used for:* explaining a director to a mixed audience, built up slide by slide.
  - *Strengths:* shows the feedback loop and, with the desired-vs-actual lanes, exactly what the director changed.
  - *Limits:* qualitative only; the transition rules live on separate text slides; only one phase is drawn.
- **For FrontRooms, this is the best master diagram for the Relay.** Suggested lanes:
  1. Director phase band: RELAX / BUILD-UP / PENDING / ENCOUNTER / SUSTAIN / FADE, as colour bands with a label on each.
  2. A tension proxy line with its thresholds.
  3. Relay state: Away / Arrive / Withdraw.
  4. Warning stage: lamps → steps → lock-on chime.
  5. Player context: trigger room entered, door shut, sanctuary reached.
  - Borrow the slide-91 trick: a "planned Relay visit" lane above an "actual" lane, showing where a shut door or a sanctuary cancelled or postponed an arrival.
- **Phase mapping (inference):**
  - Build Up ≈ BUILD-UP; Sustain Peak ≈ SUSTAIN; Relax ≈ RELAX.
  - Peak Fade ≈ FADE. L4D's rule that Relax waits for a natural break suggests FADE should end on a player-side event (door shut, sanctuary reached), not only on a timer.
  - L4D ends Relax on time or on distance toward the next safe room. FrontRooms could use "rooms travelled toward the exit" once the exit is defined.
  - L4D has no PENDING or ENCOUNTER; it folds them into Build Up. FrontRooms needs explicit entry and exit rules for both.
- **Two Booth ideas transfer directly:**
  - "Pacing, not difficulty": the director changes the frequency of threat, not its strength.
  - The debug HUD (per-player intensity bars plus the state name): a FrontRooms dev overlay could show the tension value, the phase name, the Relay state and the warning stage.
- **Dramatic Anticipation is the design rationale for the three warning stages.** The Tank's footfalls and the Witch's sobbing are audio pre-warnings; the "Infected break through doors" moment matches the door verbs.
- **Once the exit goal exists,** Warframe's population-vs-progress graph is the right tool for budgeting Relay visits across the run.

### Gaps
- The colours of the horizontal and vertical markers are not explained on the slides. Reading them as thresholds is my inference, as is reading the coloured icons as Specials and the large green figure as a boss (they are not labelled).
- No talk video or speaker notes were reviewed; only the slide text and images.
- Neither deck draws the four phases as a state diagram.

---

## Q2. How did Creative Assembly diagram Alien: Isolation's director and alien relationship (menace, back-off, senses)?

### Takeaway
I found no publicly available Creative Assembly diagram. The model is known from Andy Bray's nucl.ai 2016 talk "It's in the Vents" as reported by Tommy Thompson (AI and Games), Thompson's 2020 look at the game's own data, and Alistair Hope's GDC 2015 talk. Its shape:
- A two-layer stack: an omniscient director (macro) and an alien limited to its senses (micro).
- A menace gauge driven by proximity and visibility, not damage. At its peak, the alien withdraws "backstage" into the vents.
- A large behaviour tree whose subtrees unlock as the game goes on.

### Cited Findings
- **Sources:**
  - Thompson's 2017 article draws on Bray's 2016 nucl.ai talk and Bray's answers to Thompson's questions: [Thompson, "The Perfect Organism", Game Developer, 2017-10-31](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
  - The 2020 revisit draws on game data inspected with Matt Filer's OpenCAGE tools: [Thompson, "Revisiting the AI of Alien: Isolation", 2020-05-20](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
  - Hope's talk: [GDC Vault, "Building Fear in Alien: Isolation" (GDC 2015)](https://gdcvault.com/play/1021852/Building-Fear-in-Alien); [free video post](https://www.gamedeveloper.com/audio/video-building-fear-in-i-alien-isolation-i-); [write-up, Parkin, 2015-03-04](https://www.gamedeveloper.com/design/how-creative-assembly-brought-fear-to-i-alien-isolation-i-).
- **Two layers:**
  - The director (macro) always knows where the player is. It periodically sends the alien toward the player's general area but never gives it the exact position.
  - The alien (micro) runs on senses and a behaviour tree and has to find the player itself, so it can be fooled, surprised or escaped.
  - Sources: [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation); [Thompson 2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
- **How the director gives orders:** it works as a utility/job system. Each job has a task, a location and a priority; the priority decides whether the alien finishes its current action, blends into the new one, or drops it at once — [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
- **Menace gauge:**
  - **What raises it:** proximity and tension, not attacks. The inputs are: within a short walk of the player; in the player's line of sight; close on the motion tracker and able to reach the player quickly. Menace rises more slowly when the alien is in another room.
  - **What happens at the peak:** if menace stays at the peak, the director pulls the alien back to neighbouring areas or into the vents.
  - Thompson compares the gauge to L4D's stress measure.
  - Source: [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
  - The rise rate depends on whether the player can see or hear the alien. Designers set cooldown timers, the time it takes to peak, and the number of menaces per frontstage sequence; no values were published — [Thompson 2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
- **Frontstage and backstage:**
  - Frontstage is active: sweeping areas, or searching after a noise or trigger.
  - Backstage is passive: the alien is in the vents, heading for vents ahead of where the player may go.
  - Source: [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
  - Time spent backstage is set by sweep length, distance and idle-time parameters, ambush durations and difficulty. A loud noise can pull the alien out of backstage early. The revisit shows a screenshot of the (invisible) alien's view inside the backstage vents — [Thompson 2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
- **Senses** (the alien has to perceive the player honestly):
  - **Hearing:** walking and running footsteps and gunshots — [2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation). Sounds have strength levels; weapon fire triggers it far faster than movement — [2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
  - **Sight:** four view cones (normal, focussed, peripheral, close). Detection builds the longer the player stays in a cone, and light has little effect on the alien — [2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
  - **Touch:** bumps and damage — [2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
  - **Behind it:** short-range ray checks stop the player sneaking up directly behind it — [2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
  - **Player gear:** the motion tracker's noise carries only a short distance (about 1.5 m per the 2017 article), and the flashlight has a scaled trigger — [2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation); [2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
- **Movement is honest:** per Bray, the alien teleports only twice in a 12–18 hour campaign, both times for cutscenes — [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
- **Behaviour tree size** (the two articles count differently):
  - 2017: over 100 nodes, with about 30 top-level nodes that each select a subtree — [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
  - 2020, from the game data: 36 branches from the root, covering attacks, investigating disturbances, entering and leaving vents and fleeing fire, plus traces of cut content — [Thompson 2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
- **Unlockable behaviours:**
  - Some subtrees start locked and unlock when the player meets conditions; searching lockers and crawling into small vents are examples. This makes the alien seem to learn.
  - Unlocks never come from the player dying, and campaign triggers also unlock behaviours so the alien keeps pace.
  - Source: [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
  - Repeated flamethrower use changes its tactics: hissing, vent ambushes, flanking — [Thompson 2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation).
- **How it searches:**
  - It moves between areas of interest, either hand-tuned by designers or generated dynamically. Designers can also mark areas it must never look at.
  - "Search" points are places it visits; "spot" points are places it stands and looks around.
  - It visits nearby points in a deliberately non-optimal, visibility-weighted order, so its backtracking reads as doubt.
  - Source: [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation).
- **Hope at GDC 2015** — [Parkin 2015](https://www.gamedeveloper.com/design/how-creative-assembly-brought-fear-to-i-alien-isolation-i-):
  - The player is underpowered and the goal is survival, built on simple "stay or go?" choices.
  - The alien is not scripted; it responds to sound and light.
  - A third-person camera was tested and dropped because the alien seemed to hunt the character rather than the player.
  - The motion tracker is presented as limited technology, not a fix-all.

### Inferences
- **Diagram card: two-layer stack (macro/micro)**
  - *Shows:* who knows what.
  - *Grammar:* two horizontal bands. The director band holds full knowledge. A downward arrow carries only a coarse hint (area plus priority). The alien band contains senses → behaviour tree. The player reaches the alien only through sense arrows. A side lane holds "backstage".
  - *Strengths:* makes the fairness promise ("never cheats") visible.
  - *Limits:* says nothing about timing, so pair it with a gauge or timeline.
- **Diagram card: menace gauge**
  - *Grammar:* a meter or line with a threshold. Inputs on the left (proximity, line of sight, tracker range, same room or not); the output at the threshold is "go backstage".
  - *Strengths:* the simplest way to explain why the monster leaves.
  - *Limits:* the real values and cooldowns are not public.
- **Diagram card: frontstage/backstage loop:** a two-node cycle with the conditions written on the arrows (menace at peak → backstage; timer or loud noise → frontstage).
- **Diagram card: unlock ladder:** behaviour-tree branches shown greyed out until their unlock condition is met.
- **FrontRooms mapping:**
  - Away ≈ backstage; Arrive ≈ frontstage search; Withdraw ≈ the director-ordered back-off at peak.
  - The six director phases play the role of menace-gauge management.
  - **Draw an explicit information firewall:** the director may know the player's room but issues only a region-level job; the Relay confirms through honest senses via door and window openings.
  - Like Alien's menace, FrontRooms' tension proxy can be built from Relay proximity, line of sight and the warning stage reached, rather than from damage.
- **The unlock ladder is a cheap way to make the Relay feel adaptive.** Example: unlock "check the window" after repeated window escapes. Sanctuaries should stay outside any unlock, to keep their promise. This is a design suggestion, not a source finding.

### Gaps
- Bray's nucl.ai 2016 slides and video were not found online.
- The phrase "psychopathic serendipity" appears only in secondary coverage; I could not verify it in a primary source.
- Hope's GDC 2015 video was not watched; only the write-up was reviewed.
- Most images in Thompson's articles have only filenames as alt text, so I could not confirm whether any of them draws the two-layer system or the behaviour tree.
- I found no primary source for how the alien behaves around save stations or safe zones.

---

## Q3. How are behaviour trees, FSMs and HTNs drawn in shipped games, and when is each used?

### Takeaway
Sources on shipped games use five recurring forms:
- **Bubble-and-arrow state diagrams** for designer intent: Halo 2's idle/searching/combat/flight sketch, and the textbook guard FSM.
- **Nested HFSMs** with a history marker, to stop the number of states exploding.
- **Priority-ordered trees or DAGs** for the implementation: Halo 2, Alien: Isolation, Hitman.
- **Concurrent behaviour/action stacks:** Left 4 Dead.
- **Task-decomposition hierarchies for HTN planners:** Killzone 2, a High Moon Transformers title.

Halo 2's lead programmer explicitly separates the designer's state sketch from the tree that can actually be built.

### Cited Findings
- **Halo 2 (Isla, GDC 2005):**
  - The AI is an HFSM or behaviour tree that is technically a DAG, because one subtree can sit in several places. The core DAG has roughly 50 behaviours.
  - Non-leaf nodes choose which child runs; leaf nodes do the work.
  - Extra elements: "impulses"; "stimulus behaviours" that events insert into the tree for 1–2 s; behaviour "tags" (a bit-vector including alertness) that lock or unlock parts of the tree.
  - Five decision schemes, with prioritized-list the most common: prioritized-list, sequential, sequential-looping, probabilistic, one-off.
  - Source: [Isla, "Handling Complexity in the Halo 2 AI"](https://www.gamedeveloper.com/programming/gdc-2005-proceeding-handling-complexity-in-the-i-halo-2-i-ai).
- **Isla's two figures:** Figure 1 is a designer's conceptual diagram of player–AI interaction states (idle, searching, combat, flight), which Isla says cannot be implemented directly. Figure 2 is a heavily abbreviated version of the core behaviour DAG — [Isla 2005](https://www.gamedeveloper.com/programming/gdc-2005-proceeding-handling-complexity-in-the-i-halo-2-i-ai).
- **Overview chapter (Dawe, Gargolinski, Dicken, Humphreys, Mark), "Behavior Selection Algorithms: An Overview":** — [Game AI Pro ch. 4](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter04_Behavior_Selection_Algorithms.pdf)
  - **FSMs** are the most common technique. They are easy to sketch and visualise, which helps when talking to less technical team members.
    - Fig. 4.1: a guard FSM. Patrol → Investigate on a noise → back to Patrol; Attack on seeing an enemy.
    - Figs. 4.2–4.3: a night watchman needs duplicate Conversation states, the "state explosion" problem.
    - Fig. 4.4: an HFSM fixes this by nesting the patrol states inside a "Watch Building" machine, with a circled H marking the history state.
  - **Behaviour trees:** a tree of behaviours starting from a root. Behaviours don't know about each other, so the tree is easy to extend. Because selection runs from the root each time, it costs more per decision than an FSM.
  - **Utility systems** (The Sims as the example): adaptable but less predictable; specific moments need scripted overrides.
  - **GOAP:** pioneered by Jeff Orkin for F.E.A.R. Derived from STRIPS and searches backward from the goal.
  - **HTN:** used in Guerrilla's Killzone 2 and a High Moon Studios Transformers title. It is a forward planner: a hierarchy of compound tasks and methods breaks down into primitive tasks. Designers can read these domains, but the planner cannot invent plans the designer did not author.
- **Further primary reading (titles verified, not reviewed in depth):**
  - Orkin's talk page: [Orkin, "Three States and a Plan: The AI of F.E.A.R." (GDC Vault)](https://www.gdcvault.com/play/1013282/Three-States-and-a-Plan). The original MIT-hosted PDF now returns 404; a third-party mirror is at [gamedevs.org](https://www.gamedevs.org/uploads/three-states-plan-ai-of-fear.pdf).
  - [Humphreys, "Exploring HTN Planners through Example", Game AI Pro ch. 12](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter12_Exploring_HTN_Planners_through_Example.pdf).
  - [Champandard & Dunstan, "The Behavior Tree Starter Kit", Game AI Pro ch. 6](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter06_The_Behavior_Tree_Starter_Kit.pdf).
  - A hybrid of behaviour trees and state machines in Final Fantasy XV: [Miyake et al., Game AI Pro 3 ch. 11](http://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter11_A_Character_Decision-Making_System_for_FINAL_FANTASY_XV_by_Combining_Behavior_Trees_and_State_Machines.pdf).
- **Left 4 Dead:**
  - Each Action has OnStart, Update, OnEnd, OnSuspend and OnResume.
  - An Action changes state only by returning a transition result, so transitions are atomic, and each transition records a reason string for debugging.
  - Events pass from the innermost Action, to buried Actions, to the parent.
  - Source: [Booth AIIDE slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf).
- **Hitman (2016):**
  - Each NPC has a behaviour tree fed by goals and a knowledge base.
  - The knowledge base is filled by local sensors and by shared "services" (Disguise, Dead Body, Hitman) that update only periodically, which leaves room for exploits.
  - "Situations" coordinate groups: a body is found, the player is compromised, a target flees.
  - Crowd agents use a three-state FSM: idle, walking, pending walk.
  - Source: [Thompson, "The AI of Hitman (2016)", 2019](https://www.gamedeveloper.com/design/the-ai-of-hitman-2016-).

### Inferences
- **Designer state diagram**
  - *Grammar:* bubbles for states named in player-facing terms; arrows labelled with triggers the player can perceive.
  - *Used for:* pitches, decks, tuning discussions.
  - *Strengths:* readable, and a quick test of "can the player see why it changed?"
  - *Limits:* not implementable as drawn (Isla), and it explodes when behaviours are reused.
- **HFSM**
  - *Grammar:* boxes nested inside boxes, with an H history marker.
  - *Strengths:* shows phases containing states.
  - *Limits:* hides priorities and interrupts.
- **Behaviour tree / DAG**
  - *Grammar:* root at the top or left; children in priority order; separate glyphs for selector, sequence, condition and action.
  - *Strengths:* maps directly to code; shows locked or unlocked branches.
  - *Limits:* no time axis, so it can't show pacing; unreadable at full size (Isla abbreviates his).
- **HTN**
  - *Grammar:* a decomposition tree where each method has a condition.
  - *Strengths:* designers can read the plans.
  - *Limits:* high authoring cost and no emergent plans.
- **For FrontRooms:**
  - The Relay is already an FSM, so the natural form is an HFSM. Make the six director phases the outer layer and nest Away/Arrive/Withdraw inside, with "heard door", "lost track", "sanctuary entered" and "timer" as transition labels.
  - Keep two separate drawings, as Isla does: a designer-level diagram for the deck, and an implementation diagram (states plus priorities and interrupts) for the code.

### Gaps
- The HTN, Behaviour Tree Starter Kit and Final Fantasy XV chapters were not read beyond their titles.
- Guerrilla's Killzone 2 HTN talk was not reviewed.
- Orkin's paper is not at its original URL (404); only the GDC Vault page and a third-party mirror are linked.

---

## Q4. How are stealth awareness and detection states usually diagrammed (state machines with alert levels, perception cones)?

### Takeaway
Three forms recur:
1. **Perception geometry on a plan or side view:** view cones, "coffin" boxes, a sound radius tested against a real path.
2. **An awareness ladder or meter:** discrete states, or a filling timer with an intermediate "investigate" state, mirrored by a HUD element and spoken lines (barks).
3. **A spatial-reasoning overlay for search and chase:** area graphs whose connections are door and window choke nodes, and influence-map floods drawn step by step.

The developers rank fairness, consistency and feedback above realism.

### Cited Findings

**Thief: The Dark Project (Tom Leonard, GDC, published 2003-03-07)** — [Leonard, "Building an AI Sensory System"](https://www.gamedeveloper.com/programming/building-an-ai-sensory-system-examining-the-design-of-i-thief-the-dark-project-i-)
- **Awareness:** a set of discrete levels of certainty about an object's presence, location and identity. Higher-level AI turns awareness into an overall alertness state, and alertness feeds back into the senses.
- **Rising:** each increase passes a time filter whose delay depends on the current level. Once past it, awareness jumps straight to the goal level.
- **Falling:** decreases drain gradually through every intermediate level, like a capacitor.
- **"Sense links"** are the AI's main memory. Guards can pass them to each other by speaking or by watching each other.
- **Vision:**
  - Ordered 3D view cones (angles, length, acuity, motion and light sensitivity), aimed by head direction. Only the first cone containing the object counts, and each cone gives a constant output.
  - Visibility is a 0–1 value built from lighting, movement and exposure.
  - A backward-facing, motion-sensitive cone gives guards a sense of being followed.
- **Figures:** Fig. 1 shows view distance, view cone, line of sight and eye position (in a Half-Life comparison). Fig. 4 shows one AI with five view cones and test points A–C.
- **Design rule:** an AI should not see the player before the player could see it, or the player feels cheated.
- **Player-facing layer** — [Wikipedia, Thief: The Dark Project](https://en.wikipedia.org/wiki/Thief:_The_Dark_Project):
  - A light gem on the HUD shows how visible the player is.
  - Guards ignore brief suspicion; if it persists they become alert and search.
  - Footsteps are loud on tile and metal and quiet on carpet and grass.
  - A per-mission room database models how sound travels.

**Splinter Cell: Blacklist (Martin Walsh)** — [Walsh, Game AI Pro 2 ch. 28](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter28_Modeling_Perception_and_Awareness_in_Tom_Clancy%27s_Splinter_Cell_Blacklist.pdf); talk page [GDC Vault, "Modeling AI Perception and Awareness"](https://www.gdcvault.com/play/1020436/Modeling-AI-Perception-and-Awareness)
- **Four goals:** fairness, consistency, good feedback, intelligence.
- **Awareness model:** awareness is a set of discrete mental states reached progressively. Many games reduce it to one progress bar with two binary outcomes; Splinter Cell adds a middle "investigate" state at some percentage of the bar.
- **Detection:**
  - Fig. 28.1: rays are cast to 8 bones on the player; the stance sets how many must be visible.
  - Then a timer starts. It is scaled by distance, how lit the player is, and the NPC's state.
  - Fig. 28.2 shows detection starting once enough bones are visible. The text explains that the timer is shown to the player as a growing HUD element: the player's window to break line of sight. When it fills, the player is detected.
- **Vision shapes:**
  - Conviction replaced cones with boxes; Blacklist used "coffin-shaped" boxes that widen then narrow with distance.
  - A standard cone in front still gives fast detection, and the widest shape the player is in sets the timer range (Fig. 28.3, a debug drawing).
  - Any shape has a hard edge, outside which the player is never seen.
- **TEAS (Tactical Environment Awareness System), Fig. 28.4:**
  - Areas (sub-navmeshes) are linked by choke nodes; for example, two rooms joined by a door and a window get two choke nodes.
  - Each area stores metadata: its size, how many NPCs should enter for search or combat, and its type.
- **Hearing and fairness:**
  - Each sound event has a radius and a priority, and distance is measured along an "area path".
  - Being heard by a guard you can't see feels unfair. Design wanted a minimal HUD, so the fix was three tiers of barks — specific, then generic, then fully generic — plus hearing distances limited by plausibility.
  - Walsh's principle: player perception should win, within the bounds of plausibility.

**Mark of the Ninja (Klei)**
- **Vision and hearing** — [Miles, "How to Catch a Ninja", Game AI Pro ch. 32](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter32_How_to_Catch_a_Ninja_NPC_Awareness_in_a_2D_Stealth_Platformer.pdf):
  - A vision cone is defined by offset, direction, angle and maximum distance. Other shapes exist too: a single ray, a circle, a square, a trapezoid.
  - A guard hears a sound only if A* finds a path through the "sound mesh" (a triangulation of the level's empty space) from the source to the guard's head.
  - Fig. 32.1 shows one running noise and three guards:
    - Guard A is inside the radius and hears it.
    - Guard B is inside the radius but has no path, so does not hear it.
    - Guard C is outside the radius, but would see the player if the player entered its cone.
- **Interest sources** — [Miles](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter32_How_to_Catch_a_Ninja_NPC_Awareness_in_a_2D_Stealth_Platformer.pdf):
  - Defined in data with priorities (Listing 32.2 is a priority table). Each guard holds one interest at a time, with no queue.
  - When several guards notice the same thing, they get roles: sentry/leader, investigator, bystander.
- **Anderson's five stealth rules** — [Kumar, "Mark of the Ninja's five stealth design rules", 2012-12-03](https://www.gamedeveloper.com/design/-i-mark-of-the-ninja-i-s-five-stealth-design-rules):
  1. A transparent stealth system: light is binary.
  2. Transparent AI: three awareness levels, each with visible cues.
  3. A narrow gulf between intent and execution.
  4. Limited cost of failure: a checkpoint between meaningful encounters.
  5. Less open worlds.
  - Talk page: [GDC Vault, Anderson](https://gdcvault.com/play/1017791/Of-Choice-and-Breaking-New).

**Dishonored 2 (Arkane)**
- **States and give-up** — [Couvidou, "Flooding the Influence Map for Chase in Dishonored 2", Game AI Pro Online 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter06_Flooding_the_Influence_Map_for_Chase_in_Dishonored_2.pdf):
  - The combat AI has many states; the chapter uses two: Engage (direct perception) and Chase (target lost).
  - A failed chase ends with an explicit give-up — the NPC looks around and barks its disappointment — and then a slow-paced search.
- **The chase algorithm** — [Couvidou 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter06_Flooding_the_Influence_Map_for_Chase_in_Dishonored_2.pdf):
  - Breadcrumbs were rejected because they amount to omniscience and feel unfair when noticed. Dead reckoning was rejected because it stops at walls.
  - Instead, an influence map is flooded from the last known position: a "heat" seed plus a "barrier" that blocks the half-plane behind the player's direction of travel.
  - The chase target is the weighted centroid of the warm cells. The flood stops early in wide areas (switch to search) and runs longer in corridors.
  - Two constants tune it. Table 1 lists 20 for the maximum heated cells per step and 25 for the maximum steps; the step count doubled as a difficulty setting.
  - Figs. 1–7 are grid diagrams. Fig. 1 shows dead reckoning failing. Figs. 2a–3 show seeding and propagation, with cell temperature values (seed H = 9). Fig. 4 compares the flood with dead reckoning. Figs. 5–7 show the results: the tilt toward the likely position, early stops in wide areas, and the barrier hiding an exit.
- **The GDC 2017 talk** covers NPC coordination, spatial reasoning for stealth and search, and data-driven rules for dialogue and animation — [GDC Vault, Couvidou & Sadoulet, "Taking Back What's Ours: The AI of Dishonored 2"](https://www.gdcvault.com/play/1024660/Taking-Back-What-s-Ours); [video post](https://www.gamedeveloper.com/design/video-inside-the-ai-design-of-i-dishonored-2-i-).

**Hitman (2016):** knowledge bases, shared services and group "situations" drive reactions such as discovering a body or the player being compromised — [Thompson 2019](https://www.gamedeveloper.com/design/the-ai-of-hitman-2016-).

### Inferences
- **Perception plan view**
  - *Grammar:* a top-down plan (or a side view for height) with cones or boxes, a sound radius, and blocked or open paths.
  - *Used for:* tuning, level reviews, explaining why a guard did or didn't notice.
  - *Strengths:* spatially exact.
  - *Limits:* no sense of time.
  - *For FrontRooms:* a plan view of a room cluster in which hearing reaches the Relay only through open doors and windows is the clearest drawing of "honest senses". Mark of the Ninja's path test and Blacklist's door/window choke nodes are direct precedents.
- **Awareness ladder or meter**
  - *Grammar:* a vertical ladder or horizontal bar. States are rungs, each with its rise and decay rule (Thief: fast rise after a delay, slow stepwise decay). A parallel column lists the player-facing cue (HUD, bark, animation).
  - *Strengths:* makes the fairness window visible.
  - *Limits:* says nothing about space.
  - *For FrontRooms:* rungs are the Relay's knowledge (Away: none → Arrive: heard → locating → lock-on); the cue column is lamps → steps → lock-on chime. Mark of the Ninja's rule 2 and Blacklist's state-to-HUD matching suggest a hard rule: the Relay never moves up a rung without a cue.
- **Search flood sequence**
  - *Grammar:* small multiples of a grid or graph carrying heat values, with walls drawn thick.
  - *Strengths:* explains what the AI does after losing the player.
  - *For FrontRooms:* run the same flood over the room graph (rooms as nodes, doors and windows as edges) to choose where the Relay arrives next or how it withdraws. This mirrors Dishonored 2's "give up when it makes sense".
- **Tables and matrices:** Mark of the Ninja's priority table, Blacklist's bark tiers and Dishonored 2's tuning-constant table show that the numbers that tune a system are usually given as tables, not diagrams. For FrontRooms: a matrix of Relay state × warning stage × the player's options (shut door, use window, hide, reach sanctuary).

### Gaps
- I found no primary source naming the exact alert levels of Thief's Dark Engine, the original Dishonored, or Hitman.
- Metal Gear Solid's alert phases were not researched.
- An interview quote attributed to Arkane's Colantonio on hidden-state ambiguity (via secondary coverage) could not be verified.
- These Game AI Pro chapters were listed but not reviewed:
  - [Rabin, "Vision Zones and Object Identification Certainty"](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter04_Vision_Zones_and_Object_Identification_Certainty.pdf)
  - [Welsh, "Looking for Trouble: Making NPCs Search Realistically"](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter27_Looking_for_Trouble_Making_NPCs_Search_Realistically.pdf)
  - [Botta, "Infected AI in The Last of Us"](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter33_Infected_AI_in_The_Last_of_Us.pdf) — a horror monster that hunts by sound.

---

## Q5. How do Frictional (Amnesia, SOMA) and Arkane (Dishonored) present their systems? Are there developer blogs or talks with system maps?

### Takeaway
- **Frictional** publishes design philosophy rather than system maps: the SSM framework diagram (System / Story / Mental Model), essays on horror lessons (keep systems vague, space out scares, give agency), and monster-design diaries.
- **Arkane's** public material is AI engineering: the GDC 2017 talk, and a Game AI Pro chapter with step-by-step influence-map diagrams.
- **IOI's** Hitman AI is known mostly through talks summarised by AI and Games.
- I found no published one-page map of the threat loop for Amnesia, SOMA, Dishonored or Hitman.

### Cited Findings
- **The SSM framework (Thomas Grip, 2017-05-23)** — [Grip, "The SSM Framework of Game Design"](https://frictionalgames.com/2017-05-the-ssm-framework-of-game-design/):
  - It divides a game into three spaces:
    - System: mechanics and dynamics.
    - Story: mise-en-scène (what the player perceives) and drama.
    - Mental Model: affordances and schemas.
  - The post has a combined diagram of the three spaces and a 10-step loop: input → System → Story → senses → Mental Model → input.
  - Grip notes that entities start in story space and move into system space as players learn them. The mental model also fills with entities the player imagines.
- **"9 Years, 9 Lessons on Horror" (Grip, 2019-10-15)** — [Grip](https://frictionalgames.com/2019-10-9-years-9-lessons-on-horror/). The lessons:
  1. Horror is not enjoyable.
  2. Players work against you.
  3. Scares alone don't make a horror game; spacing them out made players more scared.
  4. Fun gameplay competes with fear.
  5. Narrative is core.
  6. The world must feel real.
  7. Keep it vague: an uncertain system is scarier.
  8. Players need a role.
  9. Agency is crucial.
  - He also notes that Amnesia's late-game monster encounters were not supported by anything else in the game.
- **SOMA's monsters (interview, 2015-10-27)** — [Graft, "The tricky design problem of SOMA's memorable monsters"](https://www.gamedeveloper.com/design/the-tricky-design-problem-of-i-soma-i-s-memorable-monsters):
  - Monsters should relate to the theme.
  - Ambiguous hostility failed in testing, so the monsters became clearly docile or clearly aggressive.
  - Their rules have to be strict yet fuzzy: too mechanical feels "systemic", too random becomes a haze players can't read.
  - Grip's key test is whether the player can build a mental model of them.
- **Other Frictional sources:**
  - A 2011 blog series documents the original Amnesia monster's design: [Frictional, "Birth of a Monster Part 1"](https://frictionalgames.com/2011-03-birth-of-a-monster-part-1/).
  - The blog's design category lists essays on planning, SSM, defenceless protagonists and Five Nights at Freddy's: [Frictional design category](https://frictionalgames.com/category/feed/blog/design/).
- **Amnesia: The Bunker's systems** — [Wikipedia, Amnesia: The Bunker](https://en.wikipedia.org/wiki/Amnesia:_The_Bunker):
  - A "photosensitive" Beast.
  - Generator fuel keeps the lights on; when it runs out, the bunker goes dark and the Beast actively hunts.
  - A noisy dynamo flashlight; fire scares the Beast.
  - Safe rooms appear regularly; they are where you save, and they give access to a storage chest and the map.
  - A semi-open world, randomised resources, no sanity mechanic.
- **Arkane:** the GDC 2017 AI Summit talk — [GDC Vault](https://www.gdcvault.com/play/1024660/Taking-Back-What-s-Ours); [video post](https://www.gamedeveloper.com/design/video-inside-the-ai-design-of-i-dishonored-2-i-) — and the chase chapter with its seven-step grid figures — [Couvidou 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter06_Flooding_the_Influence_Map_for_Chase_in_Dishonored_2.pdf). Note the readability step: the explicit give-up bark before NPCs search.
- **IOI talks summarised by AI and Games** — [Thompson 2019](https://www.gamedeveloper.com/design/the-ai-of-hitman-2016-):
  - Vehkala, GDC Europe 2013.
  - Fauerby and Büttner, Game/AI Conference 2012.
  - Schroder and Petersen, nucl.ai 2016.

### Inferences
- **SSM as a layered-stack form for the FrontRooms deck:**
  - System: the Relay's phases, states and senses.
  - Story (mise-en-scène): the lamps, steps and lock-on chime, plus the room fiction.
  - Mental Model: what the player believes the Relay can do.
- **The central tension this exposes:** Frictional's "keep it vague" vs. Mark of the Ninja and Splinter Cell's transparency. SOMA's "strict yet fuzzy" is the middle path. On that reading, FrontRooms' three warning stages are a graded legibility ramp:
  - The team diagrams must be exact, because honest senses are a rule the team keeps.
  - The player-facing cues can stay uncertain until the lock-on chime.
- **The Bunker's chain (generator → darkness → active hunting)** is a resource-to-pacing lever. It is the closest published horror analogue to a sanctuary or lamp system feeding the director.

### Gaps
- No Frictional, Arkane or IOI one-page system map of a threat loop was found.
- The "Birth of a Monster" series was not read in full; part 1's existence and topic are confirmed only through Frictional's blog listing.
- The Bunker's developer-diary videos and Grip's SSM images were not reviewed; the contents of the SSM diagram are known only from the text.
- **Outlast:** I found no Red Barrels design talk or postmortem, only interviews and reviews, so I made no claims about its systems.
- **The original Dishonored:** I found no primary source on its awareness design.
- **SOMA:** I found no system diagram of its monster AI.

---

## Q6. What is typically on a horror (or stealth) game's one-page systems overview, and how is it drawn?

### Takeaway
I found no published one-page systems overview from a horror studio. The nearest primary sources are Stone Librande's one-page design method (GDC 2010) and the way the sources above group their systems. One cluster recurs:
- threat AI (a director plus an agent);
- senses;
- player verbs;
- warnings and feedback;
- space (routes, choke points, hiding places, safe rooms);
- a pacing curve;
- failure and save rules;
- tuning numbers.

Each item has a usual diagram form.

### Cited Findings
- **Librande's one-page designs (GDC 2010)** — [GDC Vault, Librande, "One-Page Designs"](https://gdcvault.com/play/1012356/One-Page); [Riendeau, Game Developer, 2026-07-16](https://www.gamedeveloper.com/design/-the-goal-of-design-is-to-efficiently-communicate-ideas-):
  - Replace thick design bibles and wikis with one annotated page.
  - Borrow forms from architectural blueprints, engineering schematics, timeline graphics and children's placemats.
  - Leave white space for notes.
  - The talk's thesis is that design means communicating ideas efficiently. Examples Riendeau names include a Blizzard prototype, The Simpsons Hit and Run's paper prototype and Spore's systems; search-result summaries also mention Diablo 3 and SimCity.
  - A slide upload titled "One Page Designs" is on SlideShare; the uploader was not verified: [SlideShare](https://www.slideshare.net/slideshow/one-pagedesigns/13460886).
- **Systems the sources document together:**
  - **Left 4 Dead:** four AI goals; dramatic anticipation; structured unpredictability; adaptive pacing bounded by safe rooms; procedurally populated environments — [AIIDE slides](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf); [GDC 2009 deck](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf).
  - **Alien: Isolation:** director/alien split, menace gauge, senses, behaviour-tree unlocks, search, vents, motion tracker — [Thompson 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation); [Thompson 2020](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation); [Parkin 2015](https://www.gamedeveloper.com/design/how-creative-assembly-brought-fear-to-i-alien-isolation-i-).
  - **Splinter Cell: Blacklist:** visual, auditory, environmental and social awareness, judged by fairness, consistency, feedback and intelligence — [Walsh](http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter28_Modeling_Perception_and_Awareness_in_Tom_Clancy%27s_Splinter_Cell_Blacklist.pdf).
  - **Mark of the Ninja:** the five rules, including checkpoints between encounters — [Kumar 2012](https://www.gamedeveloper.com/design/-i-mark-of-the-ninja-i-s-five-stealth-design-rules).
  - **Randy Smith's stealth level-building talk:** goals and vocabulary, techniques, pitfalls, analytical tools. It is about levels that look securely guarded while still letting the player be stealthy — [GDC Vault, Smith, "Level Building for Stealth Gameplay"](https://gdcvault.com/play/1013435/Level-Building-for-Stealth).
  - **Frictional:** System/Story/Mental-model layering and the horror lessons — [Grip SSM](https://frictionalgames.com/2017-05-the-ssm-framework-of-game-design/); [Grip 2019](https://frictionalgames.com/2019-10-9-years-9-lessons-on-horror/).
  - **Amnesia: The Bunker:** safe rooms with save, stash and map; generator fuel; randomisation — [Wikipedia](https://en.wikipedia.org/wiki/Amnesia:_The_Bunker).
  - **Warframe:** an area graph, a distance-to-objective map, intensity, and a population-vs-progress curve — [Brewer 2021](http://www.gameaipro.com/GameAIProOnlineEdition2021/GameAIProOnlineEdition2021_Chapter07_Managing_Pacing_in_Procedural_Levels_in_Warframe.pdf).

### Inferences
- **Content → diagram form → precedent → FrontRooms use:**

| Content on the page | Usual form (grammar) | Precedent | FrontRooms use |
|---|---|---|---|
| Pacing / director phases | Stacked timeline: phase bands, intensity line, entity strip, vertical markers | L4D slides 84–91; Warframe Fig. 4 | 6 phases × Relay state × warning stage × player context, plus a planned-vs-actual lane |
| Who knows what | Two-layer stack with a one-way "hint" arrow and an information firewall | Alien: Isolation (Thompson) | Director knows the room → region-only job → Relay's honest senses |
| Threat logic | Designer state diagram, then an HFSM; behaviour tree only for implementation | Halo 2 Fig. 1 vs. Fig. 2; Game AI Pro Figs. 4.1–4.4 | Phases as the outer machine; Away/Arrive/Withdraw nested inside |
| Senses | Plan or side view: cones/boxes, radius, path-tested sound | Thief Fig. 4; MotN Fig. 32.1; SC Fig. 28.3 | Hearing through door and window openings only |
| Warnings / feedback | Ladder or meter with a parallel cue column; bark-tier table | SC detection HUD; MotN rule 2; Thief light gem | Away → Arrive → lock-on ↔ lamps → steps → chime |
| Space | Room/area graph with choke edges and node types | SC TEAS Fig. 28.4; Warframe TacMap Fig. 3; L4D Active Area Set | Rooms as nodes; doors and windows as edges; trigger rooms, sanctuaries, exit (TBD) |
| Search after loss | Small-multiple flood frames | Dishonored 2 Figs. 2–7 | Where the Relay re-arrives or withdraws |
| Progress / goal | Population-vs-progress curve | Warframe Fig. 5 | Relay visit budget vs. distance to the exit, once the exit is defined |
| Tuning | Tables | D2 Table 1; MotN Listing 32.2 | Cooldowns, time-to-peak, durations, hearing ranges |
| Player's belief | Layered stack (System / Story / Mental Model) | Frictional SSM | What the player is meant to infer, and when |

- **A suggested FrontRooms one-pager**, following Librande's "strong central image":
  - Centre: the room graph or floor plan, with door and window edges, trigger rooms, sanctuaries, and a placeholder for the exit.
  - Top band: the phase timeline.
  - Left: the director ↔ Relay stack.
  - Right: the warning ladder next to a player-verb column (shut door, open window, hide, enter sanctuary).
  - Bottom: the tuning table, plus the failure and consequence rule.
- **What this page must say that the sources show is easy to miss:**
  - when the threat is not allowed to act (L4D's Relax, Alien's backstage, sanctuaries);
  - what the player can perceive before each escalation (Splinter Cell's fairness goal, Mark of the Ninja's rule 2, Thief's "see before seen" rule).

### Gaps
- No primary one-page overview from a shipped horror game was found; the Amnesia, SOMA, Alien: Isolation and Outlast design documents are not public.
- Librande's slides were seen only through write-ups, not page by page.
- The list of overview components above is a synthesis across sources, not a documented industry template.
