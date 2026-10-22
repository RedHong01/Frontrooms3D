# FrontRooms system design data

*Owner of the data: 系统设计 (system design). Owner of every visual made from it: 平面视觉 · 信息设计 (information design). Red, 2026-10-15: "这部分的任务应该交给平面视觉·信息设计来负责，你只提供系统本身的元数据和信息".*

## Files

| File | What it holds |
|---|---|
| `systems.json` | **8 groups** (id, name, purpose, map position hint); **52 systems** (id, group, name, status, status class, one-line summary, owner, needs, open decisions (`open_decisions`), closed note); **50 dependency edges** (source → target means the target needs the source; `cross_group` is flagged); **34 decisions**: C01–C26 open (system, reference, question, a short recommendation, and an optional `rationale`) and A01–A08 already answered (with `answer` and `date`); a **glossary** of 10 terms; **the proposed landing order** |
| `views.json` | Data for the single-question views: **encounter timeline** (4 lanes, illustrative seconds); **knowledge layers** (who knows what, plus the flows between layers); **verbs × objects** (noise and Attention per action and object); **Relay states** (9 states and 10 transitions) |
| **Live view** | **System Atlas** (平面视觉 · 信息设计), generated from these files: https://claude.ai/artifact/FEPpCB6X8syv17wWXE1P3y. It is rebuilt by `~/FrontRoomsVisualWork/info_type/atlas/build.py`, which also lints the data. The A0 sheet (Figma 3239:126) is frozen, and will be kept only if Red wants a print version |

**Since 2026-10-15b:**
- each timeline segment carries an intensity from 0 to 3, and each lane says what its intensity means (`intensity_note`);
- verbs × objects cells are objects (`text`, `effect`, `noise_m`, `attention`, `calls_relay`, `relay_only`);
- `systems.json` has a `glossary`;
- answered decisions stay, with `status`, `answer` and `date`.

**Words:** a *call* is only the director sending the Relay (see the glossary). Red’s open items are *decisions*, in the data and in every message.

**Status values:** LIVE, PARTLY_LIVE, BUILDING, BUILT_OFF, CLONE_ONLY, DESIGN, OPTION, NOT_DESIGNED. Each has a class: `in_game`, `built` or `not_built`.

**Sources:** `RELAY_PURSUIT_REDESIGN.md`, `research/computer_ui/*`, `research/computer_ui/00_interaction_inventory.md` (verified 10-08), the audit of 10-15, and Red's answers to date.

## Update protocol

- **Data changes:** 系统设计 edits these files. Examples: a system lands, a decision is answered, a dependency changes. `updated` is bumped and 信息设计 is told what changed.
- **Visual changes:** layout, type, grid, encoding and the choice of views belong to 信息设计 alone.
- **Who reads the files:** 信息设计 builds from them and never re-types data by hand.

## Why the A0 sheet (Figma 3239:126) reads poorly: 系统设计's analysis for 信息设计

The analysis is based on the research notes in `research/research_notes/游戏系统图 常见形式/`. The notes that matter most are `production_maps_and_review.md` (Librande, graph readability, Tufte) and `ai_pacing_horror.md`.

1. **One sheet tries to answer nine questions.**
   - Librande's SimCity practice split the work into three linked artefacts: an overview diagram, a sortable spreadsheet, and a grouped list carried into meetings. All three were cross-linked, not merged.
   - The published production diagrams in the notes each answer one question.
   - The A0 sheet merges A, A2, B, C, D, E, F, G and H onto one canvas, so there is no single entry point and no reading path.
2. **The same 52 systems appear three times.** They are the cards in A, the rows and columns in A2, and the rows in B.
   - The reader matches IDs across zones by eye: split attention, with no visual link between the three.
3. **The medium fights the viewing situation.**
   - Type at 1:1 reading size on an A0 canvas works on a printed wall. On a screen in Figma you get either the whole sheet, where the text is a few pixels tall, or a fragment, which loses its context.
   - Overview and detail are never readable at the same zoom.
4. **No pre-attentive hierarchy in the map.**
   - Each of the 52 cards carries four marks at similar weight: a status tag, an ID, a name and a decision badge.
   - Status is read from the word inside a 7 pt tag. Position, value and size are not used.
   - The node-link readability findings in the notes put the limit at around 20 dense nodes, and A shows 52 cards.
5. **The DSM is correct, but hard to read on screen.**
   - The cells are 10 pt, the labels are 7 pt, and the column IDs are rotated.
   - Finding one row-column pair needs a ruler or hover, and print has no hover.
6. **Too many weak distinctions.**
   - About five greys, four line weights (0.25 / 0.5 / 0.75 / 1 pt), dashed versus solid, and ink versus grey dots. Several pairs are too close to tell apart at a glance.
   - The key sits in the header, far from the marks it explains.
7. **Reading order and labels.**
   - The zone letters don't follow the layout. The left column runs A, H, D, then E, F with A2 and G inside it, and B and C sit on the right.
   - The zone questions are prose, and the cells use system jargon (G3 gate, W ≥ 42 m, Attention) that needs the design doc.
8. **The purpose is buried.**
   - The sheet exists so Red can decide (C01–C26), yet the decisions sit in the lower right and as small yellow badges elsewhere.

**Options for 信息设计** (yours to choose):
- **A linked set instead of one poster:**
  - an overview readable at screen size: 8 groups, status class shares, open-call counts, group-to-group links;
  - one detail sheet per group, all in the same layout (small multiples), each with its own decisions;
  - the ledger as a sortable table, not a poster;
  - one frame per single-question view (timeline, layers, verbs × objects, states).
- **Decisions as the primary layer** for review.
- **Status by position or value**, not by reading a word.
