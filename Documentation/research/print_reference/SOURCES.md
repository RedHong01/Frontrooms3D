# Sources: print reference (2026-10-15)

Third-party images are kept **out of this repo**; they live under `~/FrontRoomsVisualWork/reference/`. Images in `images/` here are FrontRooms' own renders.

## The reference case

| What | Detail |
|---|---|
| Work | "Press Any Key To Wake", by **Arthur**, series NO.14 |
| Where | rednote / 小红书, watermark "rednote ID: 8933017175" |
| Copies used | Six boards Red saved from the app on 2026-10-15, 768 × 1024 JPEG, watermarked: `~/FrontRoomsVisualWork/reference/press_any_key_to_wake/board_1.jpg` … `board_6.jpg` |
| Rights | The artwork is Arthur's. It is used here for study only, and crops appear only on the internal Figma boards PR01–PR12. For any reuse, or for the full-resolution files, ask the designer. |
| Originals | None found ([`ORIGINALS.md`](file:///Users/redwang/FrontRoomsVisualWork/reference/press_any_key_to_wake/ORIGINALS.md), outside the repo). The post needs a rednote login. Red can share the post link to check each image's stored size. Scraping the CDN was not attempted: the rednote terms forbid it. |
| Type identification | `~/FrontRoomsVisualWork/reference/press_any_key_to_wake/TYPE_USE.md`, with comparison sheets in `type_id/`. Its findings are summarised on PR11. |

**Board checksums (MD5)**

| Board | MD5 |
|---|---|
| 1 | 66325a38… |
| 2 | fa8a1da2… |
| 3 | 28e8671e… |
| 4 | ca2e1fe0… |
| 5 | 3fd100d6… |
| 6 | 7b1ca7ae… |

## Real period forms and signs (research track C)

The full file is `~/FrontRoomsVisualWork/reference/print_reference/research/C_documents_signage.md`. It is 630 lines and every claim carries a URL. These primary references come from its §11.

1. **SF 63 Memorandum of Call**, rev. Aug 1967, GPO 1969. A public-domain scan, measured for `PRINT_FORM_SYSTEM.md` §2: https://archive.org/details/poem00sher (page 4).
2. **OF 41 Routing and Transmittal Slip**, rev. 7-76.
   - GSA forms library: https://www.gsa.gov/forms-library/routing-and-transmittal-slip
   - Period scans are in the CIA reading room (records listed in C §4.1).
3. **ANSI Z35.1-1968**, Specifications for Accident Prevention Signs. A public-access copy, for reference only: https://law.resource.org/pub/us/cfr/ibr/002/ansi.z35.1.1968.pdf
4. **OSHA 29 CFR 1910.145 / .144 / .37**: https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.145
5. **AIGA / US DOT Symbol Signs** (1974, 1979). Public domain; Wikimedia Commons category "AIGA symbol signs"; report NTRL PB239352.
6. **GSA Printed Optional Forms catalogue** (2019): https://www.gsa.gov/system/files/Printed%20Optional%20Forms%202019.pdf
7. **ISO catalogue pages**, for dating: ISO 3864:1984, ISO 6309:1987, ISO 7001:1990, ISO 7010:2003 (C §5.1).
8. **NISTIR 4399** (Aug 1990): https://nvlpubs.nist.gov/nistpubs/Legacy/IR/nistir4399.pdf
9. **Backrooms community pages.** Wikidot and Fandom Level 0 are CC BY-SA. The reference's Manila Room label is copied from Fandom word for word, so FrontRooms reuses no wiki text.
10. **The original Backrooms photo set:** CC0 1.0 on archive.org, https://archive.org/details/backrooms-photos

## FrontRooms sources used

- **Logo.** Red's light-paper lockup `2235:44` (Week 1 deck), cloned onto the playtest form. The white logo component `2532:5100` is not used on paper. Its usage rules belong to 平面视觉 · 广告.
- **Base type.** P1 deck styles, file `0tCbAiVUlrPId3RWd9LRif`: Title, Statement, Body, Label and Meta.
- **Kit sheets.**
  - `research/interactables/threeview/HANDOFF_平面视觉.md`, plus `png_fix/`;
  - `research/exit_sign/threeview/index.json`, plus `png/`.
  - Both are 游戏视觉's renders.

## Research tracks A and B (done 2026-10-15)

These are summarised in [`IMMERSIVE_MATERIALS.md`](IMMERSIVE_MATERIALS.md). Each file lists its own sources, about 545 URLs in B alone.

| Track | File | Cases | Candidate media |
|---|---|---|---|
| A, games | `~/FrontRoomsVisualWork/reference/print_reference/research/A_games.md` | about 90; risk cases in §10 | 379 (about 433 MB) |
| B, film / TV / attractions | `~/FrontRoomsVisualWork/reference/print_reference/research/B_film_tv.md` | 50, plus RISK-01–20 | 237; 61 are priority A |

The case IDs and channel tags are shared with 平面视觉 · 广告.

## Research track D: Scantron / OMR answer sheets (done 2026-10-15)

This research is behind the answer-sheet edition of the playtest form (`PRINT_FORM_SYSTEM.md` §9) and the reader in `Tools/playtest_scan/`.
- **File:** `~/FrontRoomsVisualWork/reference/print_reference/research/D_scantron_omr.md`, about 470 lines. Every claim carries a URL and a status: read, measured or search.
- **Era lock:** 1990, newest design no later than 1993.
- **Downloads:** none. Its §(c) lists 18 candidate images with licences and sizes, for approval if a board needs them.

Primary sources used:
1. **Scan-Tron "Test scoring apparatus",** US 3,800,439, filed 1972, granted 1974: one control mark per row, with the start mark longest and the end mark mid-length. https://patents.google.com/patent/US3800439A/en
2. **NCS *Dossier Software Reference Manual*** (Oct 1980): defines the timing track, skunk marks, bias bar and response position, on a 6-per-inch read grid. https://archive.org/details/bitsavers_nationalCorSoftwareReferenceManualRel3.2Rev9198010_12263819
3. **Scantron Form 882 as used in April 1990,** an archive.org scan measured in the browser: rows ≈ 0.2 in apart, A–E ≈ 1/3 in apart, lozenges ≈ 0.21 × 0.05 in, green drop-out ink with black timing marks. Its "USE NO. 2 PENCIL ONLY / ERASE COMPLETELY TO CHANGE" panel is the model for the HOW TO MARK cell.
4. **NCS laser-overprint patents** US 4,857,715 and US 4,937,439 (1988–90): oversized printed ovals around a smaller sensing area, and corner QA marks.
5. **ANSI X3.86-1980 / FIPS PUB 85** (read and non-read inks) and **FIPS PUB 40** (1976; 0.25 in reference margin). https://archive.org/details/federalinformati8519nati ; https://archive.org/details/federalinformati40nati
6. **Remark Office OMR form-design guide:** targets 10–14 pt tall; anchors at least 0.25 in. https://remarksoftware.com/support/remark-office-omr-support/form-design/
7. **OMRChecker** (MIT): the no-code fallback reader. https://github.com/Udayraj123/OMRChecker
8. **ERIC ED325532** (1990): students fill name grids by reflex and then worry about anonymity, which is why the sheet has no name grid.

Unreadable: GPO Pub. 310.5, a scan without a text layer, so its drop-out ink tables stay unverified. Opening it in the browser pane raised a save dialog on Red's screen; nothing was saved.

## Downloads

**Tier 1 is downloaded.** Red approved it on 2026-10-15: 146 files, 148.7 MB, listed in `~/FrontRoomsVisualWork/reference/print_reference/DOWNLOAD_TIER1.tsv`.
- The files are in `…/media/`, with `MANIFEST.tsv` (URL, status, content type, size, SHA-256).
- They are used on the Figma boards RS01–RS06.
- Nothing else from the A, B or C candidate lists has been downloaded.
- B: all 61 priority-A files.
- A: 65 starred files from the ranked cases.
- C: 20 in-era references: the SF 63 scan, ANSI Z35.1 headers, AIGA/DOT symbols and the CC0 Backrooms photo.

Apart from the public-domain and CC items, everything is © its holder and is for internal reference only.
