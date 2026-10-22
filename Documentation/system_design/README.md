# FrontRooms system design data

*Owner of the data: 系统设计 (system design). Owner of every visual made from it: 平面视觉 · 信息设计 (information design). Red, 2026-10-15: "这部分的任务应该交给平面视觉·信息设计来负责，你只提供系统本身的元数据和信息".*

## Files

| File | What it holds |
|---|---|
| `systems.json` | **8 groups** (id, name, purpose, map position hint); **54 systems** (id, group, name, status, status class, one-line summary, owner, needs, open decisions (`open_decisions`), closed note); **63 dependency edges** (source → target means the target needs the source; `cross_group` is flagged); **54 decisions**: C01–C31 (system, reference, question, a short recommendation, and an optional `rationale`; 7 still open on 2026-10-22s) and A01–A23, answered items that never had a C id (with `answer` and `date`); a **glossary** of 10 terms; **the landing order** (`landing_order_proposed`, confirmed by Red on 2026-10-21: `landing_order_confirmed`) |
| `views.json` | Data for the single-question views: **encounter timeline** (4 lanes, illustrative seconds); **knowledge layers** (who knows what, plus the flows between layers); **verbs × objects** (noise and Attention per action and object); **Relay states** (9 states and 10 transitions) |
| **Live view** | **System Atlas** (平面视觉 · 信息设计), generated from these files: https://claude.ai/artifact/FEPpCB6X8syv17wWXE1P3y. It belongs to Claude account A; from account B it reads as "not found", so it can only be republished from A (the 10-21 "unreadable" reports came from account B). On 2026-10-22 at 12:0x, 信息设计 on account B built data 22t locally but had not published it; it asked Red whether to wait for account A or to publish a second link. It is rebuilt by `~/FrontRoomsVisualWork/info_type/atlas/build.py`, which also lints the data. The A0 sheet (Figma 3239:126) is frozen, and will be kept only if Red wants a print version |

**2026-10-22 (Red's answers):**
- 22a–d: C23 (a LIT-locked sign over the EXIT now, chevron signs in its zone later), C24 = A (the route points at the floor's EXIT), A12 (doors swing away from the player; replaces A09 calls 2 and 3), and the landings of W1, the 33° fix and desktop look + E.
- 22e: the tutorial (Red in the 教程设计 chat: "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）"). New systems N6 Station and TV beacon and N7 Tutorial floor, 12 new edges, A13–A18. C15's key rule is replaced by one floor key per floor; the EXIT is locked; I6 becomes Desk monitors (still screens in the maze).

**2026-10-21c to f (Red's answers):**
- 21c–e: C27 = (d) and C18 = the EXAMINE defaults, from Red's "全部按推荐。" to 游戏视觉's own list (in the 游戏视觉 chat); D3, I4, D4, N2 and L6 follow tonight's landings.
- 21f: Red in the 系统设计 chat, "系统设计这边也全部按推荐（包括更正）". Answered: C01–C04, C06, C07, C10, C12–C17 and C22; new A09 (the doors + windows proposal defaults), A10 (fix the 33° aim bug) and A11 (door-shot details at 关卡设计's defaults); the landing order is confirmed. N1 is DESIGN now.
- Still open: C05, C08, C09, C11, C19, C20, C23, C24, C25, C26.

**2026-10-21a and b (re-opened 系统设计; b corrects the C10, C14, C15 and C22 costs with 关卡设计 and puts W1 first in the order):**
- real recommendations for C11, C14, C19, C22 and C25 (C14 and C22 carry a `rationale` with costs);
- C21 is answered (the motion family Red picked on 10-14); the one open part, the unlock-shot timing, is C27;
- D7 is BUILT_OFF, not BUILDING (the models are in main and unplaced); B2 notes the rules its core predates;
- `landing_order_proposed` is revised for the Alpha (exit, warnings bridge and small door fixes first). Red has not confirmed it.

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
