# Stairwell · sources ledger

One row per source used in `01_research.md`: what it is, the URL, when and how it was read, and its licence or status.

How each source was read:
- **read**: the primary text was opened and read.
- **[search]**: only a search-engine summary was read.
- **via**: a secondary source that quotes or summarises the primary one; the primary was not read.
- **on disk**: media or documents already in the project.
- **[memory]**: general knowledge of a well-known work; times not checked.

**No media was downloaded for this folder.** Wanted media is listed in `media_candidates.md` for Red's approval.

Side effects to report (text documents only, all outside the repo):
- **UBC 1991 OCR text.** The 2.3 MB OCR text of the UBC 1991 scan (CC0, archive.org) was fetched into the session scratchpad on 2026-10-22 at 06:52, so it could be searched: `scratchpad/stair_research/ubc1991_djvu.txt`.
- **BSS 120 text.** `scratchpad/stair_research/bss120.txt` (NBS BSS 120, public domain) was left there by the interrupted first attempt of this workflow (2026-10-21, 22:33). It was reused.
- **Cached PDFs.** WebFetch cached two PDFs in the session's `tool-results/` folder: the VA spec 05 51 00 (2023, 82 KB, unreadable as fetched) and NYC LL5 1973 (817 KB, text extracted with zlib).
- **Commons API.** Wikimedia Commons API metadata queries returned titles, sizes and licences only. No images were fetched.

---

## 1. Period codes and government documents (primary)

| ID | Source | URL | Read | Licence / status | Used for |
|---|---|---|---|---|---|
| S1 | International Conference of Building Officials, *Uniform Building Code*, **1991 edition**. Chapter 33 "Exits" (§§3301–3315, pp. 630–647), §1712 guardrails, §1807 high-rise, §3805 standpipes, §4306 fire assemblies, Appendix Ch. 1 §125 | https://archive.org/details/gov.law.icbo.building.1991 (OCR text: `/download/gov.law.icbo.building.1991/icbo.building.1991_djvu.txt`) | **read** (OCR text, searched and read by section), 2026-10-22 06:52–07:00 | CC0 1.0 (Public.Resource.Org upload; incorporated by reference, 24 CFR 200.925c) | every [read] number in §1.1, §1.4–1.6: rise/run, width, landings, headroom, handrails, guards, doors, panic hardware, enclosure ratings, glazing ≤ 100 in², hinges, closers, barrier, roof hatch, stair ID signs, exit signs, exit illumination, standpipe outlets, `ACCESS ONTO FLOOR THIS LEVEL`. OCR damage noted where it occurs (§3304(j) "7 inches", §4306(g)) |
| S2 | UBC Standard No. 33-2 "Stairway Identification" (named in S1 §3306(p) and the standards list p. 816) | in S1's companion volume (not in this scan) | not read | — | sign details (layout, sizes) still unknown |
| S3 | J. C. Archea, B. L. Collins, F. I. Stahl, *Guidelines for Stair Safety*, NBS Building Science Series 120, **May 1979** | https://nvlpubs.nist.gov/nistpubs/Legacy/BSS/nbsbuildingscience120.pdf (7.1 MB) | **read** (text: Table 8, §2.2.5, §2.4.8, §2.7.3, figure captions), 2026-10-22 | US government work, public domain | locked fire stairs trap users ("forced to descend… unintentional trip to the sub-basement"); illumination changes draw attention; residential survey (83 % < 10 fc, 26 % glare, 46 % ≥ 1 in irregularity, rails mean 31 in); wall paint pattern as a slope cue (Fig. 2.7.5) |
| S4 | New York City Local Law No. 5 of 1973 (fire safety in office buildings): C26-608.3 floor numbering signs, C26-608.4 stair identification, C26-608.5 stair re-entry signs, door-locking exceptions | https://www.nyc.gov/assets/buildings/local_laws/ll_0573.pdf (817 KB) | **read** (text extracted from the PDF's streams), 2026-10-22 | NYC public law | numerals ≥ 3 in on the stair side of the door; stair letters on the occupancy side; re-entry signs; stair-side locking allowed above the street floor with re-entry at intervals of four stories in tall office buildings |
| S5 | NISTIR 4399 (Collins, Dahir, Madrzykowski, Aug 1990) and NISTIR 4532 (Collins, Goodin, Mar 1991), with the code text they quote (NFPA 101-1988 §5-8/§5-10, UL 924-1989, IES 1987) | https://nvlpubs.nist.gov/nistpubs/Legacy/IR/nistir4399.pdf, https://nvlpubs.nist.gov/nistpubs/Legacy/IR/nistir4532.pdf | via `research/exit_sign/SOURCES.md` S1–S6 (read there 2026-10-03) | public domain | EXIT letter sizes and lamp rules; sign types of 1989–90; chevron grammar for C23's later signs |
| S6 | Building 877 "Install Emergency Light Units", May 22, 1986 (US Coast Guard; NARA via DPLA; TIFF scan) | https://commons.wikimedia.org/wiki/File:Building_877_Install_Emergency_Light_Units,_May_22,_1986_-_DPLA_-_669ae937f4933f012d083c80b8ab74de.tiff | metadata only (Commons API), 2026-10-22 | public domain (US government) | period drawing or photo candidate for the twin-head unit (`media_candidates.md` M7) |
| S7 | NFPA, *Life Safety Code Handbook*, 1988 edition | https://archive.org/details/lifesafetycodeha0000unse_f1o8 | **not read** (controlled lending) | in copyright | would confirm the NFPA 101-1988 stair numbers marked UNVERIFIED |
| S8 | BOCA, *The BOCA National Building Code/1990*, 11th edition | https://archive.org/details/bocanationalbuil0000vari_11thedi | **not read** (controlled lending) | in copyright | BOCA 1990 stair values (UNVERIFIED) |

## 2. Secondary and search results

| ID | Source | URL | Read | Used for |
|---|---|---|---|---|
| S9 | California Title 8 / Title 24 stair text following the older UBC: risers 4–7½ in, run ≥ 10 in; City of Everett checklist citing §3306 (34–38 in rails, 6 ft 8 in headroom, 1½–2 in grip) | https://www.dir.ca.gov/Title8/3231.html ; https://lfportal.everettwa.gov/WebLink/0/doc/678340/Page57.aspx | [search] 2026-10-22 | the older UBC values; agrees with S1 |
| S10 | NAHB Research Center / HUD report via firedoc.nist.gov: the 1992 BOCA was the first model code with 7/11 for **residential** stairs (BOCA allowed 8¼ in before) | https://firedoc.nist.gov/article/IncyXYQBWEcjUZEYfJAV | [search] | BOCA residential only; commercial 1990 values open |
| S11 | Palm Beach County Schools spec 05 51 00 "Metal Stairs" ("steel pan to receive concrete-fill stair treads and landings"); VA NCA spec 05 51 00 (2023) and other project specs that require the NAAMM *Metal Stairs Manual* | https://www.palmbeachschools.org/cms/lib/FL50010848/Centricity/Domain/388/05%2051%2000%20Metal%20Stairs.pdf ; https://www.wbdg.org/FFC/VA/VANCAS/VANCA055100.pdf | [search] (the VA PDF was fetched but could not be read) | the pan-and-concrete-fill stair type. Gauges and stringer sizes are ESTIMATE |
| S12 | Emergency lighting and panic hardware ledger reused from `research/room_visuals/SOURCES.md`: Wikipedia "Emergency light" (read 10-03), "Crash bar" (read), DMF Lighting on NFPA 101 §7.9.2.1 [search] | https://en.wikipedia.org/wiki/Emergency_light ; https://www.dmflighting.com/technical-bulletin-emergency-lighting-requirements/ | via `room_visuals/SOURCES.md` | twin-head battery units, 90 min, 1 / 0.1 fc, 40:1; panic bar history |
| S13 | OSHA 29 CFR 1910.144 (yellow for caution) | https://www.law.cornell.edu/cfr/text/29/1910.144 | via `room_visuals/SOURCES.md` (read there) | yellow nosing stripes as a caution marking |
| S14 | 16 CFR 1201.1 (wired glass in fire doors exempt) | https://www.law.cornell.edu/cfr/text/16/1201.1 | via `interactables/02_period_hardware.md` §6.1 (read there) | wired glass as the period fire-door lite |

## 3. Backrooms canon and games (reused ledgers, read there)

| ID | Source | URL | Read | Used for |
|---|---|---|---|---|
| S15 | Kane Pixels, *The Backrooms (Found Footage)*, 2022-01-07, 4:44–5:56 and 6:00–6:57 | https://www.youtube.com/watch?v=H4dGpz6cnHo | via `level_transitions/01_ip_research.md` §3.1 (times checked by eye on 10-03); frames on disk (`room_visuals/images/run_research_r05_kane_backofhouse.jpg`, `run_ref_kane_ff_0398_fire_exit.jpg`, ledger `room_visuals/SOURCES.md`) | the stair and push-bar door as a transition; the hard door cut |
| S16 | Backrooms Fandom Level 1 and Level 4 (CC BY-SA) | https://backrooms.fandom.com/wiki/Level_1 ; https://backrooms.fandom.com/wiki/Level_4 | via `level_transitions/01_ip_research.md` §6.2 and `office_and_film/03_ip_canon.md` §2.2 (read 10-02/03) | stairs into Level 1; office stairwells looping back |
| S17 | Backrooms wikidot Level 4 (CC BY-SA 3.0) | https://backrooms-wiki.wikidot.com/level-4 | via `office_and_film/03_ip_canon.md` §2.1 (read 10-02) | "exits are office-style stairwells" |
| S18 | *Inside the Backrooms* walkthrough (FlyyxGamer), 1:51:50–1:52:02; *POOLS* walkthrough (iStudioGamer), 54:42–54:56 | https://www.youtube.com/watch?v=mevBMJgn1sk ; https://www.youtube.com/watch?v=8D8SumQ0WlQ | via `level_transitions/01_ip_research.md` §7 | the stair as the joint between levels |
| S19 | *Escape the Backrooms* 1.0 launch trailer, Level ! frame at 94.6 s | https://www.youtube.com/watch?v=2ZLPWJ-vkBU | on disk `room_visuals/images/run_ref_etb_levelbang_0094.jpg` (ledger `room_visuals/SOURCES.md`) | EXIT as a chase goal (do not copy) |

## 4. Film, TV and games (this folder)

| ID | Source | URL | Read | Used for |
|---|---|---|---|---|
| S20 | *Jacob's Ladder* (1990): Wikipedia plot; Looper "The ending of Jacob's Ladder explained" | https://en.wikipedia.org/wiki/Jacob%27s_Ladder_(1990_film) ; https://looper.com/900960/the-ending-of-jacobs-ladder-explained | [search] 2026-10-22 | locked subway exit, stairs, forced worse route |
| S21 | *[REC]* (2007): IMDb (trailer 1:52), eCartelera (trailer 1:42, 2007-07-22) | https://www.imdb.com/title/tt1038988 ; https://www.ecartelera.com/peliculas/rec/ | [search] | trailer exists; the scene itself is [memory] |
| S22 | *Silent Hill 2* Historical Society stairs: Push Square remake walkthrough; PowerPyx ("Into the Abyss"); silenthillmemories.net (2001 walkthrough) | https://www.pushsquare.com/guides/silent-hill-2-historical-society-walkthrough-all-collectibles-strange-photos ; https://www.powerpyx.com/silent-hill-2-remake-silent-hill-historical-society-collectible-locations/ ; https://www.silenthillmemories.net/sh2/walkthrough_08_historical_society_en.htm | [search] | the long descent as a deeper layer |
| S23 | *P.T.* (2014): Wikipedia; Push Square guide | https://en.wikipedia.org/wiki/P.T._(video_game) ; https://www.pushsquare.com/news/2014/08/guide_how_to_complete_the_spooky_pt_demo_on_ps4 | [search] | the looping hallway and its last door; the short stair is [memory] |
| S24 | SCP-087 (Zaeyde), SCP Wiki | https://scp-wiki.wikidot.com/scp-087 | **read** 2026-10-22 | 38°, 13-step flights, semicircular landings about 3 m, about 1.5 flights visible. CC BY-SA 3.0: reference only, do not copy |
| S25 | *Vertigo* (1958), *Die Hard* (1988), *The Raid* (2011), *1408* (2007), *F.E.A.R.* (2005), *Severance* (2022) | — | [memory] | scene notes in `01_research.md` §2.2–2.3; check times before use |
| S26 | freesound 341176, klangfabrik, "stairwell door panic bar and slam" (CC0) | https://freesound.org/s/341176/ | via `DOOR_FOLEY_SEGMENTS.md` §3 and `briefs/sound.md` | a real steel stair door with crossbar, the one door where it is the right material |
| S31 | *The Exit 8* (Kotake Create, 2023): clips on disk; Steam page | `Research/week01/assets/clips/EX8_*`, `Research/week02/phosphor-narrative/clips/gn_exit8_*` (ledger `Research/week02/phosphor-narrative/SOURCES.md` rows 36–37: IGN PlayStation launch trailer https://www.youtube.com/watch?v=pDTFOTTlw7I) | on disk | a stair between loops; the number sign as progress |

## 5. Project files read (read-only)

- `Frontrooms3D/Documentation/system_design/systems.json` (2026-10-21g): N1–N5, C08, C22, C23 (open), C27.
- `Documentation/Takeover_2026-10-18/briefs/COMMON.md`, `briefs/map.md` (verified report, standing rules, Inbox), `briefs/sound.md` :83, `briefs/narrative.md`, `LANDINGS.md`.
- `~/FrontRoomsMapWork/notes/RESUME.md` (all 117 lines; no stairwell plan yet; Alpha order W1, then the exit minimum, then W2).
- `Frontrooms3D/Documentation/LEVEL_MODULE_SPEC.md` §1–4 (cell, wall, heights, door, window, keep-clear, ceiling, 192 m period); `MAP_GENERATION.md` (start area, tiers, Caught); `RELAY_PURSUIT_REDESIGN.md` :16; `VISUAL_CHAT_TASKS.md` :19–23; `DOOR_FOLEY_SEGMENTS.md` §3.
- Sidecars `Assets/Resources/Props/Models/Kit_DoorLeaf_Steel(.json)`, `Kit_DoorLeaf_SteelLite`, `Kit_DoorFrame_Steel`, `Kit_ExitDevice_Crossbar`, `Kit_ExitSign`, `Kit_DoorCloser_Body`, `Kit_EvacPlacard`, and the list of kits present.
- Research reused: `research/exit_sign/SOURCES.md`, `10_spec.md` :145–156, :463, :515; `research/interactables/02_period_hardware.md` §2, §5, §6, §7; `research/interactables/10_spec.md` :263, :402; `research/room_visuals/03_run_research.md` §3.3, §5; `11_run_dir_emergency_power.md`; `SOURCES.md`; `research/level_transitions/01_ip_research.md` §3, §6, §7; `media_candidates.md`; `research/office_and_film/03_ip_canon.md` §2; `research/placard/SOURCES.md` S13.
- Memory: `frontrooms-title-handoff`, `frontrooms-phosphor-narrative`.
- Figma research sections (IDs only, not opened in this stage): LEVEL TRANSITIONS 2831:6157 (P8 vertical move), ROOM VISUALS 2862:6157 (emergency power), DOORS + WINDOWS 2748:6099. Their content comes from the on-disk documents above.
