# World type by script (平面视觉, 2026-11-03)

**Red, 2026-11-03 16:43, 叙事设计 chat:** "目前切换成不同的语言，应该游戏里比如说对应的广告，平面视觉的type应该也需要做对应的改变，也就是所有的diegetic narratives 需要adapt 语言的变化。" Then 16:48: "全部按推荐" (checked in transcript b8f63700, Red's own turns).

The brief is `research/narrative/95_world_text_localization.md` §4 (叙事设计): each script gets the face that a 1990 sign shop or office working in that language would have used, in the same role as today's English face. Words are 叙事's and the tables are 设置菜单's. Faces, sizes and art are this line's (signs W5–W9, W14 and W15 belong to 平面视觉 · 信息设计). Texture swapping is 游戏视觉's.

## 1. The roles today (English)

| Role | English face | Where |
|---|---|---|
| R1 sign and plate | TeX Gyre Heros Bold, Regular and Cn Bold (Helvetica's design); engraved strips in Heros Cn Bold | evacuation plan W1, key plate W3, annunciator W13, business-hours plate W10, desk lettering W11 |
| R2 typed paper | Courier Prime | station paper W12 (log, key control, post orders), key tag W4 |
| R3 printed form | Heros heads over ruled grids | station paper register and phone list heads (W12) |
| R4 hand | Rock Salt | the visitors' columns of the register (W12) |
| R5 CRT | C20, an 8 × 16 bitmap face, ASCII | terminals W18, after the signs (95 §6 Q4) |

## 2. The map

Every CJK face below is in the project's localization sources (`~/FrontRoomsInteractionWork/settings/localization/fonts/src/`, downloaded with Red's OK on 10-30) or in `~/Library/Fonts`. All are SIL OFL 1.1. Text baked into a texture ships as pixels, and the OFL allows that. GPL fonts stay out of everything we ship.

| Script | R1 sign and plate | R2 typed paper | R3 printed form | R4 hand |
|---|---|---|---|---|
| Latin (de, fr, es, pt-BR) | Heros, as English | Courier Prime, as English | Heros | Rock Salt **if it has the accents** (check before W12) |
| Cyrillic (ru) | **PT Sans Bold / Narrow Bold** (ParaType, OFL, drawn from Soviet-era public grotesques) once Red allows the download; until then Roboto Condensed Bold, the face 广告's CG already uses for ru | a Cyrillic typewriter face [U, to research] | Roboto Condensed | [to pick] |
| 简体中文 | **黑体: Noto Sans SC 700 / 400** (Source Han Sans SC) | 仿宋, the face of typed official paper; Noto Serif SC stands in until Zhuque Fangsong (朱雀仿宋, OFL) is downloaded | 宋体 Noto Serif SC, heads in 黑体 | a ballpoint 行书 [to pick, needs a download] |
| 繁體中文 | 黑體 Noto Sans TC | 明體 Noto Serif TC | 明體, heads in 黑體 | [to pick] |
| 日本語 | ゴシック Noto Sans JP | 明朝 Noto Serif JP (a word-processor printout) | 明朝, heads in ゴシック | [to pick] |
| 한국어 | 고딕 Noto Sans KR | 명조 Noto Serif KR | 명조, heads in 고딕 | [to pick] |

Why: CJK, Hangul and Cyrillic signs of 1990 used gothic (sans) faces, and typed or printed text used Mincho or Song (95 §4). For Chinese typed paper, the government document standard still sets body text in 仿宋 (GB/T 9704-2012, "一般用3号仿宋体字"; its 1999 edition first fixed faces and sizes per element) [1]. Source Han Sans and Source Han Serif are modern drawings of the classic 黑体 and 宋体 structures. They are period-true in role, not by foundry.

## 3. Sizing and setting

These rules are shared with 平面视觉 · 信息设计 (its stair, alarm and plate signs) and 平面视觉 · 广告 (the TV's CG), so the plan, the EXIT, the signs and the TV speak with one voice (agreed 11-03).

- **Body size:** CJK body = the Latin cap ÷ 0.70, so the ideograph stands about 1.25× the cap it replaces, the 1990 bilingual sign habit.
- **Line pitch:** CJK needs about 1.3 em between lines; the Latin pitch × 1.18 gives it (7.6 → 9.0 mm at cap 5).
- **Tracking:** none for CJK; Latin keeps the slot's tracking.
- **Width:** CJK lines run shorter than English, German and Russian longer. A word too long for its slot is first set narrower, 长体 down to 85 % width (as 1990 Chinese signs did), then given a shorter word signed by 叙事 (95 §4); a smaller size only as the last resort, never a cut. Heros Cn Bold maps to the plain CJK bold.
- **Punctuation:** full-width （）、，：。 as the tables write them.
- **Tokens stay:** B1, A-2, the stair letter A, sheet numbers, 11/89, N (95 §1).

## 3b. Terminals (R5, W18): the per-language 1990 bitmap faces (Red's Q4, after the signs)

The terminal is an 80 × 25 grid of 9 × 16 cells drawn from 8 × 16 glyphs (C20, CP437 order; `research/computer_ui/50_build.md`). A 1990 PC in each language kept that grid. The CJK DOS of 1990 (CCDOS and UCDOS in mainland China, ETen in Taiwan, the NEC PC-9801 in Japan, Hangul DOS in Korea) drew Latin half-width at 8 × 16 and an ideograph or syllable full-width at 16 × 16, across **two cells**.

| Language | 1990 machine | Character set (write inside it) | Face for the atlas | Licence |
|---|---|---|---|---|
| zh-Hans | CCDOS / UCDOS | GB 2312: 6,763 hanzi, full-width punctuation | GNU Unifont 16 × 16 hanzi (the 宋体 bitmap of WenQuanYi lineage, close to HZK16) | OFL 1.1 (dual with GPL + font exception since 13.0.04) |
| zh-Hant | ETen 倚天 | Big5: 13,053 hanzi | GNU Unifont 16 × 16 (traditional forms) | OFL 1.1 |
| ja | PC-9801 | JIS X 0208 (6,355 kanji, kana); half-width katakana allowed | Shinonome 16 (東雲, k16 gothic or k16min mincho, picked on the tube) | public domain |
| ko | Hangul DOS (johab) | every modern syllable (johab composed all 11,172); no Hanja | Neo둥근모 (from the public-domain 1990s DOS 둥근모꼴 bitmap) | OFL 1.1 |
| ru | DOS CP866 | CP866: А–я, Ё ё | C20 extended with Cyrillic, drawn by 平面视觉 | ours |
| de, fr, es, pt-BR | DOS CP850 | CP850: every accent of the four, ß, ç, ñ, ã, õ, ¿, ¡ | C20 extended with the CP850 Latin, drawn by 平面视觉 | ours |

- **Widths for the words:** Latin and Cyrillic take one cell a character. CJK and Hangul take two, so an 80-cell row holds 40. Full-width punctuation (，。：「」) takes two cells. ASCII digits and letters stay half-width. Frames, box glyphs and row 25's key names stay CP437, half-width.
- **Big type on MAIN** (the 7-row block headline and the 5-row second line, built from █ and the half blocks ▀ ▄): Latin and Cyrillic keep the 5 × 7 and 4 × 5 block alphabets. CJK and Hangul draw a pixel glyph with half-block cells: a 12-px glyph is 12 columns × 6 rows (the headline: at most 6 characters across 78 columns), a 10-px glyph is 10 × 5 (the second line: at most 7). Source: Fusion Pixel 12 px and 10 px (OFL 1.1, already on this Mac).
- **Atlas:** one per language, cut to the characters the terminal files use (as the UI fonts are cut to the tables). A 16 × 16 glyph fills its two cells edge to edge, as on the CJK DOS screens. The engine needs two things from 游戏视觉 and 交互设计: glyph codes past 255, and a wide glyph that spans two cells.
- **Downloads for Red's OK:** Unifont, Shinonome, Neo둥근모 (one ask, with sizes). Fusion Pixel is on disk.

## 4. Status

| Item | zh-Hans | Other languages |
|---|---|---|
| W1 evacuation plan, all 24 sheetA* variants | **Art done 11-03** (`Tools/print/ink/art_from_graphic/placard/zh-Hans/`, `tp_gen … lang=zh-Hans`, English byte-identical). **In game:** Floor 1 room + room Phase 0 (print and glow) in `Resources/WorldText/zh-Hans/`. The landing sheets wait for 游戏视觉: their floor-token patches are placed for English glyphs | the same command once each face is set |
| W13 annunciator | **In game 11-03** (`Resources/WorldText/zh-Hans/…/Prop_AnnunciatorLabel_A.png`) | **all nine landed 11-03** |
| W3 key plate | **In game 11-03**, all nine (`station_paper/plate_lang.py`) | all nine |
| W10 business-hours plate | 信息设计's art (+ 游戏视觉's normal and smoothness): handed to 信息设计 11-03 | |
| W11 tent card | in the W12 atlas (cell tent_card) | |
| W12 station paper | needs R2 and R4 per script (downloads) | |
| W4 key tag (STAIR A) | 游戏视觉's generator, my spec | |
| W18 terminal faces | faces and sets picked (§3b), atlases after the downloads | |

## 5. Open

- Words for 叙事设计 (raised in the placard README): `DN` → 下; 您所在位置 vs 你在这里 for YOU ARE HERE.
- Downloads asked of Red 11-03 (one batch, 18.5 MB, all OFL or public domain; kept in `~/FrontRoomsVisualWork/fonts_world/`, outside the repo; only baked pixels enter it):
  - PT_Sans-Web-Bold.ttf 470 KB, PT_Sans-Web-Regular.ttf 443 KB, PT_Sans-Narrow-Web-Bold.ttf 488 KB (github.com/google/fonts, ofl/ptsans*): ru signs for this line, 信息设计 and 广告;
  - ZhuqueFangsong-v0.212.zip 5.7 MB (github.com/TrionesType/zhuque releases): zh-Hans typed paper (W12);
  - LongCang-Regular.ttf 5.2 MB (github.com/google/fonts, ofl/longcang): the zh-Hans register's handwriting (W12);
  - unifont-17.0.03.otf 5.3 MB (unifoundry.com): zh-Hans and zh-Hant terminals;
  - shinonome-0.9.11-src.tar.bz2 749 KB (openlab.ring.gr.jp/efont/dist/shinonome): ja terminal;
  - neodgm.ttf 651 KB (github.com/neodgm/neodgm releases v1.601): ko terminal.
- Later batch, when those languages come: hand faces for zh-Hant, ja, ko and ru, and a Cyrillic typewriter face.

## Sources

1. GB/T 9704-2012 《党政机关公文格式》 §7, as quoted by the Ministry pages found 2026-11-03 (search result summary, not the standard's own text): https://dbxb.bjtu.edu.cn/docs/20240426144834558971.pdf ; the 1999 edition's foreword (adds faces and sizes per element): https://rsj.pds.gov.cn/upload/files/2017/8/1611145904.doc [search]
2. Zhuque Fangsong, OFL 1.1, a technical preview (TrionesType): https://raw.githubusercontent.com/TrionesType/zhuque/main/docs/README-en.md [search]
3. Source Han Sans / Noto Sans CJK, OFL 1.1: the project's localization README §3 (`research/localization/README.md`).
