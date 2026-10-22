# NOTE 2026-10-22 12:2x: station research photos (from 叙事设计) for the station board and Figma

叙事设计 downloaded 7 research stills on Red's OK in its chat (12:15: "全部按推荐（Q1–Q7、照片下载、LOBBY 改名）").
- **Originals:** `<作业根>/Research/week03/station/stills/` (outside the repo), with their own `SOURCES.md` for rights and credits.
- **Repo ledger:** `SOURCES.md` §D in this folder.
- **Use only for the research board / Figma frames.** They never go into the game, the repo or marketing.

**Use the board copies, not the originals:** `/Users/redwang/FrontRoomsVisualWork/station/board_stills/`.
- Two originals carry the photographer's GPS EXIF: `annunciator_2020.jpg` (叙事 flagged it) and `detex_station_chain.jpg` (not flagged).
- 游戏视觉 re-encoded those two with every metadata tag removed (JPEG q95, 4:4:4; no orientation tag was set, so nothing rotates). The other five are byte copies.

| File (board copy) | sha1 | Shows | Credit (put it on the frame) | What it backs in `30_look.md` |
|---|---|---|---|---|
| `faraday_panel.jpg` | 0cf53e16 | Fire-alarm control panel in an SF office building | Faraday Fire Watch IV, Commons, CC BY-SA 3.0 + GFDL (era unverified, photo 2008) | S12 annunciator: **cream / putty cabinet, not fire red** (`Prop_SteelPutty` #A69E88 holds). **Conduit enters from below** (see the look call below) |
| `detex_station_chain.jpg` | 33c51673 | Guard-tour watchclock station, numbered key on a chain | Paul Sableman, Commons, CC BY 2.0 | S35 watchclock station for the Phase 2 corridor trail |
| `detex_station_open.jpg` | cc123d3e | The same station type with the lid open | Commons, CC BY-SA 3.0 | S35 interior (hinged box) |
| `watchman_fd40a_1985.jpg` | 97cad427 | 4 in B&W pocket TV, Oct 1985 | Joe Haupt, Commons, CC BY-SA 2.0 (recheck uploader before any public use) | The guard's own-TV type (scale, not the station set) |
| `watchman_fd210.jpg` | 89e907f7 | Pocket TV, 1982–84 | Commons, GFDL / CC BY-SA 3.0 | Same |
| `annunciator_2020.jpg` | 76dd5291 | Modern annunciator, DC apartment building | Commons, CC0 | **Contrast only (post-era)**: label it "not 1990" on the board |
| `office_terminal_1980.jpg` | bb2bbc0b | 1980 office textures, Tampa | USF Digital Commons, Gandy collection, CC BY-NC 3.0 (**licence to recheck**) | Paper and surface wear; preview size only |

**Rules for the frames**
- Real brands (Faraday, Detex, Sony Watchman) may appear in the research photos. They never appear in game art, kit names or textures; the kits stay unbranded (era 1990, no brands).
- These photos lead the station research frames; diagrams only support them (research needs real sources).
- Frame per `SHOT_FORMAT.md` with `W/tools/shotframe/shotframe.py`. Credits go in the frame's header or source line, never as hand-drawn captions.

**Look call from `faraday_panel.jpg` (游戏视觉, 12:2x): conduit from below.**
- The S13 EMT run leaves the key plate's box downward, runs along the wall about 0.25 m below the annunciator, and enters the cabinet's bottom knock-out with a connector.
- It never enters from the top or the side. 1–2 runs, 3 straps, as in S13.
- The blockout and the 40 draft use this routing; it changes no budget.

**Not for visual work (from the same Red answer, recorded for completeness):**
- The scratch-throughs (M9) are cut.
- No T4 figure layer in v1.
- The wall glow (Q4, A vs B) is **NOT settled**: 叙事 is re-asking Red because its ask wrongly said glow A hides FLOW (VL350 disproved that). Keep `FrontRoomsPrintImporter.cs` (glow-ink) on hold.
