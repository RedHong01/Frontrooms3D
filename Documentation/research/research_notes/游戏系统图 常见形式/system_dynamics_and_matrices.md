# System dynamics and interaction diagrams in game design: Machinations, causal and feedback loops, chemistry-engine rules, immersive-sim stim/response, verb × object matrices

Researched 2026-10-15. Source tags: **[F]** = I opened and read the page or PDF; **[S]** = taken only from a search-engine result summary or snippet (I did not open the page); **[L]** = the link appears on a page I read, but I did not open it. Nothing was downloaded into the project; all items are links.

---

## 1. Machinations: node types and notation, what real diagrams look like, strengths and limits, uses beyond economies

### Takeaway
Machinations is a formal resource-flow notation that a computer can run. Joris Dormans designed it (papers 2008–2011). It was codified in Adams & Dormans, *Game Mechanics: Advanced Game Design* (2012), and is now sold as a commercial tool at machinations.io. A diagram is built from pools, sources, drains, converters, traders and gates. Solid resource connections link them, and dotted state connections (label modifiers, node modifiers, triggers, activators) change them, with markers that show how each node activates. It is best at making feedback loops visible and testing them by simulation: artificial players, charts and repeated quick runs. Its authors limit it to single-player, abstract, whole-number models. They also say plainly that it does not replace studying or playtesting the real game. Dormans also used it outside economies: for mission and lock-and-key progression, and for abstract quantities such as "threat level" or "player skill". I found no published Machinations model of a horror game or an AI director.

### Cited Findings
**What it is and why it exists**
- Machinations "was devised by Joris Dormans to help designers and students of game design create, document, simulate, and test the internal economy of a game". The book names its main advantage as "a clearly defined syntax" — [Adams & Dormans, *Game Mechanics* ch. 5 "Machinations" sample, p. 79 ff. (Pearson PDF)](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf) [F]
- The theory behind it: gameplay is "ultimately determined by the flow of tangible, intangible, and abstract resources", and the diagrams let you "see and study the feedback structures". The syntax is "formal enough to be interpreted and executed on a computer; it is close to a visual programming language" — [same PDF](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf) [F]
- Why the authors did not use existing models: code, finite state diagrams and Petri nets "are complex and not really accessible for designers". They also sit at the wrong level of abstraction for features like feedback loops to be "immediately apparent" — [same PDF](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf) [F]. Dormans contrasts it with Petri nets directly. In Machinations, the current distribution of resources *is* the game state, and that distribution can change later flows or turn nodes on and off — [Dormans, "Simulating Mechanics to Study Emergence in Games", AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]
- Tools: the original free tool was at www.jorisdormans.nl/machinations, and the book's tutorial appendix is online — [O'Reilly, Online Appendix C "Getting Started with Machinations"](https://www.oreilly.com/library/view/game-mechanics-advanced/9780132946728/app05.html) [S]. The current commercial tool is [machinations.io/docs](https://machinations.io/docs/) [F: navigation only, because the docs pages load their content with JavaScript].
- The 2009 founding paper: Dormans's own 2011 papers cite it as "Machinations: Elemental feedback structures for game design", Proceedings of GAMEON-NA 2009 — [AAAI 2011 reference list](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]; [DiGRA 2011 reference list](https://dl.digra.org/index.php/dl/article/download/593/593) [F]. A 2014 reference list gives the title as "...Elemental Feedback **Patterns**..." [S]. I did not find the full text. Dormans's PhD thesis *Engineering Emergence* (2012) is at [ILLC eprints](https://eprints.illc.uva.nl/id/eprint/2118/) [S] (the PDF is over 10 MB and could not be fetched).

**Visual grammar (book ch. 5, unless another source is given)** [F, same Pearson PDF]
- **Pool**: an open circle. Resources are small coloured circles stacked on it; above a display limit (default 25) the pool shows a number instead. Pools hold whole numbers only. Different colours are different resource types; one pool can hold several, but the book advises against it at first.
- **Resource connection**: a solid arrow with a flow-rate label (no label means 1; "all" means unlimited). Random rates are written as "D" (an unspecified random factor), dice notation (D6+3, 2D6, 2D4+D8+D12) or percentages (25%; 250% means two plus a 50% chance of a third).
- **Activation modes**: *automatic* (marked with an asterisk, fires every step), *interactive* (double outline, a player action), *starting action* (marked "s", fires once before the first step) and *passive* (no mark, fires only when triggered).
- **Pull and push**: pull is the default. Push mode is marked **p**, and all-or-nothing mode is marked **&**. **Time modes**: synchronous, asynchronous (the tool's default), and turn-based with action points.
- **State connections** are dotted arrows and come in four kinds:
  - *label modifiers* (labels like +2 or −1): new label = old label + Σ(M × ΔS);
  - *node modifiers*: change the number of resources in the target node; fractions such as +1/3 are allowed;
  - *triggers* (label `*`): fire the target when all of the origin's inputs are satisfied;
  - *activators* (conditions such as `==0`, `<3`, `>=4`, `!=2`, `3-6`): enable or block the target.
- **Gate**: a diamond that passes resources on immediately. Its outputs are either *probable* (percentages or weights) or *conditional*. A deterministic gate is a plain open diamond (a "counting gate"); a random gate carries a die symbol. Every state connection that leaves a gate is a trigger.
- **Source**: a triangle pointing up. **Drain**: a triangle pointing down. **Converter**: a triangle pointing right with a vertical line through it; it "act[s] exactly as a drain that triggers a source" (e.g. a sawmill turning 1 tree into 50 boards). A "limited converter" is a drain plus a limited source.
- **Trader**: drawn as "a vertical line over two triangles". It changes who owns resources (Settlers of Catan trading) and is "relatively rare"; most RPG merchants are really converters — [AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]
- **Automated elements** (AAAI 2011):
  - *End conditions* are switched on by an activator. They are used for goals and timers, and repeated quick runs count how often each end condition was reached.
  - *Charts* plot pool values over time.
  - *Artificial players* follow simple scripts (e.g. `invest=100-upgrades*30`, `run=100`). They "do not have very advanced artificial intelligence" but "will happily do so over thousands of runs" — [AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]
- The current machinations.io docs have pages for [End Conditions](https://docs.machinations.io/end-conditions) (which says end conditions "only take state connections as input") and [Artificial Player](https://docs.machinations.io/artificial-player) [S; both redirect to machinations.io/docs/… and render client-side].
- **Abstract resources are allowed.** A margin note says node modifiers can create and destroy resources: "This is all right for an abstract resource such as 'threat level' but is best avoided for tangible and intangible resources such as 'keys' or 'health'" — [Pearson PDF, ch. 5 "Node Modifiers"](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf) [F]. Even more abstract aspects, "such as player skill level and strategic position can be modeled through the use of resources" — [AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]

**Real game diagrams (figures are inside the linked PDFs)**
- Book ch. 5 [F]:
  - Monopoly: a label modifier from property to the chance that other players' money flows to you (Fig. 5.12), and a trigger where spending money acquires property (Fig. 5.14).
  - Settlers of Catan: node modifiers (villages give 1 point, cities 2).
  - Caylus: an activator for a labourer at the gold mine (Fig. 5.15).
  - Risk: a probable gate for attack risk, and an interactive source for building armies.
  - Star Wars: X-Wing Alliance: shield regeneration as an automatic source.
  - An hourglass made of two pools (Fig. 5.9).
  - [Pearson PDF](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf)
- SimWar is a game Will Wright described but never built. Dormans diagrammed it in full (Fig. 6), charted each player's strength over time as "the fingerprint of an interesting match" (Fig. 7), and played turtling against rushing artificial players. He tabulated win counts against factory-cost tweaks — [AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]
- **Zelda: Twilight Princess, Forest Temple** mission as a Machinations diagram (Fig. 1). The player's location is a black token moved by clicking adjacent interactive pools. Collected items (eight monkeys, the master key, the Gale Boomerang) unlock new locations. The paper then adds feedback to lock-and-key in three variants: dynamic friction (keys drain away), the dynamic engine pattern (invest keys to raise the harvest rate), and dynamic engine plus dynamic friction (enemies eat keys, giving RTS-like balancing). It also shows the "escalating complications" pattern (Pac-Man, Space Invaders) and "escalating complexity" (Tetris) — [Dormans, "Integrating Emergence and Progression", DiGRA 2011](https://dl.digra.org/index.php/dl/article/view/593) ([PDF](https://dl.digra.org/index.php/dl/article/download/593/593)) [F]. The interactive versions were hosted on the old jorisdormans.nl Machinations wiki ("Mission_Mechanics" page) [L, legacy].
- Pattern claims: a game's internal economy "exhibits emergent behavior when somewhere between two and four feedback loops operate" (Dormans 2009, cited in DiGRA 2011). "Single feedback systems almost always suffer of dominant strategies" — [DiGRA 2011 PDF](https://dl.digra.org/index.php/dl/article/download/593/593) [F]
- Current commercial examples:
  - A Monte-Carlo model of army power for different player personas over a 2-hour session (1 gameplay minute = 1 step) — [machinations.io article](https://machinations.io/articles/balancing-f2p-economies-simulating-player-personas-and-progression-curves-with-machinations) [S]
  - Difficulty and score balancing for the hyper-casual game Stack Jump — [docs](https://machinations.io/docs/balancing-difficulty-score) [S]
  - A community "Systemic Combat" diagram — [machinations.io community](https://machinations.io/community/neoleegdg/systemic-combat-38f777a6b42a11eda2330626ff1c9bc8) [S]
- Link to system dynamics: machinations.io keeps comparison pages against the system-dynamics tools [Vensim](https://machinations.io/vensim-vs-machinations) and [Stella Architect](https://machinations.io/stella-architect-vs-machinations) [L: links in the site footer]

**Strengths and limits as stated by the authors** [F unless marked]
- Scope: "the natural scope of a Machinations diagram is that of a single player". There is no support for several players on several input devices, and "It's a simulation tool, not a tool for building playable games". The book's diagrams are deliberately simplified and "aren't a substitute for studying the game itself". A printed diagram shows only one distribution of resources. The framework is "a lot to take in at once" — [Pearson PDF](https://ptgmedia.pearsoncmg.com/imprint_downloads/peachpit/peachpit/samplechapters/0321820274/0321820274_gamemechanics_ch05section.pdf)
- Simulation can produce play data before a prototype exists, but "It is naive to assume that the tweaks suggested in this paper translate to a perfectly balanced game". Levels and player skill must be considered as well — [AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf)
- Lock-and-key mechanics "do not involve any feedback": they use only activators. Feedback in Machinations "needs a closed circuit that consists of at least one state connection". Diagrams and simulation help designers spot dominant strategies — [DiGRA 2011 PDF](https://dl.digra.org/index.php/dl/article/download/593/593)

**Uses beyond economies (progression, abstract tension, AI/pacing)**
- Progression and missions: the Forest Temple and lock-and-key models above. The same paper cites the *Warhammer Fantasy Roleplay* (2009) progress tracker. It can track "the built up of some looming danger unknown to the players". Its party-tension meter can cause stress, fatigue or wounds, which Dormans calls "positive but destructive feedback" — [DiGRA 2011 PDF](https://dl.digra.org/index.php/dl/article/download/593/593) [F]
- Running mechanics in a live game: "Adapting game mechanics with Micro-Machinations" (Klint & van Rozen, with Dormans's group) — [HvA research listing](https://research.hva.nl/en/publications/adapting-game-mechanics-with-micro-machinations/) [S]
- Stealth and horror: the machinations.io glossary entry "Proximity" says proximity can decide whether an enemy detects the player in stealth games, and can "intensify the sense of fear and suspense" in horror — [glossary](https://machinations.io/glossary/proximity) [S]. These are general statements; there is no model behind them.
- A director-pacing diagram that is *not* Machinations, for comparison (Valve, Michael Booth, "The AI Systems of Left 4 Dead", 2009) [F]:
  - "Survivor Intensity" rises when a survivor is injured (in proportion to damage), incapacitated, or pulled off a ledge. It also rises when a nearby Infected dies (inversely to distance). It decays toward zero, but not while Infected are actively engaging the survivor.
  - The director cycles through **Build Up** (full threat until intensity peaks), **Sustain Peak** (3–5 s), **Peak Fade**, and **Relax**. Relax keeps a minimal threat population for 30–45 s or until the survivors have travelled far enough toward the next safe room.
  - "Algorithm adjusts pacing, not difficulty". "Amplitude (difficulty) is not changed, frequency (pacing) is". "Survivor Intensity estimation is crude, yet the resulting pacing works". Under future work: "Expand repertoire of verbs available to the AI Director".
  - [Booth slides PDF](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)

### Inferences
- **FrontRooms fit — strong, as an "attention/threat economy".** The book explicitly allows abstract pools like "threat level", so the Relay director can be drawn as:
  - *sources*: noise from verbs (ease, barge, slam, pry), footsteps by gait, sight contacts;
  - *pools*: Relay attention, per room or per target, and player stress;
  - *drains*: decay over time, and sanctuaries;
  - *activators* on attention thresholds, which gate the staged warnings in "your room";
  - an *end condition*: caught.
  The L4D state set (Build Up / Sustain / Fade / Relax) shows that director phases can sit on top of this as activator-gated pools.
- **Where it breaks for FrontRooms:** Machinations has no geometry. Hearing by path, sight cones, and which door separates which room must be collapsed into gates or probabilities, or into a room graph. The Forest Temple diagram shows that pools-as-locations works for a small room graph but becomes cluttered at scale. Use it for the *quantities*, not the *map*.
- Artificial players and charts are the useful part for a horror pass. Script "always barge" against "always ease" players and chart Relay attention to check for dominant strategies. This is the dominant-strategy check Dormans recommends.

### Gaps
- I could not read the current machinations.io specifications for **Register, Delay and Queue** nodes: the docs render client-side, and the book's sample section stops at converters. Their notation is unverified here.
- I did not obtain the 2009 GAMEON-NA paper or the 2012 thesis text, so the full pattern list (static/dynamic engine, friction, stopping mechanism, etc.) is only partly verified. Only dynamic engine, dynamic friction, escalating complications and escalating complexity are confirmed above.
- I found no published Machinations model of a horror game, a stalker AI, or an AI director.

---

## 2. Causal-loop and feedback-loop diagrams: positive vs negative feedback and where game designers use them

### Takeaway
Causal-loop diagrams (CLDs) come from system dynamics. They connect noun variables with signed arrows (+/−, older s/o) and label each closed loop **R** (reinforcing) or **B** (balancing), with a tick on an arrow for delays. Game design adopted the positive/negative feedback idea from Marc LeBlanc's GDC 1999 talk. *Rules of Play* ch. 18 "Games as Cybernetic Systems" popularised it. Machinations then made it runnable: there, feedback = a closed circuit that contains a state connection. CLDs are quick and easy to read, which makes them good for explaining why a game snowballs or stabilises. However, they hide accumulations (stocks), rates and thresholds unless annotated, and they cannot be simulated.

### Cited Findings
**Notation (system dynamics)**
- Variables are written as words. The arrows "do not represent a material flow". "+" means both variables move the same way and "−" means opposite ways. Loops are labelled R or B and often numbered (B1, B2). To find a loop's type, trace a change around it: a reinforcing loop has an even number of negative links. A delay is marked with a short line across the link. Reinforcing loops tend toward exponential growth or decline, balancing loops toward a plateau. The article traces the history to Sewall Wright (1918) and Maruyama's "The Second Cybernetics" (1963) and cites Sterman's *Business Dynamics* — [Wikipedia, "Causal loop diagram"](https://en.wikipedia.org/wiki/Causal_loop_diagram) [F]
- Daniel Kim's classic guidelines use **s/o** (same/opposite) and say to name variables as nouns ("Costs", not "Increasing Costs"). A balancing loop is goal-seeking, so state its goal explicitly — [Kim, "Guidelines for Drawing Causal Loop Diagrams" (PDF)](https://thesystemsthinker.com/wp-content/uploads/2016/01/PG03E-Guidelines-for-Drawing-Causal-Loop-Diagrams.pdf) [S]. A later Systems Thinker article prefers **+/−** because it also applies correctly to flow-to-stock links. It warns that the shortcut of counting negative links can fail through mislabelled polarity — [Systems Thinker, "Fine-Tuning Your Causal Loop Diagrams—Part I"](https://thesystemsthinker.com/fine-tuning-your-causal-loop-diagrams-part-i/) [S]

**How game design took it up**
- LeBlanc's 1999 GDC lecture "introduced feedback loops to the game design world" [F, DiGRA 2011]. As reported there:
  - Positive feedback "drives the game to a conclusion and magnifies early successes". Examples: Monopoly, and losing pieces in Chess (positive feedback with a negative effect).
  - Negative feedback stabilises the game, can prolong it, and "magnifies late successes". Examples: rubber-banding, and Super Mario Kart's strongest weapons hitting players in front.
  - In most multiplayer games with direct interaction, rational players already target the leader.
  - Negative feedback is illustrated with a thermostat.
  - The paper cites Salen & Zimmerman (2004), pp. 224–225 for these points.
  - [Dormans, DiGRA 2011 PDF](https://dl.digra.org/index.php/dl/article/download/593/593) [F]
- The 1999 talk's title: "Formal Design Tools: Feedback Systems and the Dramatic Structure of Competition" — [8kindsoffun.com](https://8kindsoffun.com/) [S; the site's TLS certificate had expired when I fetched it]. The DiGRA paper gives the slide file as `http://algorithmancy.8kindsoffun.com/cgdc99.ppt` [L, legacy, untested]. LeBlanc's later prose version, "Tools for Creating Dramatic Game Dynamics", is in *The Game Design Reader* (MIT Press, 2006) [S, via 8kindsoffun.com summary]. A DiGRA 2015 paper applies it to MOBAs — [The Well-Played MOBA](https://dl.digra.org/index.php/dl/article/view/729) [S]
- *Rules of Play* chapter 18 is "Games as Cybernetic Systems" (p. 212), in the unit on systems — [table-of-contents listing](https://archive.neural.it/init/default/show/1891) [S]. The MIT Press page returned 403.
- The cybernetic model taught in game-design courses: a **sensor** monitors game state, a **comparator** decides whether to act, and an **activator** changes the state. To fix an unwanted loop, change one of the three, or add a loop of the opposite type — [SUNY Oswego CSC430 slides "Feedback Loops"](https://www.cs.oswego.edu/~alex/teaching/csc430/slides/FeedbackLoops.pdf) [S]
- An open textbook adapted from Ian Schreiber's *Game Design Concepts* summarises the effects:
  - positive loops destabilise a game, shorten it and emphasise the early game;
  - negative loops stabilise it, lengthen it and emphasise the late game;
  - examples include the Mario Kart blue shell, possession changing after a score in basketball and football, rugby (positive), and Starfarers of Catan catch-up bonuses.
  - [Chemeketa CIS125G reader §5.6](https://computerscience.chemeketa.edu/cis125greader/MechanicsDynamics/FeedbackLoops.html) [F]
- Machinations.io's article defines a feedback loop as "a game system that takes the output of a game and introduces it as an input". Examples include Call of Duty killstreaks, Hades, XCOM, the blue shell and Cultist Simulator's notoriety. It notes that "a single positive feedback loop can negatively impact game balance" (killstreaks were later phased out) and that players often distrust dynamic difficulty — [article](https://machinations.io/articles/game-systems-feedback-loops-and-how-they-help-craft-player-experiences) [F]
- In Machinations form, the "more factories → faster production → more factories" loop is the recurring *dynamic engine* pattern — [AAAI 2011](https://cdn.aaai.org/ojs/12477/12477-52-16005-1-2-20201228.pdf) [F]
- Michael Sellers, *Advanced Game Design: A Systems Approach* (2018), puts a chapter "Creating Game Loops" in its Principles part — [InformIT](https://www.informit.com/store/advanced-game-design-a-systems-approach-9780134667607) [S]. I could not verify its loop notation. His 2015 essay defines a system as "parts that interact to form a purposeful whole". It is text-only, with no CLDs — [Sellers, Game Developer 2015](https://www.gamedeveloper.com/design/systems-game-systems-and-systemic-games) [F]

### Inferences
- **FrontRooms fit — best "one-picture" overview for a pitch or slide.** Two loops capture the Relay:
  - **R1 "panic spiral"**: Noise (+) → Relay attention (+) → Relay pressure in your room (+) → player panic verbs (barge, slam, run) (+) → Noise.
  - **B1 "hide and decay"**: Relay attention (+) → player hides, eases doors, reaches a sanctuary (−) → Noise and visibility (+) → Relay attention.
  - Put a delay tick on "Noise → Relay attention" to show hearing-by-path propagation time.
  - The staged warnings are the visible output of the sensor–comparator–activator chain.
- In LeBlanc's terms, a reinforcing loop toward "caught" ends an encounter. Balancing loops (decay, sanctuary) bring back the tension cycle, much like L4D's Relax phase (§1).
- Limit to respect: a CLD cannot show the threshold where a staged warning fires, or how much attention has built up. Pair it with a Machinations or stock-and-flow sketch, or a state chart, for those.

### Gaps
- I could not read *Rules of Play* ch. 18. The attribution of the sensor/comparator/activator model and the thermostat example to that chapter rests on secondary sources: the DiGRA paper cites pp. 224–225 for the LeBlanc points.
- I found no CLD drawn by a shipped game team. Sellers's loop chapter is unverified. LeBlanc's 1999 slides were not retrieved.

---

## 3. Breath of the Wild's chemistry engine (GDC 2017): the rules and how they were shown

### Takeaway
The talk was "Change and Constant: Breaking Conventions with *The Legend of Zelda: Breath of the Wild*" (GDC 2017; Hidemaro Fujibayashi, Satoru Takizawa, Takuhiro Dohta). In it, technical director Dohta set the physics engine ("a rule-based movement calculator") against a chemistry engine ("a rule-based state calculator"). The chemistry engine splits the world into two classes, **elements** and **materials**, and reduces every state change to three rules. In effect this is a 2 × 2 interaction rule between classes, not a matrix of every object pair, and it is what lets Fujibayashi's "multiplicative gameplay" scale. I did not confirm the exact visual layout of the slide; the talk is on YouTube, with the chemistry section at about 38:14–45:28.

### Cited Findings
- Session page: "Change and Constant: Breaking Conventions with 'The Legend of Zelda: Breath of the Wild'", GDC 2017, speakers Fujibayashi, Takizawa and Dohta — [GDC Vault](https://gdcvault.com/play/1024562/Change-and-Constant-Breaking-Conventions) [S; the Vault page showed a login limit]
- Recording: ["Breaking Conventions with The Legend of Zelda: Breath of the Wild" on YouTube](https://www.youtube.com/watch?v=QyMsF31NdNc). The uploader appears to be GDC: the description says "We post a fresh GDC video every weekday". According to an AI summary, the chemistry section begins at [38:14](https://www.youtube.com/watch?v=QyMsF31NdNc&t=2294s) ("Expanding Beyond Physics") and the three rules come at [40:42](https://www.youtube.com/watch?v=QyMsF31NdNc&t=2442s) — [videohighlight summary](https://videohighlight.com/v/QyMsF31NdNc) [F, AI summary rather than a transcript]
- Definitions and rules (Daniel New, Thumbsticks, 2 Mar 2017) [F]:
  - physics is "a rule-based movement calculator"; chemistry is "a rule-based state calculator";
  - *elements* are things without a constant state (fire, water, ice, and also electricity and wind);
  - *materials* are solid objects (trees, rocks, the player);
  - Rules: "Elements can change the state of materials"; "Elements are able to change each other's states"; "Materials cannot influence each other's states";
  - Dohta counts "all phenomena that occur in nature" except rigid-body dynamics as "chemistry". He calls game physics "false physics or liars physics", and the team used Havok;
  - [Thumbsticks](https://www.thumbsticks.com/gdc-17-breath-of-the-wild-science-lies)
- Same three rules and examples: fire burns a tree, water puts out fire, wind on a sail, a sword thrown in a storm draws lightning. Dohta: "Little of it would scientifically work in the real world" — [GamesBeat, Mike Minotti, 3 Mar 2017](https://gamesbeat.com/the-legend-of-zelda-breath-of-the-wild-makes-chemistry-just-as-important-as-physics/) [F]
- Reports disagree on the list of elements. Tom's Guide gives fire, water and ice as elements, and trees, rocks, characters and weapons as materials — [Tom's Guide](https://www.tomsguide.com/uk/us/zelda-breath-wild-design,news-24586.html) [S]. A Zelda Universe explainer (republished by CheatCC) gives five elements: fire, water, ice, wind and electricity [S].
- Multiplicative gameplay: Fujibayashi described "a set of mechanics and methods that could be scaled and multiplied consistently throughout the game world". The NES-style prototype was "a three-dimensional game engine" underneath. It was a sandbox of "a river and some trees" in which Link crosses the river on a felled trunk — [Thumbsticks part 1](https://www.thumbsticks.com/gdc-17-breaking-conventions-breath-of-the-wild) [F]. Nintendo of America's live tweets said the 2D prototype ran the physics and "chemistry" engines in the background. The aim was an "active" game in which objects react to the player and the world — [Miketendo64 roundup](https://miketendo64.com/2017/03/01/nintendo-of-americas-twitter-coverage-of-the-gdc-breath-of-the-wild-panel/) [F]
- Slide photos appear in the [Perfectly Nintendo gallery](https://www.perfectly-nintendo.com/zelda-breath-wild-details-pictures-gdc-2017-panel/) [F: the gallery has no captions, so I could not identify the chemistry slide from text].
- A designer's reverse-engineered matrix for BotW is in §5: [Reeve, "Systems Interaction by Spreadsheet"](https://www.gamedeveloper.com/design/systems-interaction-by-spreadsheet) [F]

### Inferences
- **Visual grammar:** the talk shows the rules at the level of *classes*. Rows are the acting class (element or material) and columns the target class. The cells hold yes/yes/no for "can change state": E→M yes, E→E yes, M→M no. Every new object automatically joins this system by being assigned a class. That is what makes the system "multiplicative" rather than a list of hand-made pairs.
- **FrontRooms fit:** a three-rule "Relay chemistry" slide in the same form would make a strong systems overview. A possible rule set (a design proposal, not a sourced finding):
  1. Player verbs emit stimuli.
  2. Stimuli change the Relay's state; fixtures (doors, windows) change how stimuli travel (a shut door muffles).
  3. Fixtures do not change each other.
  A small 3 × 3 class matrix can follow, with an object-level matrix (§5) as backup.

### Gaps
- I did not verify the exact slide visuals (icons, arrows, layout). Watch the video from 38:14 or check the Perfectly Nintendo gallery images.
- The official element list (three vs five) differs between reports.
- Whether Nintendo kept an internal object × element table is not public.

---

## 4. How immersive-sim teams document systemic interactions (Looking Glass, Ion Storm, Arkane)

### Takeaway
The public record shows immersive-sim teams documenting interactions mainly in three ways, and very little as published grids:
- **data-driven object systems with stimulus/response rules**: Looking Glass's Dark Engine "Act/React", authored as *sources* and *receptrons*, and its descendant, The Dark Mod's Stim/Response system;
- **talk slides about connecting mechanics**: Harvey Smith and Randy Smith, GDC 2004;
- **philosophy talks and developer anecdotes**: Arkane on *Dishonored* and *Prey*.
I found no developer-published stim × response or verbs × objects table for Thief, Deus Ex, Dishonored or Prey. That includes Prey's GLOO and mimics.

### Cited Findings
**Looking Glass: Dark Engine (Thief, System Shock 2)**
- The Dark Engine offered "a powerful object-oriented object system". AI and object behaviour run in loadable .OSM script modules. NPCs have three awareness levels, and the designer controls how sound propagates through a level. DromEd has versions for Thief 1, Thief Gold, Thief 2 and System Shock 2 ("ShockEd") — [Wikipedia, Dark Engine](https://en.wikipedia.org/wiki/Dark_Engine) [F/S]
- Act/React "originated from Looking Glass". The thread credits Marc LeBlanc only as "the internet tells me", so that credit is unverified. It links:
  - an internal "LG/Doc 9/24/1999: Act/React requirements" [L, TTLG returned 403];
  - community guides titled "Sources and Receptrons" [L, certificate expired] and "Sources & Receptron Basics" [L, connection failed];
  - [Handmade Network thread](https://hero.handmade.network/forums/code-discussion/t/876/p/14085) [F]
- The descendant system, **The Dark Mod's Stim/Response** [F], takes its name from DromEd's system, though the wiki says the similarities are "only superficial". It defines "a stim [as] the equivalent to DromEd's Source".
  - Any entity can emit stims, carry responses, or both.
  - A response fires when its numeric ID matches a stim and runs a script action.
  - Each stim has a radius (a torch's fire stim is 10). The radius can grow over a set duration.
  - Magnitude can stay flat or fall off with distance via an exponent (0 none, 1 linear, 2 quadratic).
  - Built-in stims include Frob, Heal, Damage, Trigger, Sound, Visual, Communication and Water.
  - Since DarkRadiant 0.9.0 it is authored in a graphical Stim/Response Editor, per entity. The page shows no global stim × response table.
  - [The Dark Mod wiki, "S/R"](https://wiki.thedarkmod.com/index.php?title=S/R)
- Doug Church, "Object Systems": slides on "methods for attaching data to objects, and connecting behaviors" — [third-party mirror on SlideServe](https://www.slideserve.com/giolla/object-systems) [S]
- Tom Leonard, "Building an AI Sensory System: Examining the Design of *Thief: The Dark Project*" (Game Developer, March 2003) — [listing, AngelCode reference DB](https://angelcode.com/refdb/category.php?cat=118) [S; I did not obtain the article text]. A later talk in the same line: Eric Martel (Ubisoft), "Can You See Me Now? Building Robust AI Sensory Systems", GDC 2017 AI Summit. It proposes a designer-configurable sensory system drawing on *Assassin's Creed* and *Thief*, mostly visual with some audio — [GDC Vault](https://gdcvault.com/play/1026497/Can-You-See-Me-Now) [F]

**Ion Storm: Harvey Smith (Deus Ex / Invisible War) and Randy Smith (Thief 1/2/3), GDC 2004, "Practical Techniques for Implementing Emergent Gameplay"** [F, slide text pages]
- Stated goals: "Improvisational (player-authored) gameplay", a large possibility space, and emergence as more explorable space — [slide 2](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/tsld002.htm)
- "Where Mechanics 'Live'": power-ups, environmental objects, simulations and autonomous agents, e.g. "When barrels are damaged, they release poison gas" — [slide 24](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/tsld024.htm)
- A Thief chain written as a cause-and-effect list: box thrown → table wobbles → candle falls into oil → oil ignites → guard injured — [slide 20](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/tsld020.htm)
- "Creating Emergence by Adding Indirect Connections" uses a counter-example: a guard with a torch slips on oil, falls, and "Nothing else happens" — [slide 26](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/tsld026.htm)
- "Getting Mechanics to Connect": "Mechanics are independent, but indirectly connected" … "Choose Mechanics that have relationships to each other" — [slide 32](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/tsld032.htm)
- "Visualizing All Mechanics", "-1 Mechanic" and "-2 Mechanics" are graphic-only slides. The diagram is in the graphic version, e.g. [slide 29 graphic](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/sld029.htm) [L, image not inspected] — [outline](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/index.htm)
- "World population" negative example from Thief: fire elementals that re-light torches when guards are nearby were a good idea, but elementals and guards/torches "were never in the same levels" — [slide 43](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/GDC04_Emergence/tsld043.htm)
- Smith's archive also lists "Orthogonal Unit Differentiation" (`gdc03_OUD.ppt`), "Systemic Level Design" (`systemic_ld.zip`) and "The Future of Game Design" — [bio page](https://gamespy-archives.quaddicted.com/sites/www.planetdeusex.com/witchboy/bio.shtml.html) [F; files not opened]

**Arkane: Dishonored and Prey (2017)**
- GDC 2013, "Empowering the Player in a Story-Rich World: Co-Directing *Dishonored*" (Raphaël Colantonio and Harvey Smith). It argues for consistent game systems plus coherent world fiction, so that players improvise their own ways past obstacles — [GDC Vault](https://gdcvault.com/play/1018062/Empowering-the-Player-in-a) [F]
- PC Gamer's report on that GDC talk: playtesters' Blink + double-jump routes were supported rather than blocked. The rat-swarm power depends on rats being present, which depends on corpses left on lethal playthroughs — [PC Gamer](https://www.pcgamer.com/au/dishonored-arkane-player-improvisation) [S]
- Prey anecdotes (Ben Reeves, Game Informer, 23 Dec 2016) [F]:
  - Lead systems designer Seth Shain used the lift power to slam a phantom into a ceiling, then shot it with the GLOO gun, which held it there after the lift ended.
  - A kinetic blast pushed a medkit across a room. Ricardo Bare said the team simulates physics so such moments can happen.
  - Combining leverage with remote manipulation let a player catch a fridge thrown by a poltergeist and throw it back.
  - A level designer hacked a static turret and lifted it, after which turrets were made portable.
  - A QA tester who mimicked an Operator robot stopped being attacked by other Operators.
  - [Game Informer](https://gameinformer.com/b/features/archive/2016/12/23/seven-ways-prey-has-shocked-its-own-developers)
- Bare on mimics: the creature began as something that hides as objects among many unscripted physics objects. Because the design is systemic, even developers can't tell which object is the mimic — [Stevivor interview, Dec 2016](https://stevivor.com/features/interviews/prey-evolved-happy-accidents-caused-creative-linking-systems-powers/) [S; the page returned 403 to me]
- GLOO cannon origin: research into real non-lethal weapons. Prototypes included pellets bursting like airbags and a continuous firehose stream — [PCGamesN, "The Gunsmiths: Prey"](https://www.pcgamesn.com/prey/the-gunsmiths-prey) [S]

### Inferences
- A stim/response system is in effect a sparse matrix of stimulus types × receivers, stored on each object rather than drawn as a grid. For documentation it can be shown as a table:
  - rows: stimuli (sound by source and loudness, sight, damage, water or fire);
  - columns: receivers (the monster, doors, windows, props, lights);
  - cells: the response plus its parameters (radius, falloff, threshold).
- **FrontRooms fit — "honest senses" is a stim/response design.**
  - Each verb (ease, barge, slam, pry; walk, run, handheld walk) emits a *Sound* stim with a magnitude.
  - That stim is carried along the room/door path, as with Thief's designer-controlled sound propagation, and the Relay's "receptrons" are hearing-by-path and the sight cone (the *Visual* stim).
  - A stim × receiver table is the precise spec. It also shows gaps, e.g. does *pry* emit a distinct stim, and does a slowly eased window register at all?
- The Smiths' fire-elemental counter-example applies directly to trigger rooms and sanctuaries. A filled cell in an interaction table counts only if level design actually places the participants together. So a matrix should be checked against a per-room placement list.

### Gaps
- I could not open any primary Looking Glass Act/React documentation (TTLG 403, one guide's certificate expired, another site unreachable). The exact receptron effect list and intensity ranges remain unverified.
- I found no Arkane-published interaction table for Prey (GLOO, mimics, Typhon powers) or Dishonored. No developer source confirmed Prey's internal "signal"/damage-type architecture.
- I found nothing from Irrational (System Shock 2, BioShock). I did not obtain the Leonard article text or the image content of the Smiths' "Visualizing All Mechanics" slides.

---

## 5. Verb × object (verb × noun) matrices: what they are and how they expose gaps or emergent combinations

### Takeaway
A verb × object matrix lists verbs on one axis and object types or states on the other; each cell records what happens. The verbs can belong to the player or to an AI. Filled cells are intended or discovered combinations; blank cells are either gaps (a verb that doesn't apply, an object that doesn't respond) or deliberate no-ops. The idea comes from:
- Crawford's "verb thinking";
- Anthropy & Clark's "Verbs and Objects" chapter;
- Schell's Lens of Emergence ("How many objects can each verb act on?").
In practice it is a spreadsheet (Reeve 2018). It is pairwise and static. It says nothing about chains, timing, or whether the objects are ever in the same place, so it needs a loop or graph view alongside it.

### Cited Findings
- Schell's **Lens of Emergence** asks: "How many verbs do my players have?", "How many objects can each verb act on?", "How many ways can players achieve their goals?", "How many subjects do the players control?" and "How do side effects change constraints?" — [Schell Games blog, 7 Apr 2025](https://schellgames.com/blog/lens-of-emergence) [F]
- Crawford's interactive-storytelling work puts *verbs* at the centre. The book has a "Verb Thinking" chapter, and Storytron promises "thousands of verbs" — [Peachpit, *Chris Crawford on Interactive Storytelling*](https://www.peachpit.com/store/chris-crawford-on-interactive-storytelling-9780321864970); [Crawford's site](https://erasmatazz.com/library/my-books/chris-crawford-on-interacti.html) [S]. A University of Tartu thesis reviews Crawford, Koster and Anthropy, who all treat verbs as the way to describe a game's interactions and rules — [UT thesis record](https://dspace.ut.ee/items/fd28082d-4add-422e-bbf5-20a94e9bfc95) [S]
- Anthropy & Clark, *A Game Design Vocabulary* (2014): Part I (by Anthropy), ch. 2 "Verbs and Objects" (p. 13), which covers rules, creating choices, objects, the physical layer and elegance. The publisher pitches it as controlling the relationships between game "verbs" and "objects" — [InformIT](https://www.informit.com/store/game-design-vocabulary-exploring-the-foundational-principles-9780321886927); [O'Reilly](https://oreilly.com/library/view/a-game-design/9780133155181) [S]
- **A worked spreadsheet matrix** (Justin Reeve, Game Developer, 1 Aug 2018) [F]:
  - A *balancing grid*: classes × stats.
  - A *systems interaction grid*: 7 combat features (dodge, guard, sneak, melee, ranged, charged attack, weapon durability) × 7 BotW Sheikah Slate features (Magnesis, Remote Bomb, Stasis, Cryonis, Camera, Master Cycle Zero, amiibo), filled with known interactions (e.g. Stasis + melee). Blank cells mean no meaningful interaction, which "is acceptable".
  - Its limits: it is "quick-and-dirty", covers two systems at a time, is "hardly comprehensive", and does not predict accidents such as rocket jumping.
  - [Reeve, "Systems Interaction by Spreadsheet"](https://www.gamedeveloper.com/design/systems-interaction-by-spreadsheet)
- Interaction density is uneven, and designers build dependencies between objects on purpose. Systemic games "rely on interactions between game objects" — [Sellers, Game Developer 2015](https://www.gamedeveloper.com/design/systems-game-systems-and-systemic-games) [F]
- "Design levers": tunable values held in data rather than hard-coded. The example is a fire ScriptableObject with combustion temperature, heat-up rate and heat radius — [Unity blog, Oriz & Nobbs, 7 Dec 2021](https://unity.com/blog/games/systems-that-create-ecosystems-emergent-game-design) [F]
- Door verbs as a design-problem checklist (Liz England, 21 Apr 2014). Can every door be opened, or are some decoration? Can it be locked and later unlocked, and what signals that? How does it open (proximity, walking into it, a button)? Do enemies or allies use it? What if someone is in the way? — [Liz England, "The Door Problem"](https://lizengland.com/blog/2014/04/the-door-problem/) [F]
- A director can have verbs too: L4D lists "Expand repertoire of verbs available to the AI Director" as future work — [Booth 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf) [F]

### Inferences
- **How matrices expose gaps:** every blank cell forces a decision: implement it, make it a deliberate no-op, or remove the verb. Rows with few entries are weak verbs, and columns with few entries are inert objects. Cells marked "emergent" record discoveries from playtests, like Prey's lift + GLOO.
- **What they miss:** (1) chains (A→B→C, as in the Smiths' Thief example). (2) Co-location: the fire-elemental case. (3) Time and feedback, which need a CLD or Machinations (§1–2).
- **FrontRooms fit — the most precise spec for the door and window systems.** Build two matrices:
  - **(A) Player verbs × fixture states.** Rows: ease, barge, slam, pry (+ hold/brace, peek). Columns: wooden door latched, locked, jammed, open, broken; window shut, latched, broken; trigger-room door; sanctuary door. Each cell holds the outcome, the time cost, the Sound stim magnitude (§4) and the Relay response.
  - **(B) Relay verbs × the same fixtures.** Can it ease, barge or pry? This keeps the monster under the same rules as the player, the "consistent systems" principle Arkane and Looking Glass stress (§4), and it answers England's "do enemies use doors?".
  - Mark cells ✓ implemented / ○ deliberate no-op / ? gap / ★ emergent.
  - Then check every ★ and ✓ against a per-room placement list (trigger rooms, sanctuaries) to avoid the fire-elemental trap.

### Gaps
- I found no verb × object matrix *published by a developer* for an immersive sim. Reeve's BotW grid is a third-party reverse-engineering.
- I did not read the actual chapter text of Crawford ("Verb Thinking") or Anthropy & Clark ("Verbs and Objects"); their content above rests on the publishers' tables of contents and blurbs.
