# Loops and progression diagrams: core and compulsion loops, loops and arcs, skill atoms and chains, MDA

Researched 2026-10-15. These notes contain links only. No images, PDFs or slides were saved to the project. I did not open the diagram images themselves, so any description of a drawing's layout is inferred from the author's text, figure captions and image file names, and is labelled that way. Quotations are kept to one; everything else is paraphrased.

## 1. What does a core or compulsion loop diagram look like, who popularised it, and what are the criticisms?

### Takeaway
A core-loop diagram is a small closed ring of 3–5 player verbs or phases joined by arrows that lead back to the start. Its key explanations come from mobile free-to-play (F2P) analysis (Katkoff 2013, Kim 2014), which added session loops and left the long-term metagame off the drawing. Josh Bycer's posts in 2019–20 carried the term "core gameplay loop" into general design writing. Critics say the ring hides long-term structure, records one pass instead of the range of possible outcomes, and comes from retention and monetisation design built on behaviourist reward schedules.

### Cited Findings
**Compulsion loop: definition, stages and lineage**
- Joseph Kim, "The Compulsion Loop Explained" (Game Developer, 23 Mar 2014; first posted on his Quarterview blog). Adapting Adam Crowe's definition, Kim calls a compulsion loop a habitual, designed chain of activities aimed at a neurochemical payoff (pleasure or relief). He keeps it separate from the *core loop*, which he describes as the chain of activities in the game's primary user flow — [Game Developer](https://www.gamedeveloper.com/business/the-compulsion-loop-explained)
- Kim's diagrams show three phases. In **Anticipation** the player wants a state, such as more gold or power; **Action** is the behaviour the designer wants; **Reward** is the payoff. He grounds the idea in Skinner's operant conditioning ("Skinner box") and in ratio and interval reward schedules — [Game Developer](https://www.gamedeveloper.com/business/the-compulsion-loop-explained)
- Kim's examples:
  - Clash of Clans lets players visit high-level villages ("aspirational neighbour").
  - Hay Day's tutorial sends players to an advanced farm.
  - Final Fantasy Airbrigade adds a variable-ratio Chocobo capture.
  - He calls Mafia Wars and FarmVille purely compulsion-driven.
  - Game of War's excessive appointment timers are the article's only cautionary example.
  — [Game Developer](https://www.gamedeveloper.com/business/the-compulsion-loop-explained)
- John Hopson, "Behavioral Game Design" (27 Apr 2001; the author bio lists him as head of user research at Bungie), is an early game-industry primer on reinforcement. It covers:
  - Fixed and variable ratio schedules, and fixed and variable interval schedules.
  - Chain schedules, extinction, and avoidance, such as Ultima Online castles that decay unless visited.
  - It never uses the words "compulsion" or "loop", and it calls itself a primer rather than a blueprint.
  — [Game Developer](https://www.gamedeveloper.com/design/behavioral-game-design)
- Alexandre Mandryka (Game Developer featured community blog, 10 Aug 2016) reduces the compulsion loop to three steps: lower the player's tool efficiency, offer a clear way back to baseline, then repeat. He compares the dip to withdrawal. His illustrations are Diablo's race between damage and monster HP, and a Clash of Clans chart of wall-breaker strength against wall HP — [Game Developer](https://www.gamedeveloper.com/design/compulsion-loop-is-withdrawal-driven)

**Core loop in F2P practice: structure and built-in nesting**
- Michail Katkoff, "Mid-Core Success Part 1: Core Loops" (24 Oct 2013), names three traits of successful mid-core loops — [Game Developer repost](https://www.gamedeveloper.com/design/mid-core-success-part-1-core-loops); original on [Deconstructor of Fun](https://www.deconstructoroffun.com/blog//2013/10/mid-core-success-part-1-core-loops.html):
  - A *dual loop*: two session loops inside one core loop.
  - A controlled median session length, which follows from the dual loop.
  - A *metagame*.
- In Katkoff's Clash of Clans example the loop is resource collection → building and training → battling. A quick session takes about 30 seconds and a full session under 5 minutes — [Game Developer](https://www.gamedeveloper.com/design/mid-core-success-part-1-core-loops)
- Katkoff says the metagame does not appear in the core-loop picture. It is the part you only see by playing, which a proponent of the form concedes as a limit — [Game Developer](https://www.gamedeveloper.com/design/mid-core-success-part-1-core-loops)

**Bycer and the "core gameplay loop" in general design writing**
- Josh Bycer (25 Apr 2019) defines the core gameplay loop as the primary system or mechanic that defines a game, often deciding its genre. The post is an excerpt from a longer Super Jump Magazine piece and uses Clockwork Empires' troubled development as its case — [Game Developer](https://www.gamedeveloper.com/business/why-the-core-gameplay-loop-is-critical-for-game-design)
- Bycer (6 Mar 2020) makes these points — [Game Developer](https://www.gamedeveloper.com/design/the-importance-of-a-well-defined-core-gameplay-loop):
  - The loop is the moment-to-moment "microlayer", measured in minutes, as opposed to macro goals.
  - A player should understand the game within minutes.
  - Players should always see the immediate goal, the tasks needed, how long they will take, and what permanent progress they have made.
  - Open worlds that simply tell players to explore leave them without direction.
  - The post has one captioned image, and no diagram is described.
- Bycer's piece on progression models (Game Wisdom, 28 Aug 2018) describes three kinds of progression. It says a game becomes a grind when its progression stops working — [Game Wisdom](https://game-wisdom.com/critical/progression-models):
  - Player progression: skill and knowledge, as in the Souls series.
  - Character progression: abstracted stats and levels.
  - Game progression: levels or areas leading to the credits.

**Criticisms**
- *Ethics and business model.* Adrian Hon (Six to Start) responded to BBC Panorama in December 2010. He described compulsion loops, and avoidance mechanics such as FarmVille's withering crops, as techniques that keep feeding players new content and goals. He argued that games funded by subscriptions and microtransactions have a financial interest in maximising play time. MCV's report questioned his evidence — [MCV](https://mcvuk.com/business-news/hon-defends-panorama-claims/)
- *Design.* Mandryka warns that a compulsion loop on its own starts to feel meaningless to skill-driven players, who need layered systemic depth — [Game Developer](https://www.gamedeveloper.com/design/compulsion-loop-is-withdrawal-driven)
- *Representation (one pass vs. possibility space).* Daniel Cook argues that writing a loop out as a single pass (his "expanded loop", a series of arcs) records one playthrough and misses the loop's range of possibilities. A loop describes a statistical spread of outcomes — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/)
- *Representation (linear vs. nested).* Raph Koster (2007) said Cook's skill-chain diagrams look mostly linear. He and Andrew McLennan see game atoms as nesting to any depth — [raphkoster.com](https://www.raphkoster.com/2007/07/23/cooking-up-chemistry/)
- *Business form.* Cook ties loop-heavy design to business models without using the term F2P. Arcade operators and in-app purchases reward sustained play, while retail rewarded beatable, consumable arcs — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/)

### Inferences
- **Visual grammar (inferred; images not viewed).** A ring of 3–5 labelled nodes, such as phases (anticipation, action, reward) or verbs (collect, build, battle). Arrows show sequence and sometimes resource flow, and the last arrow returns to the first node. F2P variants add a second ring sharing a node (Katkoff's dual loop) and leave the metagame and progression outside the drawing.
- **What it shows.** Flow and repetition at one time scale. It does not show state, dependency or long-term progression.
- **When used.** Pitch one-pagers, F2P session and retention design, onboarding checks ("can a player name the loop after five minutes?").
- **Lineage.** Behaviourist reward schedules (Hopson 2001) → "compulsion loops" in public debate by 2010 (Hon) → F2P core-loop teardowns (Katkoff 2013, Kim 2014) → generic "core gameplay loop" (Bycer 2019–20). Cook's loops come from a different tradition, learning and mental models, even though both are drawn as circles.
- **FrontRooms.** A one-ring core loop works as a pitch slide: enter room → look, listen, read → choose a door or window → respond to a Relay warning → next room. The compulsion template does not fit, though. Its reward phase, appointment timers and avoidance are retention tools for persistent, monetised games, and FrontRooms is a single-run horror game whose payoff is reaching the exit. The ring also cannot show the director's pacing or the procedural map's progression, so pair it with a nested-loop diagram (section 2) and a pacing timeline (section 5).

### Gaps
- I did not find who first used "core loop" in print. The earliest dated use of "compulsion loops" in these sources is Hon's, December 2010.
- I did not view Kim's or Katkoff's images (link-only constraint), so their layouts are inferred.
- I found no rigorous empirical critique of core-loop diagrams; the critiques are essays and opinion.
- Bycer writes a lot about core loops, but no source shows he popularised the term.

## 2. What are Daniel Cook's "loops and arcs" and nested-loop diagrams, how are they drawn, and what do they reveal that a flat list cannot?

### Takeaway
Cook's loop runs mental model → action → system/rules → feedback → updated model, and is drawn as a ring that returns to the player's model. An arc is the same chain without the return. Combining the two shows what repeats and what is consumed once, which parts are engines and which are filler, and what the content will cost to produce. The combinations are sequences of arcs, arc/loop "sandwiches", parallel arcs, and nested, dependent loops at many time scales. A flat feature list cannot show repetition, dependency, time scale or which feedback stage is failing.

### Cited Findings
- **"Loops and Arcs"** (Lostgarden, 30 Apr 2012). In a loop the player holds a mental model, acts on a system, gets feedback, updates the model, then repeats or starts a new loop. Loops are fractal, running at many levels and frequencies. They deliver value by being exercised, so they suit mastery and trial and error — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/)
- An **arc** has the same parts (model, action, system, feedback) but is not built for repetition; books and films are Cook's examples. In his words, "An arc is a broken loop you exit immediately." The payload sits in the feedback, repeat exposure loses value, and players burn out on arcs quickly — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/)
- Loops build what Cook calls *wisdom*: a robust, branching model of the system. Arcs deliver *success stories*, lessons that break down when the context changes — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/)
- **Combinations** Cook names — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/):
  - A *sequence of arcs*, the basis of most commercial content, which leads to the "content treadmill": stop producing content and the business fails.
  - An *arc/loop sandwich*, such as cutscene → gameplay → cutscene.
  - *Parallel arcs*, such as music running alongside play.
  - Levels as *spatial arcs* framing a central loop.
  - *Micro parallel arcs*, as in Half-Life.
  - *Nested, dependent loops*, which produce complex feedback and unexpected dynamics.
  - His analysis question for any of these is which parts repeat and which do not.
- Removal exercise: most games stay playable with their arcs stripped out, but arcs can never be removed entirely, nor should they be. Cook recommends separating filler from core engines, and calls loop-focused design the harder path — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/)
- The essay's four diagrams — [Lostgarden](https://lostgarden.com/2012/04/30/loops-and-arcs/):
  - [loop](https://lostgardenhome.files.wordpress.com/2019/09/a6772-gameatoms_loop.png)
  - [arc](https://lostgardenhome.files.wordpress.com/2019/09/987c0-gameatoms_arc.png)
  - [sequence of arcs](https://lostgardenhome.files.wordpress.com/2019/09/8da11-gameatoms_sequenceofarcs.png)
  - [arc/loop sandwich](https://lostgardenhome.files.wordpress.com/2019/09/a62e1-gameatoms_arcloopsandwich.png)
- **"Building Tight Game Systems of Cause and Effect"** (Lostgarden, 1 Jul 2012) splits the loop into action, rules, feedback and mental-model update — [Lostgarden](https://lostgarden.com/2012/07/01/building-tight-game-systems-of-cause-and-effect/):
  - A *tight* system has clear cause and effect; a *loose* one hides it. There is no single correct tightness.
  - The lowest-level skill loops should usually be the tightest.
  - Loops range from about 200 ms (hover dialogs) to whole-game consequences, such as Alpha Centauri's late game.
  - Mastery "chunks" loose systems into tight, repeatable patterns.
- The same essay attaches tuning dials to each stage. Cook advises using them as tuning tools after playtests, not for up-front planning. Loop image [here](https://lostgardenhome.files.wordpress.com/2019/09/b0749-gameatoms_loop.png) — [Lostgarden](https://lostgarden.com/2012/07/01/building-tight-game-systems-of-cause-and-effect/)
  - Action: option complexity, pacing.
  - Rules: processing complexity, probability, indirection, linearity.
  - Feedback: strength, noise, sensory channel, hidden information, discreteness. Feedback failures are the most common error.
  - Modeling: time pressure, use of existing mental models.
- **Chains vs. nesting** — [raphkoster.com](https://www.raphkoster.com/2007/07/23/cooking-up-chemistry/):
  - Koster (2007) says a skill atom breaks down into smaller atoms, all the way to button presses. Higher challenges are built from mastered lower ones (jumping → ledges → crossing canyons).
  - Cook, replying in the comments as "Danc", said a directed graph can represent nesting and handles atoms that feed several higher atoms more flexibly. In his view, choosing between the two is a matter of information design.
- **Nested loops in industry practice.** Katkoff's dual session loops inside a core loop, with a metagame around both — [Game Developer](https://www.gamedeveloper.com/design/mid-core-success-part-1-core-loops). Bycer's split between a minutes-scale micro layer and macro goals — [Game Developer](https://www.gamedeveloper.com/design/the-importance-of-a-well-defined-core-gameplay-loop)

### Inferences
- **Visual grammar (inferred from text and file names; images not viewed).**
  - Loop: labelled stages in a ring, with an arrow back into the player's model.
  - Arc: the same stages in a line with no return.
  - Sequence of arcs: arcs placed end to end.
  - Sandwich: arc → loop → arc in a row.
  - Nesting: small fast rings hung inside or off larger slow rings, best labelled with their time scale.
- **What it reveals beyond a list.**
  1. What repeats and what is consumed once.
  2. Which loops depend on which.
  3. Time scale and frequency.
  4. Production cost: arcs need new content, loops need tuning.
  5. Which stage of a loop is failing (Cook's per-stage dials).
- **FrontRooms fit: strong.**
  - Nested loops:
    - Seconds: look or listen → door or window action → sound, light or Relay cue → updated model.
    - Room: enter → scan or read → choose an exit.
    - Relay encounter: staged warning → pursuit → withdrawal.
    - Run: procedural map → exit or death → new seed.
  - Arcs, consumed once:
    - The 1990 narrative strings.
    - EGRESS placards.
    - Close-up documents.
    - The title-to-map handoff.
  - Asking what repeats and what does not shows that the procedural rooms are loops and the readables are arcs. If the readables gate progress, they will turn into a treadmill. Run as Half-Life-style micro parallel arcs, they will not.
  - A storyboard of one scripted run is Cook's "expanded loop": it misses the procedural spread, so judge the design over many seeds.
- **Tight vs. loose in horror.** Horror loosens feedback around the monster on purpose (noise, hidden information). Cook's rule that the lowest loops stay tight still argues for crisp, readable door and window handling, even when the Relay cues are ambiguous.

### Gaps
- I did not view the four images, and the text gives no exact layout (orientation, arrow style).
- I found no single Cook article titled "nested loops". Nesting is covered within "Loops and Arcs", "Building Tight Game Systems" and the comment exchange with Koster.

## 3. How are skill atoms and skill chains drawn, and how are they used to find gaps in a design?

### Takeaway
A skill atom is a small closed feedback loop in which the player learns one skill. In 2007 it had four stages (Action → Simulation → Feedback → Modeling); the 2021 repost adds Decision. A skill chain is a directed graph of atoms. It starts from the skills players already bring, runs from prerequisite to dependent skills, and can carry status icons (mastered, partially mastered, unexercised, active/grind, burned out). Designers use it to list the skills needed to start and finish, to place feedback and pleasure, and to diagnose burnout:
- Early burnout starves everything downstream.
- Late burnout makes upstream skills wither (atrophy).
- Players may lack the initial skills.
- Veterans may already know what the tutorial teaches.
- Red herrings lead nowhere.

### Cited Findings
- Daniel Cook, **"The Chemistry of Game Design"** (Gamasutra feature, 19 Jul 2007; now on Game Developer). A skill atom is a self-contained feedback loop in which the player gains a skill, with four elements: **Action, Simulation, Feedback, Modeling**. The 2007 text does not use the word "Decision" — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design)
- The **2021 Lostgarden repost** (13 Mar 2021, reposted with minor edits and repaired links) renames the atom an *interaction loop* and lists **five** elements. It adds **Decision**, in which the player looks at the known affordances and weighs possible outcomes, before Action — [Lostgarden](https://lostgarden.com/2021/03/13/the-chemistry-of-game-design-2/)
- Worked atom, Mario's jump: press the button → Mario arcs → the animation plays → the player concludes that the button means jump. Players usually go round an atom several times before it clicks — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design)
- **Skill chain:**
  - Atoms are linked into a directed graph. The skill one atom produces feeds the actions of atoms further down the chain.
  - Players bring an initial set of skills, and these form the chain's starting nodes.
  - Tetris shows how a mechanically simple game can have a large chain.
  — [Lostgarden 2021](https://lostgarden.com/2021/03/13/the-chemistry-of-game-design-2/); [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design)
- **Figures in the article** — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design):
  - Diagram 5: the canonical skill atom.
  - Diagram 6: the Mario-jump atom.
  - Diagram 7: two linked atoms.
  - Diagram 8: a sample skill chain for Tetris.
  - Diagram 9: players' limited foresight.
  - Diagrams 10–12: status icons, including the active and burned-out icons.
  - Diagram 13: skill atrophy caused by later-stage burnout.
  - Diagram 14: pre-existing skills feeding the first atoms.
  - The Tetris chain is also posted as a [PDF](https://lostgarden.com/wp-content/uploads/2021/03/tetris-skill-chain.pdf). Image links, matched to captions by position because the page has no alt text: [canonical atom](https://lh3.googleusercontent.com/XEEgNrvaIhBmKYbRIegEFNzED7vfJPhfphv3dSTdDagZW7IdU0YdY0oxAM0Y9h4sc0G8i3nwzN9EWygdVkB8LTcWTxcNApk3yImkixTbyfCivJae7uCKhlgXryDIEZ-Itmb8eXL8), [Tetris chain](https://lh6.googleusercontent.com/iN-HKwUUWkd0pLY1meMzOhfMApMp_8hxYHTrXmi8DNepkuZyC7CdJzVlPswV1jFsWVqBaN8vNI5dNy54FqIG1TSWRu5H1D5k5ek-bsIhJHFGF8-QC1UaSiBSZUdk1UoZ-F-7VxA), [status icons](https://lh3.googleusercontent.com/0LdKqmXG9O2gJjJ1zD4jddOToG0WKgmRHdy6_p1M6XXpi5SYTYOBl-BOXsVLdEC1rPPF4USE1Q8PuAdWprPGzd46W6YVbXJjmjN9Q-WOLfiyZvI7jgiY7BaDcTODhKhyC20X9lk), [atrophy](https://lh4.googleusercontent.com/551tBhelWpofryst61MgD64BZ8bK1jpU-GSyexoJUYY1OOpMcqbQBhI5GYmgm5Ozf9Ho5-nVUuFvgBjA3tma8AatHIeQDN1obCeixxIy7eqZxl0eljXGdXwc2CBF6f5LMPXdvAE) — [Lostgarden 2021](https://lostgarden.com/2021/03/13/the-chemistry-of-game-design-2/)
- **Status icons:** mastered, partially mastered, unexercised (not yet tried), active (used as a tool to reach other atoms, i.e. the grind), and burned out — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design)
- **Six stated uses of a chain** — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design); [Lostgarden 2021](https://lostgarden.com/2021/03/13/the-chemistry-of-game-design-2/):
  1. Identify the pre-existing skills needed to begin.
  2. Identify the skills needed to finish.
  3. Find which skills need feedback.
  4. Find where the pleasure is.
  5. Alert the team to where and when burnout happens.
  6. Give a framework for analysing why it happens.
- **Diagnostics in the article** — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design):
  - *Early-stage burnout:* a blocked early atom means downstream atoms are never exercised, such as a "reach platform" atom that is never mastered.
  - *Later-stage burnout and atrophy:* a platform with nothing on it teaches the player to stop jumping, so Jump withers.
  - *Lack of the correct initial skills:* players who don't realise a button does something, or non-gamers defeated by twin-stick controllers.
  - *Pre-mastery:* a veteran bored by a 10-minute navigation tutorial.
  - *Red herrings:* atoms in story, setting or imagery that give a pleasant sense of partial mastery but never lead to a useful skill. The example is the mushroom imagery in Super Mario Bros.
  - Burnout as the route to testability, through per-atom logging.
- **Limits Cook states:** the article covers only the basics; the limitations of skill chains are listed as a future topic; instrumentation is left for further exploration — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design)
- **Cook on MDA** (references section): MDA is descriptive, models the player experience poorly, and offers no objectively testable structure, whereas skill chains can be instrumented — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design)
- **Empirical chains.** Horn, Cooper and Deterding (CHI PLAY 2017) say designers usually build chains from design intent, theory or personal observation rather than from watching players. Their adapted cognitive task analysis brought overlooked low-level skills to the surface, though its efficiency and generality are unproven — [University of York](https://pure.york.ac.uk/portal/en/publications/adapting-cognitive-task-analysis-to-elicit-the-skill-chain-of-a-g/)
- **Gameful-design offshoot.** Deterding's "The Lens of Intrinsic Skill Atoms: A Method for Gameful Design" (*Human–Computer Interaction* 30(3–4), 2015) carries the atom into gamification — [TU/e special-issue introduction](https://research.tue.nl/en/publications/introduction-to-this-special-issue-on-hci-and-games/). A 2017 course abstract claims use in more than 20 industry projects; the claim is self-reported — [White Rose](https://eprints.whiterose.ac.uk/120651/)

### Inferences
- **Visual grammar.**
  - Atom: a small ring of four or five stages.
  - Chain: a directed acyclic graph (DAG) of atom nodes, with pre-existing skills at the entry and arrows from prerequisite to dependent skill.
  - Status icons overlaid on the nodes turn the chain into a heat map of where players stall.
  - Nesting (Koster) can be drawn by collapsing a sub-chain into one node.
- **How it finds gaps.**
  1. An atom with no incoming arrow that isn't a pre-existing skill is never taught.
  2. An atom with no outgoing arrow and no payoff is a red herring or a candidate for atrophy.
  3. A long single path means high early-burnout risk.
  4. An atom without a feedback channel is invisible to the player.
- **FrontRooms fit: strong, especially for onboarding and the Relay.**
  - Pre-existing skills: mouse look and WASD, real-world doors, reading.
  - Early atoms: open or close a door, hold or lock a door, look through a window, read a document up close.
  - Middle atoms: hear a staged warning and work out where the Relay is, hide behind a shut door, pick a route by EGRESS signage.
  - Late atoms: evade a pursuit, reach the exit.
  - Cook's diagnostics map onto real risks:
    - If players never learn to read the warnings, deaths block everything downstream (early burnout).
    - If door-holding is never needed later, it atrophies.
    - Atmosphere and readables that feed no skill are Cook's red herrings. Tie reading to route and exit knowledge.
- **Limit for horror.** The model treats mastery as the pleasure, but horror often wants some atoms to stay partly mastered, such as uncertainty about the Relay's senses. The chain can show where that uncertainty is deliberate, but it does not model dread. Cook's per-atom telemetry could track signals such as warnings heard versus warnings acted on.

### Gaps
- I did not view the diagram images, and the text does not specify orientation or arrow style.
- I could not read the full texts of Deterding (2015) or Horn et al. (2017), so I don't know which game Horn et al. studied.
- I found no published skill chain for a horror, stealth or exploration game.

## 4. How is MDA usually drawn, and is it a diagram for systems or a lens?

### Takeaway
MDA is drawn as three boxes, M → D → A, with two arrows in opposite directions. The designer reads from mechanics toward aesthetics, and the player meets aesthetics first and reaches mechanics last. The paper pairs this with a mapping from Rules → System → "Fun" and a designer–game–player figure. Its subsection "MDA as Lens" presents each layer as a lens or view, so MDA is mainly an analytic lens and vocabulary rather than a systems diagram. Its "dynamic models" section does include small system diagrams: a thermostat feedback loop, Monopoly's rich-get-richer loop, and the 2d6 distribution.

### Cited Findings
- **Origin.** Hunicke, LeBlanc and Zubek say MDA was developed and taught at the Game Design and Tuning Workshop at GDC, San Jose, 2001–2004. They present it as a formal approach that bridges design and development, criticism and technical research — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- **Definitions** — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf):
  - *Mechanics:* the game's components at the level of data representation and algorithms.
  - *Dynamics:* the run-time behaviour of the mechanics acting on player inputs and on each other over time.
  - *Aesthetics:* the desired emotional responses in the player.
- **Figures, as they appear in the paper's text** — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf):
  1. The designer creates the game and the player consumes it.
  2. Consumption is broken into Rules → System → "Fun", each mapped to a design counterpart: Mechanics → Dynamics → Aesthetics.
  3. An M–D–A row with designer and player arrows running in opposite directions. For the designer, mechanics produce dynamics, which produce aesthetics. For the player, aesthetics set the tone, which shows up in dynamics and finally in mechanics.
  4. A thermostat (thermometer, controller, room; "too hot" and "too cold") as a feedback system.
  5. Monopoly's feedback system, in which winners cash in and losers pay up.
  6. The probability distribution of 2d6.
- **Lens.** In a subsection titled "MDA as Lens", each component is described as a lens or view on the game, separate but causally linked; the paper cites LeBlanc's 2004 lecture for this — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- **Taxonomy.** Eight aesthetics: Sensation, Fantasy, Narrative, Challenge, Fellowship, Discovery, Expression and Submission. The paper profiles Charades, Quake, The Sims and Final Fantasy. Note that the Quake profile also lists "Competition", which is not one of the eight — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- **Dynamic models.** Challenge comes from time pressure and opponent play. Dramatic tension comes from dynamics that give rising tension, release and denouement. In Monopoly, rich-get-richer feedback kills tension, which subsidies, taxes or time-pressure mechanics could fix; the paper then tunes these by playtesting — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- **Stealth example.** In the third pass of its AI "tag" example, the paper recasts the game as Splinter Cell- or Thief-style stealth. Agents now reason over sensory data, and the designer must choose between realistic sound propagation and simple distance checks. The authors conclude that there are no "AI mechanics" as such: coherence comes from AI logic interacting with gameplay logic — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- The authors treat games as artifacts rather than media: a game's content is its behaviour, not the media it streams — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- **Critique: DDE.** Wolfgang Walk (Game Developer, 11 Nov 2015) argues — [Game Developer](https://www.gamedeveloper.com/design/from-mda-to-dde):
  - "Aesthetics" is a confusing term.
  - Most of what MDA files under mechanics is not mechanical: graphics, style, world-building, narrative.
  - MDA has no interface for narrative design.
  - The M→D→A order is right for the designer, but it is not a timeline.
  - He proposes **DDE**. *Design* splits into Blueprint, Mechanics (code only) and Interface (everything the player sees and hears). *Dynamics* stays. *Experience* splits into sensory, emotional and intellectual "journeys", written more from the player's side.
  - An academic version by Walk, Görlich and Barrett is listed on [Academia.edu](https://independent.academia.edu/WWalk).
- **Other critics.** Cook (2007) calls MDA descriptive and untestable as a model of the player — [Game Developer](https://www.gamedeveloper.com/design/the-chemistry-of-game-design). Koster (2007) called for unifying the atom/chain approaches with MDA and with Salen and Zimmerman's framework — [raphkoster.com](https://www.raphkoster.com/2007/07/23/cooking-up-chemistry/)

### Inferences
- **What it is.** MDA is a lens: a vocabulary and a direction of reasoning, starting from target aesthetics and working back to mechanics. It is not a picture of a particular game's system. Loop diagrams enter MDA only as illustrations of *dynamics*, through the thermostat and Monopoly figures.
- **When designers use it.** Early concept work (choosing target aesthetics), critique, tuning discussions and teaching.
- **Strengths.** It separates the designer's view from the player's, gives a shared vocabulary, and pushes design toward experience rather than features.
- **Limits.** It shows no flow, state, time or progression. "Aesthetics" is ambiguous, and audiovisual and narrative work has no clear home (Walk).
- **FrontRooms.**
  - Useful for stating target aesthetics: Sensation, Fantasy, Narrative, Discovery, and some Challenge.
  - The paper's own rising tension → release → denouement dynamic is a usable goal.
  - The stealth pass reads almost as a brief for the Relay's honest senses (sensory reasoning, the sound-propagation choice).
  - It cannot diagram the director or the warning stages, so use state, flow or pacing diagrams for those.
  - DDE's Interface and Blueprint layers fit FrontRooms' CRT text, lighting and Foley better than MDA, which would file them under mechanics.

### Gaps
- The extracted paper text does not name a venue. It is commonly cited as a 2004 AAAI workshop paper, which I did not verify here.
- I did not verify the DDE academic paper's publisher or year.
- I did not check whether the paper's workshop and lecture links (algorithmancy.8kindsoffun.com) still resolve.

## 5. Are there real examples from shipped games (postmortems, GDC talks) that show these diagrams, especially horror, stealth or exploration?

### Takeaway
Published primary diagrams from shipped horror games are mostly director and pacing diagrams, not core-loop rings or skill chains:
- *Left 4 Dead* (Valve): Adaptive Dramatic Pacing, with phases Build Up → Sustain Peak → Peak Fade → Relax → Build Up and plots of intensity and population over time.
- *Alien: Isolation*: a two-tier director/alien system with a menace gauge, documented second-hand from Creative Assembly's 2016 talk.
- Frictional's 4-Layers (GDC 2014, used for *SOMA*): narrative layers stacked on gameplay scenes.
- Outer Wilds: GDC talks on curiosity-driven exploration exist, but their content is behind GDC Vault.

The canonical published core loops are F2P (Clash of Clans).

### Cited Findings
- **Left 4 Dead: "The AI Systems of Left 4 Dead"**, Michael Booth, Valve; a 95-page 2009 deck hosted by Valve that does not name its venue — [Valve PDF](https://cdn.fastly.steamstatic.com/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf):
  - One AI goal is to generate dramatic pacing. It was inspired by Counter-Strike's spiky rhythm, since constant combat is fatiguing and long inactivity is boring.
  - The algorithm estimates each Survivor's "emotional intensity" and tracks the maximum across all four. When intensity is too high it removes major threats for a while; otherwise it builds an interesting threat population.
  - Intensity rises with damage taken, incapacitation, ledge pulls, and nearby Infected deaths (more when closer). It decays toward zero, but not while Infected are actively engaging.
  - Phases:
    - **Build Up:** full threat population until intensity crosses the peak threshold.
    - **Sustain Peak:** 3–5 s more.
    - **Peak Fade:** minimal population until intensity leaves the peak range, waiting for a natural break.
    - **Relax:** minimal population for 30–45 s, or until the team has moved far enough toward the next safe room, then back to Build Up.
  - Boss encounters are exempt.
  - The deck's conclusions: the system changes pacing (frequency), not difficulty (amplitude), and a crude intensity estimate still yields good pacing.
  - Slides plot how the Director shapes the population against the team's intensity.
  - A companion GDC 2009 deck, "Replayable Cooperative Game Design: Left 4 Dead", is also on Valve's CDN; I did not read it — [Valve PDF](https://cdn.fastly.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf)
- **Alien: Isolation.** Tommy Thompson, "The Perfect Organism: The AI of Alien: Isolation" (Game Developer, 31 Oct 2017, adapted from his AI and Games video), draws mainly on Andy Bray's (Creative Assembly) 2016 nucl.ai talk, "It's in the Vents" — [Game Developer](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation):
  - A macro *director* always knows where the player is and periodically points the alien toward them.
  - A micro *alien* AI, a behaviour tree of over 100 nodes, must find the player with its own senses.
  - A *menace gauge* rises with proximity, line of sight, and closeness on the motion tracker (more slowly if the alien is in another room).
  - At the gauge's peak the director pulls the alien back into the vents ("backstage") so the player can make progress. Active sweeping and searching is its "front stage" mode.
  - Game Developer also hosts [Revisiting the AI of Alien: Isolation](https://www.gamedeveloper.com/design/revisiting-the-ai-of-alien-isolation), which I did not read.
- **Frictional's 4-Layers.** Thomas Grip, Frictional Games blog, 29 Apr 2014, based on his GDC 2014 talk — [Frictional Games](https://frictionalgames.com/2014-04-4-layers-a-narrative-design-approach/); [Blogspot mirror](https://frictionalgames.blogspot.com/2014/04/4-layers-narrative-design-approach.html); [GDC 14 lecture resources](https://frictionalgames.blogspot.com/2014/03/gdc-14-lecture-resources.html):
  - The game is split into scenes, and four layers are built on each scene:
    1. Gameplay.
    2. Narrative Goal, a short-term story reason for the current action.
    3. Narrative Background, in which the actions themselves carry story.
    4. Mental Modeling, the player's internal model of the world.
  - Grip says the method is not linear; you move back and forth between layers.
  - It was co-developed with Adrian Chmielarz, was a cornerstone of *SOMA*, and The Astronauts used it on *The Vanishing of Ethan Carter*.
  - Chmielarz later wrote a critical reflection on its limits — [The Astronauts](https://www.theastronauts.com/2015/09/soma-and-the-lost-layer-of-narrative-design/)
- **Outer Wilds (exploration).** GDC Vault lists "Sparking Curiosity-Driven Exploration Through Narrative in 'Outer Wilds'" (Independent Games Summit) — [GDC Vault](https://gdcvault.com/play/1027008/Independent-Games-Summit-Sparking-Curiosity). A GDC preview describes a talk by Alex Beachum and Loan Verneau on the game's meta-design, from 2D prototypes to the final mystery structure — [GDC](https://www.gdconf.com/news/attend-gdc-and-learn-how-outer-wilds-nailed-curiosity-driven-game-design). Vault recordings are members-only, so I could not check whether either talk shows loop or progression diagrams.
- **Interest curve (arc/timeline form in a textbook).** Jesse Schell's *The Art of Game Design*, 3rd edition (2020), devotes chapter 17 to judging experiences by their interest curves — [Routledge](https://www.routledge.com/The-Art-of-Game-Design-A-Book-of-Lenses-Third-Edition/Schell/p/book/9781138632059)
- **Stealth in MDA.** The MDA paper's third pass treats a Thief or Splinter Cell-style stealth game as a worked example. It is a hypothetical design exercise, not a shipped-game diagram — [MDA paper](https://users.cs.northwestern.edu/~hunicke/MDA.pdf)
- **F2P core loop (the canonical published loop, not horror).** Clash of Clans: collect → build/train → battle — [Game Developer](https://www.gamedeveloper.com/design/mid-core-success-part-1-core-loops)

### Inferences
- **The closest published analogue for the Relay director** is Left 4 Dead's pacing loop. It is a *loop*, a four-state cycle, that generates an *arc*, the intensity-over-time curve. Two of its ideas carry over directly:
  - It separates pacing (frequency) from difficulty (amplitude).
  - Its Relax phase ends partly on player progress toward the next safe room. In FrontRooms that could be progress toward EGRESS.
- **Alien: Isolation's director, alien and menace-gauge split** is the clearest model for drawing a two-tier director over an honest-senses monster. It also gives a "backstage/front stage" vocabulary for the staged warnings.
- **4-Layers is the arc layer of Cook's sandwich** in a first-person horror context. It suits FrontRooms' close-up reading: room "scenes" get gameplay first, then narrative goal, background and mental model.
- **Which family to use for which FrontRooms question:**

| Family | Shows | Visual grammar | Good for in FrontRooms | Weak for |
|---|---|---|---|---|
| Core / compulsion loop | Repeated flow at one time scale | Ring of 3–5 verbs or phases; return arrow | Pitch slide; checking that the room-to-room verbs read in minutes | Pacing, director state, progression; compulsion framing doesn't suit a single-run exit goal |
| Loops and arcs / nested loops | What repeats vs. what is consumed; dependency; time scale | Rings (loops), open chains (arcs), sandwiches, nested rings | Separating procedural rooms (loops) from readables and narrative (arcs); setting loop time scales | Exact rules and state |
| Skill atoms and chains | Learning progression and dependency | Atom ring; DAG of atoms; status icons | Onboarding the door, window, warning and hide verbs; finding burnout, atrophy and red herrings | Dread and uncertainty; system state |
| MDA (and DDE) | Design reasoning from target experience | M→D→A row with opposing arrows; lens | Naming target aesthetics; the stealth-AI reasoning in the paper | Any flow or timing |
| Pacing / director (L4D, Alien: Isolation) | State cycle that produces an intensity timeline | Phase cycle plus intensity-vs-time plot; gauge | Relay director and staged warnings | Player learning |

### Gaps
- *Darkest Dungeon: A Design Postmortem* (Tyler Sigman, GDC 2016) exists according to GDC listings, but its content, including any town–dungeon loop diagram, is behind GDC Vault and was not verified.
- I found no core-loop ring or skill chain published by the developer of a shipped first-person horror or stealth game. The horror sources show pacing and director diagrams instead.
- The Alien: Isolation details are second-hand (Thompson summarising Bray). I did not access the nucl.ai talk itself.
- Fullerton's *Game Design Workshop* and Salen and Zimmerman's *Rules of Play* (feedback loops in cybernetic systems) were not checked against publisher previews in this pass.
