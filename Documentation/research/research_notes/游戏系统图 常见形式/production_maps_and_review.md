# How game teams present a whole game's systems for production and review: system maps, one-page designs, status boards, and the information design behind them

*Research notes, 2026-10-15. Written for a designer who rejected a 52-row FrontRooms system table (52 systems in 8 groups, each row with status, owner, dependencies and open decisions, on Letter-landscape sheets). Quotations are kept to one; everything else is paraphrased. "SimCity notes" below means Librande's GDC 2013 notes-view PDF ([SimCity-OnePage.pdf](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)), cited by page number.*

## Q1. What did Stone Librande show in *One-Page Designs* (GDC 2010), and why does it work?

### Takeaway
Librande replaced design bibles and wikis with single annotated visuals (maps, flow charts, matrices, storyboards, timelines) that are printed, hung up and marked up. They work because nothing is hidden behind a page turn, scroll or link, relationships are visible at once, and drawing the page forces the designer to understand and simplify the design. His 2013 SimCity follow-up is the closest precedent to a 52-system table. A whole game's system set did **not** fit on one page. He ended up with three linked layers: an overview diagram, a sortable spreadsheet, and a printed, grouped *decorated list* for the team.

### Cited Findings
**The 2010 talk**
- The GDC 2010 session defines a one-page design as a document that is exactly one page long, on the reasoning that most people read only the first page anyway. The listed examples are Diablo III, The Simpsons and Spore. — [GDC Vault: One Page](https://gdcvault.com/play/1012356/One-Page)
- Librande's own 2013 notes confirm that the 2010 examples came from Diablo 3, a Simpsons game and Spore. — [SimCity notes p.3](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- The slides were posted as OnePageDesigns.ppt (12 MB, updated 2 Apr 2010). — [stonetronix.com/gdc-2010](https://www.stonetronix.com/gdc-2010)
- Secondary listings disagree on the titles:
  - Stanford mediaX lists Diablo 3, The Simpsons Game, Spore and SimCity. — [mediaX event page](https://mediax.stanford.edu/event/one-page-design)
  - Game Developer's 2026 revisit names a Blizzard prototype, a *Simpsons: Hit & Run* paper prototype and Spore. — [Game Developer, 16 Jul 2026](https://www.gamedeveloper.com/design/-the-goal-of-design-is-to-efficiently-communicate-ideas-)
- **His diagnosis of the existing formats:**
  - A design bible is the definitive, all-in-one source, and writing it is itself a way of designing. But it does not scale, is hard to update and is hard to search.
  - A design wiki is easy to access, update, contribute to and version. But it needs constant maintenance, hides relationships between design elements, has low resolution and is limited by the viewport.
  - His slide's conclusion: use only one page.
  - — [SlideShare upload of the deck](https://www.slideshare.net/slideshow/one-pagedesigns/13460886)
- Game Developer's 2023 summary puts the core problem as: nobody reads long documentation, and wikis break the core relationships between elements. — [Game Developer, 8 Aug 2023](https://www.gamedeveloper.com/design/video-one-page-designs)
- The deck names these formats: flow charts, storyboards, time-and-space diagrams, relationships between modules and units, and matrices. One example is a character-class × element ability matrix: Fighter/Archer/Mage/Scout/Thief/Warlock × Fire/Metal/Nature/Water, with x for empty cells. — [SlideShare deck](https://www.slideshare.net/slideshow/one-pagedesigns/13460886)
- His inspirations were architectural blueprints, children's restaurant placemats, engineering schematics and timeline graphics. The revisit suggests whiteboard tools like Miro can carry the same practice today. — [Game Developer 2026](https://www.gamedeveloper.com/design/-the-goal-of-design-is-to-efficiently-communicate-ideas-)
- **Readability tips from the talk:**
  - Keep it extremely clear, with lots of white space.
  - Build around a strong central image, with room for notes.
  - Use small visual elements to show relationships between parts.
  - Do not let matrices get over-complex. He showed an over-complicated chart as the counter-example.
  - Expect many drafts. The Spore page took many drafts and rough working spreadsheets before it was clear.
  - Date every printed sheet so the current version is obvious.
  - Make it something people want on their wall.
  - Do not cram: the content has to fit together simply.
  - — [Game Developer 2023](https://www.gamedeveloper.com/design/video-one-page-designs)
- **Benefits he claims:**
  - For the team: one-pagers are easy to share, actually get seen, and invite participation (hand out pencils).
  - For the designer: they force full understanding and concise writing, show relationships within the system, and help solve problems.
  - His closing line: "The goal of design is to efficiently communicate ideas."
  - — [SlideShare deck](https://www.slideshare.net/slideshow/one-pagedesigns/13460886)

**The 2013 SimCity follow-up (the method tested on a whole game)**
- **Definition recap:**
  - Nothing is hidden from view: no stapled second page, no scrolling, no hyperlink.
  - The page can be any size or orientation.
  - Print it, hang it, hand out copies, and bring it to meetings to write on.
  - Balance clarity and concision against answering as many questions as possible.
  - — [SimCity notes p.5](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- He set himself the goal of doing all SimCity design as one-pagers, from pre-production to ship. He says he could not, and that the point was to find where the approach breaks. — [SimCity notes p.4](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- The first document (Sept 2009) was an **overview diagram that locked the high-level features**. It worked as an outline of the whole game, with each bold heading a feature set and little detail. The team then drilled down top-down into each section over the project. — [SimCity notes p.7](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- The team made more than 100 one-page documents for SimCity. — [SimCity notes p.79](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **Where one page broke: building and module data** (tuning, tooltips, triggers).
  - A magnetic whiteboard, a corkboard of cards and string, and index cards all proved impractical.
  - The answer was an Excel list of about 650 rows. It was updated almost daily, shared with the content director and the writer, and could be sorted, filtered and revision-controlled.
  - — [SimCity notes pp.55–61](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **He also kept a printed, *decorated* Excel list**, because most of the team would never open the spreadsheet.
  - He printed the high-level base buildings divided into 12 categories and hung more than 20 copies near producers', directors', artists' and designers' desks.
  - He replaced them about monthly, or after significant content changes.
  - He brought a copy to every meeting so that discussion of one system did not lose sight of the whole city.
  - — [SimCity notes pp.62–63](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- As the pages multiplied they became *one-wall* and then *one-hall* designs. A conference room became a *Design Gallery* used to walk press and new hires through the game. — [SimCity notes pp.85–87](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **Limits he admits:**
  - The pages were hard to keep current, and quality fell after alpha. Changes became scribbles on printouts. — [p.80](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
  - Writing still matters: he used the wiki daily. The problem is getting people to read, not the writing. He drafts a wiki outline first and then converts it, and text and picture inform each other. — [pp.90–91](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
  - Pick the audience: executives want high-level concepts, programmers want details, and not every page should be a hallway poster. — [p.88](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- Projected slides cannot show the paper documents. Slides are about 96 ppi against a laser printer's 1200 ppi, and are forced to 16:9, so he redrew many diagrams for the deck. — [SimCity notes p.6](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- He trained new hires with a talk-length walkthrough of the pages instead of telling them to read the wiki. — [SimCity notes p.92](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)

### Inferences
- **Why it works**, synthesising the above:
  1. It removes the navigation cost (page turns, scrolling, links) that hides relationships.
  2. Drawing the page is a design test. Librande says that if a diagram is too complex to understand, the design probably is too ([p.21](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)).
  3. Review moves into a shared space where people can annotate and cut.
- **For FrontRooms**, the SimCity case is the direct precedent. Librande did not put all systems' data on one page. He separated:
  - structure: the overview diagram and the system flow pages;
  - data and status: the spreadsheet;
  - scope at a glance: the grouped printed list.

  The rejected 52-row sheet merges all three into one artifact. That is the pattern his talk shows breaking down.
- A projected deck is not a wall. Librande re-laid out paper diagrams for 16:9. A FrontRooms overview meant for projection needs a slide-native layout, not a shrunken Letter sheet.

### Gaps
- I did not access the 2010 speaker notes or video transcript. The GDC Vault video needs a login, and I did not download the 12 MB .ppt without permission. So the content of the Diablo III, Simpsons and Spore pages is known only from secondary summaries, which disagree on titles.
- I found no primary source for a Riot- or Blizzard-specific house format. The Riot link is only Librande's later job as a Riot design lead ([Game Developer 2023](https://www.gamedeveloper.com/design/video-one-page-designs)).

## Q2. What do real game system maps and dependency diagrams look like?

### Takeaway
Published production diagrams come in six recurring forms:
1. A **feature outline map** of the whole game.
2. **Flow diagrams**, where agents or resources move from sources to sinks.
3. **Dependency graphs**, where a node cannot happen until its prerequisites have: puzzles, skills, unlocks.
4. **Interaction-rule matrices**, such as element × material.
5. **Layered architecture diagrams**, such as a director (macro) over a creature brain (micro).
6. **Timelines**, read like an orchestral score.

Almost none shows every system with every edge. Each answers one question, and whole-game views stay at feature-set level.

### Cited Findings
**1. Feature outline map**
- The SimCity overview was an outline of the whole game made of bold feature-set headings with almost no detail. It was used to lock scope and keep the team on course, and it later became the table of contents for everything else. Its sections included Selecting a Region, Trading with Neighbors, Simulating a City, Constructing Buildings, Harvesting and Manufacturing, and Global Markets. — [SimCity notes pp.7, 18, 24, 53, 66, 74](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- The simulation section was drawn small because Librande assumed it needed little design. He later says that assumption was very wrong. — [SimCity notes pp.24–25](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)

**2. Flow diagrams (agents, resources, economies)**
- **Vocabulary first, then layers.** The first agent page had one job: teach the team three words (source, agent, sink) and how they relate. Later pages typed the agents (shoppers, workers, commuters) and units (factories, stores, houses). Then came system flows, such as education lowering pollution and the garbage loop. — [SimCity notes pp.31–35](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **A whole-simulation *master plan* on one sheet**, so that all relationships were visible.
  - He likened it to a pachinko machine: agents enter at the top, bounce through the middle, and drain out the bottom when unhappy.
  - A legend used bar graphs for *relative* tuning (residents, tax, water/power, garbage), to be replaced with numbers later.
  - It was printed large on a plotter and as an 8.5×11 triptych for desks.
  - — [SimCity notes pp.37, 41–42, 51–52](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **Decoration must carry meaning.** The same trade flow drawn as plain boxes was fine for one-on-one work but ignored on a wall. Redrawn with isometric buildings and trucks on the arrows, it drew attention. He warns that anything added must reinforce the idea, and cites Tufte's *chartjunk*. — [SimCity notes pp.22–23](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **Single-agent flowcharts as debugging tools.** The sim decision flowchart made an over-complex algorithm visible. Simplifying it by splitting shoppers and workers into separate agents was easy because the problem area could be seen on the page. — [SimCity notes pp.38–39](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **Machinations (Joris Dormans)** is a formal diagram language for a game's internal economy. It draws resource flows so that feedback structures stand out, and the diagrams can be simulated. — [AAAI AIIDE paper page](https://ojs.aaai.org/index.php/AIIDE/article/view/12477)

**3. Dependency graphs**
- **Puzzle dependency charts** (Ron Gilbert, 10 Aug 2014):
  - Nodes are puzzles or puzzle steps; a line means the later node depends on the earlier one.
  - The chart is explicitly not a flowchart of play order, and leaves out story beats unless a puzzle needs them.
  - He lays them out left to right so they can run along an office wall, and designs backwards from goals.
  - Each act ends in a bottleneck. The chart's shape shows when a design is too linear or offers too many open choices.
  - Maniac Mansion, made without such charts, had dead ends and uneven flow. Monkey Island relied on them for every aspect of puzzle design.
  - Tools: project-scheduling software at Lucasfilm, Graphviz (DeathSpank) and OmniGraffle.
  - — [Grumpy Gamer](https://grumpygamer.com/puzzle_dependency_charts)
- **Shape heuristic:** a linear game gives a long, narrow chart and a non-linear one gives a wide chart. — [IFWiki: Puzzlon](https://ifwiki.org/Puzzlon)
- **Conflicting origin accounts:** a Gamasutra article (reprinted on GamerBoom) says Gilbert developed the technique on Maniac Mansion, and that Noah Falstein believes the original charts are lost — [GamerBoom reprint](https://gamerboom.com/archives/69319). Gilbert himself says Maniac Mansion lacked them — [Grumpy Gamer](https://grumpygamer.com/puzzle_dependency_charts).
- **Skill chains** (Dan Cook, 2007) map how a player masters skills across a game, with a Tetris example — [Lost Garden](https://lostgarden.com/2007/07/19/the-chemistry-of-game-design/). Raph Koster reads them as directed graphs: basic skills must be learned before complex ones — [Raph Koster, 23 Jul 2007](https://www.raphkoster.com/2007/07/23/cooking-up-chemistry/).
- **The SimCity unlock tree** for buildings and modules was built as black-and-white cards and string on a corkboard. It was abandoned for three reasons: re-routing string was tedious, the board sat outside the design area behind two artists, and much of the team never saw it. — [SimCity notes pp.58–59](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)

**4. Interaction rules and matrices**
- **Breath of the Wild's chemistry engine** (GDC 2017) reduces to three rules:
  - elements can change the state of materials;
  - elements can change each other;
  - materials cannot change each other.
  - — [GamesBeat write-up](https://gamesbeat.com/the-legend-of-zelda-breath-of-the-wild-makes-chemistry-just-as-important-as-physics/)
- The talk framed an action game as collision + movement + state. Physics covers the first two and chemistry covers state. — [Zelda Universe report](https://zeldauniverse.net/2017/03/09/zelda-team-discusses-breath-of-the-wilds-development-and-2d-zelda-prototype-at-gdc/)
- Librande's class × element ability matrix is another example — [SlideShare deck](https://www.slideshare.net/slideshow/one-pagedesigns/13460886). He warns against matrices that grow over-complex — [Game Developer 2023](https://www.gamedeveloper.com/design/video-one-page-designs).

**5. Layered architecture (a horror example)**
- **Alien: Isolation** splits its AI into two layers:
  - A **director** (macro) always knows where the player is, runs a *menace gauge* and periodically hints the alien toward the player.
  - The **alien** (micro) must find the player with its own senses.
  - Menace rises with walking distance, line of sight and motion-tracker proximity. At peak menace the director sends the alien to vents or neighbouring areas.
  - The alien's behaviour tree has 100+ nodes, about 30 of them top-level, with branches unlocked over time.
  - — [Game Developer, Tommy Thompson, 31 Oct 2017](https://www.gamedeveloper.com/design/the-perfect-organism-the-ai-of-alien-isolation)

**6. Timelines**
- The SimCity region timeline splits play into early, mid, late and end game. It uses two extreme cities, one mineral-rich and one water-rich, as bookends. — [SimCity notes pp.13–17](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- A later overview timeline is a branching structure at minute, hour and session scales. Librande likens it to an orchestral score that coordinates departments, which is effectively a discipline swimlane. Its guessed times later matched telemetry, partly because the team tuned against the chart. — [SimCity notes pp.64–65](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)

**Cross-industry dependency formats used for the same job**
- **Design Structure Matrix (DSM):**
  - A square matrix with the same elements on rows and columns, where a mark means a dependency.
  - Marks above the diagonal are forward flows; marks below are feedback.
  - Clustering reveals modules, and sequencing reduces cost and schedule risk.
  - It is described as a simple, concise way to represent a complex system while analysing parts in the context of the whole.
  - — [DSM Web: Introduction to DSM](https://dsmweb.org/introduction-to-dsm/)
- **C4 model notation rules for architecture diagrams:**
  - Every diagram has a title stating its type and scope, plus a key or legend.
  - Every element has an explicit type and a short description.
  - Every line is a single-direction relationship with a specific label, not a vague verb such as uses.
  - Colours are used consistently, and diagrams are checked for black-and-white printing and colour blindness.
  - Diagrams should explain themselves without a narrator.
  - — [C4 model: Notation](https://c4model.com/diagrams/notation)

### Inferences
- Production diagrams in these sources are answers to one question each:
  - what blocks what (Gilbert);
  - where things flow (SimCity, Machinations);
  - what interacts with what (Zelda rules, Librande's matrix);
  - who knows what (Alien: Isolation);
  - when things happen (timelines).

  None tries to show 50+ systems with all relations and all metadata on one sheet. Even Librande's whole-simulation master plan had to be explained one zoomed region at a time.
- **Possible mapping for FrontRooms** (my inference, not from sources):
  - progression, keys and exits suit a Gilbert-style dependency graph;
  - the threat/pursuer stack suits an Alien-style macro/micro layer diagram, since the Relay director with honest senses has the same split;
  - door, light and sound interactions suit a rules matrix;
  - the run arc suits a timeline.
- The habit of building vocabulary first and then layering detail (SimCity source → agent → sink) suggests the overview page should first teach FrontRooms' core nouns, then show how systems connect.

### Gaps
- I found no published whole-game system dependency map for a commercial horror title. The Alien: Isolation material is a secondary analysis, and its figures were not described in the text I could access.
- I did not access the original BotW slides, and the rule wording comes from press write-ups.
- I did not verify Looking Glass's Act/React (stimulus/response) system for Thief, a likely first-person interaction-matrix precedent. The Thief postmortem exists ([Game Developer](https://www.gamedeveloper.com/design/postmortem-i-thief-the-dark-project-i-)), but I did not check its contents.

## Q3. How do studios show decided vs open and built vs designed on one view, and how do they pair a system diagram with a status table, board or roadmap for review?

### Takeaway
In the sources, **structure and status are separate layers that are linked, not merged**:
- The design map stays stable and leads.
- Status lives in a spreadsheet, tracker or board that rolls up to the map's nodes.
- Decisions get their own small status vocabulary (proposed / accepted / superseded).
- Built-vs-designed uses milestone vocabulary (locked, feature complete, content complete).

Reviews (scoping meetings, milestone check-ins, greenlight gates) use an overview page that people mark up, with the detailed table kept as backing data.

### Cited Findings
**Librande: design document vs snapshot vs data**
- His magnetic board of buildings and modules turned out to be a *snapshot* of what had already been built rather than a design telling the team what to do. Snapshots are valuable for showing progress, but maintaining them is a producer's job. Design works best at the head of development. — [SimCity notes p.57](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- Data went into the 650-row spreadsheet (daily updates, sort/filter, revision control) — [p.61](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf). Scope at a glance went onto the monthly *decorated* printouts grouped into 12 categories — [pp.62–63](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf).
- **Scoping on paper.** The team brought printed resource-chain diagrams to scoping meetings and crossed out cuts in red marker. Cutting on the diagram keeps the whole design visible, while deleting rows in Excel makes each cut in isolation. The cut down to 10 resources was easy because they could see which ones were critical. — [SimCity notes pp.76–77](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
- **Marking values not yet decided precisely:** relative bar graphs in a legend stood in for numbers until tuning data existed. — [SimCity notes p.42](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)

**Tools that hang status on structure**
- **HacknPlan's Game Design Model:**
  - An editable tree of design elements (name, type, parent, dates, Markdown description). Tasks and user stories link to the elements.
  - Each element shows progress metrics that can include its children, so status rolls up the tree.
  - Work items can be filtered by board and stage. One view answers what is being built, how, and when.
  - Its stated rationale is that traditional GDDs quickly go out of date.
  - — [HacknPlan docs](https://hacknplan.com/docs/design-model/game-design-model)
- HacknPlan pitches planning by discipline, effectively swimlanes. — [hacknplan.com](https://hacknplan.com/)
- **Tufte on project-management graphics:**
  - Charts should be retrospective as well as prospective, showing the actual dates goals were met.
  - Annotate key points with to-do lists and costs.
  - Sort detail by its potential for problems, separating routine items from important ones.
  - He sketched a design with a context bar for the overview, linked detail beneath, and no grid lines.
  - For wall plans: integrate text, graphics, images, tables and sparklines; leave room for handwriting; make updating routine, frequent and easy (for example, 11×17 sheets).
  - — [Tufte notebook: Project Management Graphics (or Gantt Charts)](https://www.edwardtufte.com/notebook/project-management-graphics-or-gantt-charts/) (same thread at [?p=521](https://www.edwardtufte.com/?p=521))
- **Clinton Keith** brought Scrum (2003) and Lean/Kanban (2006) to game teams, and presents Kanban as giving predictability and transparency in complex production — [InfoQ talk](https://www.infoq.com/presentations/kanban-video-game-dev). His book's 2nd edition covers communicating a game's vision, features and progress — [InformIT](https://www.informit.com/store/agile-game-development-build-play-repeat-9780136204749).

**Decided vs open: decision status**
- **Architecture Decision Records** (Michael Nygard, 15 Nov 2011):
  - Each decision is a short numbered record: title, context, decision, status, consequences.
  - Status moves from proposed to accepted, and later to deprecated or superseded. Superseded records are kept and point to their replacement.
  - His argument: large documents are neither kept current nor read, while small modular records are.
  - — [Cognitect blog](https://www.cognitect.com/blog/2011/11/15/documenting-architecture-decisions)

**Built vs designed: milestone and lock vocabulary**
- **Vampire: Bloodlines 2** (31 Jan 2020): after reaching Alpha, the team began *locking* features and quests. Locked means the content is considered complete and content work has stopped, but it still needs full testing and review. The next target was Content Complete: all content final, with bug fixing and polish the priority. — [RPG Codex repost](https://rpgcodex.net/article.php?id=11321) of [bloodlines2.com dev diary #9](https://www.bloodlines2.com/en/-9-new-year-new-milestone)
- **Older vs live-service usage:** in boxed-product usage, beta meant content complete (alpha conventionally meant feature complete). A live-service view argues teams are never feature- or content-complete. — [GamesBrief, 2019](https://www.gamesbrief.com/2019/04/words-to-ban-3-alpha-and-beta/)
- **A Sony development-process talk** (Mark Lintott, 2014):
  - Gates run Prototype → Concept → Green Light → Pre-Production → Vertical Slice → Production → First Publishable → Alpha → Beta → Gold Master.
  - Each gate passed means more investment and less risk.
  - Green light is where the concept is sold, with animatics, an elevator pitch and an early grey-box demo.
  - — [SlideShare deck](https://www.slideshare.net/slideshow/digibury-sony-game-developemen-porcess-mark-linott/34714689)
- Hothead Games treats vertical-slice approval as normally the last chance to experiment before the game's direction is locked. — [Hothead: Our Process](https://www.hotheadgames.com/our-process)

**Review settings: design reviews, milestone reviews, pitch**
- Librande carried the grouped system list into every meeting so that one-system discussions kept the whole game in view ([p.63](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)). The Design Gallery room served press tours and onboarding ([p.87](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)).
- **Valve's Cabal process:**
  - Small cross-discipline groups owned the design of parts of Half-Life — [Valve Developer Community](https://developer.valvesoftware.com/wiki/Cabal_process).
  - For Half-Life 2, Valve moved to three nearly independent design teams plus art, sound and story teams — [GDC Vault: Valve's Design Process for Creating Half-Life 2](https://gdcvault.com/play/1013450/Valve-s-Design-Process-for).
- **Ubisoft's editorial group**, about 100 designers and producers in Paris, advised on titles. Its chief creative officer checked in on games at key milestones. A 2020 restructure split oversight across franchise VPs after criticism that the games felt alike. This comes from press reporting with anonymous sources. — [VGC, 2020](https://videogameschronicle.com/news/ubisoft-revamps-editorial-team)

**Pillars to systems**
- Chris Wade set five pillars for Sausage Sports Club at the start and checked every design question against them. — [Game Developer, 7 Jan 2019](https://gamedeveloper.com/design/designing-sausage-sports-club)
- Max Pears: ask whether each mechanic serves a pillar and consider cutting it if not, though off-pillar mechanics can deliberately create different emotions. — [Game Developer](https://www.gamedeveloper.com/design/design-pillars-the-core-of-your-game)

### Inferences
- The recurring pattern is: **put structure on the page, show status as light marks on its nodes, and keep details in a linked table or log.** None of the cited practices uses one table to carry structure and status equally.
- **Decided vs open** maps cleanly onto an ADR-style three-state status (proposed / accepted / superseded), shown on the map as a small marker numbered to a decision log.
- **Built vs designed** maps onto a short milestone ladder, for example designed → prototyped → locked → content-complete, shown as fill or line weight on each node. That keeps status as a quiet layer over the structure.
- **Owner** is producer-snapshot data in Librande's sense. It belongs in the backing table, or at most a small tag. It should not be a column that competes with dependencies for attention.
- **Pillars can sit above the system map as its top layer.** Every system node should trace up to a pillar, and nodes that do not are cut candidates. This is how the pillar articles describe pillars being used, but I found no published diagram that draws pillars → systems explicitly.

### Gaps
- I found **no published example of a studio sheet that combines a whole-game system graph with per-system status, owner and decision columns**. The closest analogues are HacknPlan's tree-plus-status (a tool), Librande's overview + spreadsheet + decorated list, and Tufte's wall plans.
- I found no public documentation of Riot, Blizzard or Ubisoft internal design-review page formats. The Ubisoft information is press reporting with anonymous sources.
- I did not find a public pillars → systems diagram from a shipped game.

## Q4. What information-design guidance applies when one page must show structure, status and decisions? (Including failure modes, and what tables and diagrams each lose)

### Takeaway
The guidance converges on five points:
1. **Overview first, then zoom and detail.**
2. **Layer and separate**: status is a quiet layer over the structure, not a second grid.
3. **Match the form to graph size and task.** Node-link diagrams suit small or sparse networks and path questions. Matrices suit large, dense networks.
4. **Label every edge and include a key.**
5. **Treat an unreadable diagram as a sign the design is too complex.**

The documented failures are: long tables or outlines that split relationships into fragments; heavy grids; snapshot boards mistaken for design; diagrams that go stale; and node-link *hairballs* past roughly 20 dense nodes.

### Cited Findings
**Tufte**
- *Envisioning Information* (1990) covers escaping flatland, micro/macro designs, layering and separation, small multiples, colour and information, and narratives. — [edwardtufte.com](https://www.edwardtufte.com/book/envisioning-information/)
- He applies layering and separation (the book's chapter 3) to busy line diagrams by graying down the line structure — [Tufte notebook: genealogical information](https://www.edwardtufte.com/notebook/genealogical-information/). He also says it would have made a complicated set of documentation drawings much easier to read — [Tufte notebook: documentation sketches](https://www.edwardtufte.com/notebook/documentation-sketches/).
- Micro/macro designs support local and global comparison at once, without the cost of switching context. This is Tufte as quoted in a secondary summary. — [Medium summary](https://medium.com/@blakewilliford/information-design-9-29-ee995efb584e)
- **Typical project charts, in Tufte's view:**
  - analytically thin;
  - dominated by heavy grids;
  - badly typeset, with unexplained acronyms;
  - task lists that copy the org chart.

  Screens are too small for an overview of a big project, so print it on a wall. — [Tufte notebook thread](https://www.edwardtufte.com/notebook/project-management-graphics-or-gantt-charts/)
- **On slideware:** it often lowers the analytical quality of presentations, and stock templates weaken verbal and spatial reasoning — [Tufte: The Cognitive Style of PowerPoint](https://www.edwardtufte.com/book/the-cognitive-style-of-powerpoint-pitching-out-corrupts-within-ebook/). The essay criticises forcing all content into a deep, single-path bullet hierarchy — [copy of the essay hosted by Univ. of Edinburgh](https://www.inf.ed.ac.uk/teaching/courses/pi/2016_2017/phil/tufte-powerpoint.pdf). Its case study is the Columbia shuttle's Boeing slide, where the critical caveat was buried in a crowded bullet list — [Tufte notebook](https://www.edwardtufte.com/notebook/columbia-accident-investigation-board-the-boeing-powerpoint-slide/).

**Overview first, and the scale limits of node-link diagrams**
- **Shneiderman's Visual Information-Seeking Mantra:** overview first, zoom and filter, then details on demand. The taxonomy adds three more tasks: relate, history and extract. — [Shneiderman 1996, UMD DRUM](https://drum.lib.umd.edu/handle/1903/5784)
- **Ghoniem, Fekete & Castagliola** (InfoVis 2004) tested synthetic graphs of 20, 50 and 100 nodes at densities 0.2, 0.4 and 0.6 — [LISN record](https://www.lisn.upsaclay.fr/publications/a-comparison-of-the-readability-of-graphs-using-node-link-and-matrix-based-representations/). As summarised by later authors:
  - node-link was better for connectivity tasks on small, sparse graphs;
  - matrices beat node-link on every task for large, dense graphs;
  - matrices did poorly on long-path tasks.
  - — [Okoe et al., GD 2017 (ar5iv)](https://ar5iv.labs.arxiv.org/html/1709.00293)
- The NodeTrix paper cites the same work: node-link diagrams struggle with basic tasks such as checking whether two nodes are linked, even at about 20 nodes. — [Henry, Fekete & McGuffin, arXiv](https://arxiv.org/pdf/0705.0599)
- **Okoe, Jianu & Kobourov** studied a sparse real network (258 nodes, density 0.016). Node-link was better for most connectivity tasks and for memorability. Matrices were better for estimating the number of clusters. — [ar5iv](https://ar5iv.labs.arxiv.org/html/1709.00293); [TVCG record](https://experts.arizona.edu/en/publications/node-link-or-adjacency-matrices-old-question-new-insights/)
- Course summary (Munzner): node-link for small networks, matrices for large ones, and matrices when the task does not involve topology. — [UBC lecture slides](https://www.cs.ubc.ca/~tmm/courses/531-16/lectures/lect7-8.pdf)
- The DSM is the systems-engineering matrix form of the same idea. Grouped rows show modules as blocks, and below-diagonal marks show feedback. — [DSM Web](https://dsmweb.org/introduction-to-dsm/)

**Notation hygiene**
- Title plus scope, key or legend, typed elements, labelled one-way arrows, consistent colour, and readable in black-and-white and by colour-blind viewers. — [C4 model: Notation](https://c4model.com/diagrams/notation)
- Decoration only where it carries meaning. — [SimCity notes p.23](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)

**Documented failure modes**
1. **Long documents go unread, and wikis hide relationships** — [SlideShare deck](https://www.slideshare.net/slideshow/one-pagedesigns/13460886); [Game Developer 2023](https://www.gamedeveloper.com/design/video-one-page-designs). Large documents are neither kept current nor read — [Nygard](https://www.cognitect.com/blog/2011/11/15/documenting-architecture-decisions).
2. **Spreadsheet scoping cuts each item in isolation**, without the whole in view. — [SimCity notes p.77](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
3. **A diagram too complex to read signals a design too complex to play.** SimCity shipped its city-to-city trading design and players struggled to understand it. Librande advises changing the design rather than re-laying out the chart. — [SimCity notes p.21](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
4. **Decoration that does not carry meaning** (chartjunk). — [p.23](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
5. **A status snapshot mistaken for a design document.** Boards that are physically costly to update, or hung where the team never sees them. — [pp.57–59](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf)
6. **Staleness.** Diagram quality fell after alpha — [p.80](https://www.stonetronix.com/gdc-2013/SimCity-OnePage.pdf). GDDs quickly go out of date — [HacknPlan docs](https://hacknplan.com/docs/design-model/game-design-model).
7. **Grid-dominated, analytically thin charts** organised by org chart. — [Tufte thread](https://www.edwardtufte.com/notebook/project-management-graphics-or-gantt-charts/)
8. **Deep bullet hierarchies that split reasoning into fragments.** — [Tufte essay copy](https://www.inf.ed.ac.uk/teaching/courses/pi/2016_2017/phil/tufte-powerpoint.pdf)
9. **Node-link *hairballs*** once a graph is large or dense. — [Okoe et al.](https://ar5iv.labs.arxiv.org/html/1709.00293); [NodeTrix](https://arxiv.org/pdf/0705.0599)
10. **No dependency view at all.** Gilbert ties Maniac Mansion's dead ends and bottlenecks to designing without charts. — [Grumpy Gamer](https://grumpygamer.com/puzzle_dependency_charts)

### Inferences
**What a pure table loses compared with a diagram**
- **Topology.**
  - In a table, a dependency is text in a cell. To trace a chain, find a bottleneck or spot a feedback loop, the reader has to rebuild the graph in their head row by row.
  - Node-link views are measurably better for connectivity and path questions on sparse networks (Okoe; Ghoniem).
  - Gilbert reads linearity and bottlenecks straight off a chart's *shape*. DSM marks show feedback at a glance.
- **Wholeness during decisions.** Librande's spreadsheet cuts happened one row at a time without the whole in view. Wikis and outlines hide relationships the same way (Librande; Tufte on bullet hierarchies).
- **Emphasis.** Every row has the same visual weight, so the core loop and a minor subsystem look equally important. A diagram can make the central image dominant, as Librande's tips say. The size of an area on a diagram can also be wrong, as his undersized simulation section shows.
- **Memorability and shared mental image.** Node-link views are better remembered (Okoe). Librande's scoping and onboarding relied on everyone seeing the same picture.

**What a diagram loses without a table**
- **Volume and precision.** SimCity needed a 650-row, daily-updated, sortable, revision-controlled spreadsheet for module data. Tuning numbers began as relative bars.
- **Status that stays true.** Once status is painted into a diagram it becomes a snapshot that must be maintained (Librande's magnetic board). Diagrams go stale after alpha. Linked trackers solve this by rolling status up from tasks (HacknPlan).
- **Decision history.** Who decided what, when, and what it superseded needs a log (ADR) that a picture cannot hold.
- **Scale.** Past about 20 dense nodes, node-link readability collapses. A matrix or table takes over for lookup tasks (Ghoniem; NodeTrix; Munzner).

**Implications for the FrontRooms 52-system overview** (my synthesis, not from sources)
1. **Overview page:** draw the 8 groups as 8 nodes, which is well under the ~20-node comfort zone, with combined edges between groups. Add one pillar or core-loop line on top, as a central image.
2. **Drill-down pages:** one page per group, all using the same visual grammar (Tufte's small multiples). Each group has about 6–7 systems, which is node-link territory.
3. **Cross-group coupling:** optionally, a 52×52 DSM ordered by group, so groups appear as diagonal blocks and off-block marks show coupling. This shows which systems touch which without a hairball.
4. **Status as a quiet layer:** use fill or line weight for built state (a 3–4 step ladder), a numbered marker for each open decision keyed to a decision log, and owner as a small tag or swimlane band.
5. **Keep the 52-row table as backing data or an appendix**, the way Librande's spreadsheet sat behind the wall. Do not make it the review artifact.
6. **For projection:** redraw for 16:9 rather than shrinking Letter sheets. Each slide should answer one question.

### Gaps
- I could not open Ghoniem et al.'s abstract directly (HAL blocked the request and Semantic Scholar rate-limited it). Their findings here come from Okoe et al.'s and the NodeTrix paper's summaries.
- I found no empirical study of how well *game-production* system diagrams work. The guidance combines practitioner accounts (Librande, Gilbert, Tufte's correspondents) with general visualization research.
- I could not verify the commonly attributed Tufte phrase 1 + 1 = 3 (from the layering chapter) on Tufte's own pages, so these notes do not use it.
