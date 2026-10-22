# Room visuals: sources ledger

One row per source used in this folder: URL, when it was read, licence or status, and how it was read.
- **read** = the page was opened and read.
- **[search]** = only a search-engine summary was read; the page itself was not opened.
- **on disk** = media already on disk; no new download.

Nothing in this folder was downloaded for this workflow. Wanted media is in `media_candidates.md`.
Other steps of this workflow may append their own sections below.

---

## 03 Run! research (`03_run_research.md`), accessed 2026-10-03

### Video frames (on disk)

The raw trailers were downloaded on 2026-10-02 with Red's approval for the IP research (official uploads, video only). They sat in that session's scratchpad (`…/658709b9-…/scratchpad/src1080/`). **That scratchpad was wiped by the 2026-10-05 reboot (`/private/tmp`), so the raw trailers are gone.** The frames cut from them survive in `images/` (`run_ref_*`, `run_research_r0*`); re-cutting needs a new download, which needs Red's approval. Their ledger is `Research/week02/ip-research/SOURCES.md`. Frames were cut with ffmpeg at the times below.

| Used in | Source | URL | Times (s) | Licence / status |
|---|---|---|---|---|
| `images/run_research_r01`, `r02`, `r03` (top), `r07`, `run_ref_etb_levelbang_0094.jpg` | Escape the Backrooms, "Official 1.0 Launch Trailer" (Secret Mode) | https://www.youtube.com/watch?v=2ZLPWJ-vkBU | 94.6, 95.2, 95.6, 96.6, 97.1, 97.5 | on disk; © the publisher; research use |
| `images/run_research_r05`, `r06`, `r07`, `run_ref_kane_ff_*.jpg` | Kane Pixels, "The Backrooms (Found Footage)" | https://www.youtube.com/watch?v=H4dGpz6cnHo | 368–416 (back of house), 487.5, 489, 506, 509 (flight) | on disk; © Kane Parsons; research use |
| `images/run_research_r07`, `run_ref_a24_trailer_0027_run.jpg` | A24, "Backrooms" Official Trailer HD | https://www.youtube.com/watch?v=0HjdiohVOik | 27 | on disk; © A24; research use |
| `images/run_research_r07`, `run_ref_backrooms1998_0076_red.jpg` | The Backrooms 1998, "Found Footage Official Game Trailer" (Steelkrill Studio) | https://www.youtube.com/watch?v=1wA1VLDJMQI | 76, 144 | on disk; © Steelkrill Studio; research use |
| `images/run_research_r03` (bottom), `run_ref_darkdeception_chase.jpg` | Dark Deception, "Enhanced Trailer" (Glowstick Entertainment), clip `Research/week01/assets/clips/DD_3_chase.*` (0:35–0:39) | **URL not logged in week 1** (see `media_candidates.md` C1) | clip 0.3, 1.3, 3.5 | on disk; © Glowstick Entertainment; research use |
| reference only | Escape the Backrooms clip `Research/week01/assets/clips/ETB_3_run.*` | same 1.0 trailer as above, 1:34–1:39 (per `Research/week01/assets/clips/for-slides/INSERT_VIDEOS_INSTRUCTIONS.md`) | — | on disk. This row also fills the missing week 1 ledger entry for ETB_3_run |
| `images/run_research_r01`, `r02` (bottom) | our own captures `images/room_title-run_wide.jpg`, `room_title-run_sign.jpg` | `04_captures.md` | — | ours |

### Photos (on disk)

| File | Source | Licence |
|---|---|---|
| `Research/week02/phosphor-narrative/stills/gn_tritium_exit_sign.jpg` | Wikimedia Commons, *Tritium-exit-sign.jpg* (Gazebo) | CC BY-SA 4.0 (ledger: `research/wallpaper_motion/30_narrative_phosphor.md`) |
| `Research/week02/phosphor-narrative/stills/gn_photoluminescent_exit_sign.jpg` | Wikimedia Commons, *Photoluminescent Exit Sign.jpg* (modern) | CC BY-SA 4.0 (same ledger) |
| `Research/week02/phosphor-narrative/stills/gn_scandinavian_star_1990-04.jpg` | Wikimedia Commons, *MS Scandinavian Star 001.jpg* (Terje Fredh / Sjöhistoriska museet) | CC BY-SA 4.0 (same ledger) |

### Canon and game guides

| Source | URL | How read | Licence / status |
|---|---|---|---|
| The Backrooms Wiki (Fandom), "Level !" | https://backrooms.fandom.com/wiki/Level_! | [search]; WebFetch refused (HTTP 402) | Fandom text is CC BY-SA 3.0 |
| Backrooms Wiki (Wikidot): no "Level !" page found at `level-run-for-your-life` or `level-run` (HTTP 404) | https://backrooms-wiki.wikidot.com/ | read (404) | — |
| A-Sync Backrooms Investigation wiki, "level run" (fan copy) | https://a-syncbackroomsinvestigationwiki.webador.es/level-run | read | "© 2023" notice; secondary source |
| Gamer Journalist, "How to escape Run For Your Life in Escape the Backrooms" (Aidan Lambourne, 2022-08-22) | https://gamerjournalist.com/how-to-escape-run-for-your-life-in-escape-the-backrooms/ | read | © publisher |
| gameplay.tips, "Escape the Backrooms – All Levels Guide" (2023-11-13) | https://gameplay.tips/guides/escape-the-backrooms-all-levels-guide.html | read | © publisher |

### Real 1990 references

| Source | URL | How read | Note |
|---|---|---|---|
| Wikipedia, "Exit sign" | https://en.wikipedia.org/wiki/Exit_sign | read | incandescent signs; red vs green in the US; city rules |
| Wikipedia, "Emergency light" | https://en.wikipedia.org/wiki/Emergency_light | read | battery units, adjustable heads, 90 min, 1 fc average |
| Wikipedia, "Fire alarm notification appliance" | https://en.wikipedia.org/wiki/Fire_alarm_notification_appliance | read | steady sound before 1996; incandescent flashers; ADA 1990 strobes; temporal-three 1996 |
| Wikipedia, "Crash bar" | https://en.wikipedia.org/wiki/Crash_bar | read | 1892 patent; Iroquois 1903; Von Duprin 1908; alarmed bars |
| Wikipedia, "Electromagnetic door holder" | https://en.wikipedia.org/wiki/Electromagnetic_door_holder | [search] | release on alarm; door closes by itself |
| DMF Lighting, technical bulletin on NFPA 101 §7.9.2.1 (2018 edition) | https://www.dmflighting.com/technical-bulletin-emergency-lighting-requirements/ | [search] | 1 / 0.1 fc, 0.6 / 0.06 fc after 90 min, 40 : 1. 1990 wording UNVERIFIED |
| MeyerFire, "How occupants know where exits are located" (NFPA 101 §7.10) | https://meyerfire.com/university/how-occupants-know-where-exits-are-located | [search] | 100 ft (30 m) to the nearest sign; 6 in letters, 3/4 in strokes |
| Allegion, "Decoded: Double-Egress Pairs in a Health Care Occupancy" | https://www.forconstructionpros.com/home/press-release/21330760/allegion-decoded-doubleegress-pairs-in-a-health-care-occupancy | [search] | cross-corridor door pairs in 8 ft corridors |
| ICC, Egress Work Group report, meeting 4 | https://www.iccsafe.org/wp-content/uploads/Egress_WG_Report-Meeting-4.pdf | [search] | 8 ft health-care corridors; smoke compartments, 200 ft travel to a smoke-barrier door |
| EC&M, "Strobe code compliance 101" | https://www.ecmag.com/section/integrated-systems/strobe-code-compliance-101-visible-appliance-requirements | [search] | more than two strobes in view must be synchronised |
| NKY Tribune, Keven Moore on strobes and epilepsy (2021) | https://www.nkytribune.com/2021/08/keven-moore-there-are-risks-related-to-fire-alarms-with-strobe-lights-those-with-epilepsy-beware/ | [search] | early-1990s strobes and seizures |
| OSHA 29 CFR 1910.144, Safety color code | https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.144 | read | red = fire equipment, danger, stop; yellow = caution; 1974 |
| `research/interactables/06_period_windows.md` §2.3 | (ours) | read | US wired-glass production ended after a 1992 ruling |
| `research/wallpaper_motion/30_narrative_phosphor.md` §3 | (ours) | read | Scandinavian Star 1990; NFPA self-tests; OSHA "Not an Exit" 1974 |

### Design, readability and safety

| Source | URL | How read | Note |
|---|---|---|---|
| The Level Design Book, "Lighting" (Robert Yang) | https://book.leveldesignbook.com/process/lighting | read | critical-path lighting; lit doorways show flow (Magnar Jenssen) |
| GDC 2018, "Level Design Workshop: Invisible Intuition" (David Shaver, Robert Yang) | https://gdcvault.com/play/1025179/Level-Design-Workshop-Invisible-Intuition | [search] | blockmesh and lighting to guide players; talk not watched |
| EA, "Follow the red: Runner's Vision in Mirror's Edge Catalyst" | https://www.ea.com/news/runners-vision-in-mirrors-edge-catalyst | [search] | red objects as the route |
| Wikipedia, "Mirror's Edge" | https://en.wikipedia.org/wiki/Mirror%27s_Edge | [search] | Runner Vision |
| Valve Developer Wiki, `prop_car_alarm` | https://developer.valvesoftware.com/wiki/Prop_car_alarm | [search] | alarm car with blinking glass |
| W3C, Understanding WCAG 2.1 SC 2.3.1 | https://www.w3.org/WAI/WCAG21/Understanding/three-flashes-or-below-threshold.html | read | 3 flashes/s; red flash = R/(R+G+B) ≥ 0.8; area limit |
| PMC, "A Global Perspective of Color Vision Deficiency" | https://www.ncbi.nlm.nih.gov/pmc/articles/PMC12385717/ | [search] | about 8 % of men, mostly red-green |

### Figma (read only)

| Node | What |
|---|---|
| 2245:9 (DESIGN PLAN DP01, file 0tCbAiVUlrPId3RWd9LRif) | "train of rooms" Lobby, Lobby, Shift, Office, Run, Next |
| 2235:735 (Week 1 slide 09 "Different rooms, different rules") | "Level ! · Run: A red corridor. The hunter behind. Nothing to read." |

---

## 30 Figma section FRONTROOMS · ROOM VISUALS (`30_figma.md`), 2026-10-08

No new media. Every image in the section is either our own render or on-disk media that already has a row in a ledger:

| Image in the section | Ledger row |
|---|---|
| `ETB_3_run`, `DD_3_chase`, `run_ref_*` (ETB 1.0 trailer, Kane Found Footage, A24 trailer, Backrooms 1998) | §03 above (video frames); trailers in `Research/week02/ip-research/SOURCES.md` |
| `gn_tritium_exit_sign` (Commons, Gazebo, CC BY-SA 4.0) | `Research/week02/phosphor-narrative/SOURCES.md` and §03 above |
| `gn_scandinavian_star_1990-04` (copied from GLOW INK `2471:3804`; Commons, MS Scandinavian Star 001) | `Research/week02/phosphor-narrative/SOURCES.md` |
| `fig-2312-852`, `fig-2349-852` (copied from IP RESEARCH and REAL vs MODEL) | the ledgers of those sections: `Research/week02/ip-research/SOURCES.md`, `Research/week02/kit-references/` |
| all `room*`, `run_dir_*`, `run_research_*`, `run_*_matte`, `11r2_*`, `es_*`, `q16_*`, `d_03_*`, `01_rep_L0`, `g10r3_*`, `20_*`, `04_b_*`, `db_*`, `office_round2`, other `fig-*` | our own renders or diagrams; no third-party source |

The three `media:` placeholders on RV24 are candidates A4, A5 and A9 in `media_candidates.md` (not downloaded).

---

## Fix stage, 2026-10-14

No new media. Nothing was downloaded.

- New images in this stage are our own renders and captures: `room1014_*`, `run_dir_*_probes`, `run_dir_*_noprobe`, `run_dir_*_c4k*`, `run_floor_*`, `run_fix_*`.
- Figma RV17 now shows our own 14 Oct frames instead of `es_b08_family_lineup`, our render with baked labels.
- **`DD_3_chase` still has no URL (L5).** A web search on 2026-10-14 ("Dark Deception Enhanced Trailer Glowstick Entertainment youtube", [search]) found no "Enhanced Trailer" upload. It did find:
  - the IMDb external-sites page for Dark Deception (https://imdb.com/title/tt9661238/externalsites), which lists "Dark Deception Trailer #1";
  - a Steam thread about a Chapter 3 trailer (https://steamcommunity.com/app/332950/discussions/0/1639789306545652483).

  Neither was opened, and neither is confirmed as the clip's source. The row stays open as media candidate C1: Red or the week 1 chat should log the URL they used on 2026-09-24.
