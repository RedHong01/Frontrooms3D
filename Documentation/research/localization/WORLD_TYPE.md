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
- **Width:** CJK lines run shorter than English, German and Russian longer. A word too long for its slot is first set narrower, 长体 down to 85 % width (as 1990 Chinese signs did), then given a shorter word signed by 叙事 (95 §4), which every sheet of that language then prints (one word for one door: de AUSGANG, 11-03); a smaller size only as the last resort, never a cut. Heros Cn Bold maps to the plain CJK bold.
- **One face per block (11-03):** the header (title and floor token, fitted once for the longest token, B10+), the alarm lines, the legend rows and the title block of a sheet are each set in one face for the whole block and every sheet of the language: Heros Bold while each line fits, else Heros Cn Bold for the whole block (Latin takes the condensed face before any narrowing). The lines of one row share one width and, as the last resort, one smaller size: one line is never narrower than its neighbour. A boxed block keeps its padding.
- **Breaks:** a row English sets on one line stays on one line while 85 % width fits it. Two lines break at a phrase mark (after . , ; : or before a parenthesis), as the English rows do, then by length, with no word alone on a line and no short word (an article or a preposition; in Korean one syllable, such as 그) hanging at a line end. CJK breaks after 。、，：； when both halves fit.
- **Punctuation:** full-width （）、，：。 as the tables write them.
- **Tokens stay:** B1, A-2, the stair letter A, sheet numbers, 11/89, N (95 §1). In a CJK line a Latin token keeps the slot's English face and weight (the W4 fob's A is Courier Prime, as every tag token), a quarter em from the CJK, on its baseline.

Added 11-03 22:3x from 游戏视觉's six zh-Hans questions (sheet `W/worldtext/art/sheets/worldtext_open_options_zh-Hans.jpg`):
- **R2 weight:** 宋体 (Noto Serif SC, standing in for 仿宋) takes the weight whose vertical stem matches the English slot's stem, one step heavier for 宋体's thin horizontals. Measured at CJK em = cap ÷ 0.70: Courier Prime Regular stem 0.127 cap → wght 600 (stem 0.104 em); Courier New Bold 0.170 cap → wght 700 (0.128 em). Labels, tabs, typed cards and tags stay 宋体 at that weight: 400 fades on the 60 × 11 mm tab at 0.35 m. 仿宋 has one light weight, so it is only for typed documents (W12), checked against Courier Prime's colour once downloaded.
- **Vertical strips:** where English runs a word along a narrow strip (binder spines, W17), CJK stands upright, 竖排, top to bottom, solid (1 em pitch), from the English start margin. The em is cap ÷ 0.70 unless the strip is narrower than 1.25 em; then the strip decides (the last step).
- **Placing a shorter word:** it keeps the English alignment where that ties it to another mark (THIS SIDE UP flush left with its arrows, KEEP DRY centred under its umbrella). Where the English word fills its field beside a mark (FRAGILE before the glass) or a plate's title rows (W16 ELECTRICAL / ROOM), the CJK word is centred on that word's or block's place, and a small line keeps the English small line's place.
- **Stencils:** a sprayed mark follows the English art. The crate words have no ties in English, so none in CJK. A die-cut legend (EXIT) is constructed as the English one is: one stroke width equal to the English stroke (19.05 mm), drawn on the CJK face's skeleton with square ends and the punch's fillet. Ties go where the stroke is thinnest, two opposite ties for a counter of at least 0.06 em² (口: top and bottom, in every option), and never through a character's defining join (全: through the foot of the left 撇, not the apex).
- **Kept boxes:** where a fixed opening cannot hold the rule size (EXIT, `research/exit_sign/80_world_text_options.md` §1), the last step sizes the word to the box. A life-safety sign keeps at least the Latin letter height and stroke it replaces, as Hong Kong's bilingual code does (FSD Code of Practice §5.10: Chinese not lower than the English, strokes at least 15 / 10 mm). 出口 meets it (143 mm, constructed at 19.05 mm); 安全出口 at 85 mm does not.

Readings confirmed or set 11-04 06:3x, from 游戏视觉's build of the calls above:
- **Height means ink.** A floor in letter height (the life-safety rule) is met by the CJK ink, not its em: 出口's em becomes 162.6 mm so the ink stands 152.4 mm, EXIT's letters.
- **Air at the sides (11-04 07:0x):** at the floor height a legend may not crowd its opening: its side margins stay at least 1.25 × its top and bottom ones. Narrow the skeleton (长体, constructed strokes unchanged) to get there, never below 85 %. 出口 at 100 % filled the opening to 14 mm of its sides and read heavier than EXIT's letters between their thin chevrons; built at 94.9 %, the widest 0.1 % step that keeps the floor (95.0 % measures 1.244 ×), its sides are 21.01 mm against 16.78 up and down, ink 277.98 × 152.45 mm (`research/exit_sign/sheets/exit_constructed_C_sideair_zh-Hans.jpg`, checked 11-04 07:4x). The floor is measured, never rounded: take the widest step that meets it.
- **Ties:** 0.30 × the stroke, at least 3 mm (5.7 mm at 19.05).
- **Constructed ends:** a free end keeps the face's own end, cut square; ends of one kind (one character, same axis and side, within 1 % of the em) are drawn to one length: 口's two feet, 出's 凵 tops.
- **Latin token in a CJK line:** the token at the English slot's cap; the gap is a quarter of the CJK em, advance to advance.
- **Vertical strip:** the column is centred on the strip (not on the English line's axis). Too long for its strip: 扁体 (characters set lower) down to 85 % height, then a shorter word signed by 叙事, then a smaller em; never a cut, and one column on a spine.
- **长体 with a token:** only the CJK run is narrowed (its quarter-em gap with it); the token keeps its face, width and size; at the last step (a smaller em) the token shrinks with its line, so it keeps its relation to the CJK.
- **Tokens in a vertical column:** one or two digits or Latin letters stand upright side by side in one em (縦中横: ja 第3四半期報告, ko 3분기); three or more turn 90° clockwise as one run, reading down; a word space (ko) is a half-em gap in the column.

EXIT in the Latin and Cyrillic languages (11-04 08:1x, this line; the call 80 §7 left open):
- **The face:** the words of 95 §5 (AUSGANG, SORTIE, SALIDA, SAÍDA, ВЫХОД) are cut in EXIT's own die face, extended: one stroke of H/8 square to the stroke, straight strokes cut square, diagonals cut level, round strokes on one centre, sharp outer corners, the punch's R 0.5 inside, no overshoot. Widths are FHWA Series B 2000 × 0.93 (the die face's E and T against Series B): D G N R S U В 0.40 H, O 0.43 H, A and Д 0.50 H. Generator `~/FrontRoomsVisualWork/exit_latin/dieface.swift` (exact arcs and booleans) and `legend.py` (holes in the schema of 游戏视觉's `exit_legend.py`).
- **Ties** (0.30 × the stroke, at least 3 mm): beside the stem where a bowl meets it (D, R, В, Ы, as the stencil's D and B), top and bottom on O, through A's crossbar, mid-counter on Д's top and base (beside its right stem it read as Д + I).
- **Size in the kept box** (95 §6 Q2): ink no taller than EXIT's (top and bottom at least 16.8 mm), sides at least 21.0 mm (the floor's 1.25 × 16.8, kept when a word stands lower), the skeleton narrowed to 85 % before the size drops. An accent or a descender counts in the ink. The floor binds a word that can meet it; none of these can and none has a shorter signed word (AUSGANG is already NOTAUSGANG's short form), so the size is the last step, as 安全出口 in option A.
- **Built** (`exit_legend_<code>.json`, sheet `images/exit_dieface_latin_cyrillic.jpg`, VL503): SAÍDA 131.4 mm (86 % of EXIT; its acute sets the height), SORTIE 126.0 (83 %), ВЫХОД 120.1 (79 %), SALIDA 116.4 (76 %), AUSGANG 89.6 (59 %). At 8 m on a 1080p desktop the four read as words at 10–11 px; AUSGANG at 7.8 px keeps only its shape (it reads at 3 m). Real long-word signs keep 6 in letters in a wider housing (Lumacell's SORTIE sign, 486 × 222 mm, source 6); a wider box for German is the only way back to EXIT's height. Red took it for German (next item); the other four stay in the kept box.
- **A wider box for German** (Red 11-04 10:1x, 平面视觉 chat: "加宽德语框"): AUSGANG stands at EXIT's letters (152.4 mm) and stroke (19.05), cut in the die face at its own width, 100 % as EXIT (the 85 % narrowing is a kept-box step; at 8 m it would close U's counter to 1.4 px against 2.0). Ink 521.25 × 152.4 mm, top and bottom 16.8, sides 21.4 (the ink + 21.0 a side, up to the whole mm): the opening is 564 × 186 and the housing 590 × 212, 1.7 × EXIT's, the English pan stretched in width only (frame, bead, seams, depth and height as they are). Its two lamps become long sources at 0.30 and 0.70 of the opening, their spread set so the end letters light at no less than EXIT's E (.51 of the brightest): the proof's 0.14 of the width gave .52 / .50; built, 游戏视觉's field model gave the last G .499 at 0.14, so the kit carries 0.15: ends .537 / .518, the middle five .85–1.00, max/min 1.93 (EXIT 1.94). The rule is the end-letter floor, the spread is whatever meets it. EXIT's two point lamps at their fractions would leave the ends at .22. The battery pair stays at EXIT's fractions (0.12 / 0.88), the ends glowing. Legend `exit_legend_de.json` from `exit_legend_dieface.py`'s `WIDE` set (it adds `openingMm` and `housingMm`; the other five legends rebuild byte-identical), sheet `images/exit_de_wide.jpg` (VL506): at 8 m it reads as a word at EXIT's 13.2 px. Built by 游戏视觉 11-04 13:43 (`research/exit_sign/sheets/exit_built_de_wide.jpg` once landed; W/worldtext/art/sheets): PASSED here 13:5x; its maps keep the English pixel size, so across the face and diffuser they carry 0.59 / 0.57 of EXIT's texels per mm (softer grain at 0.35 m only), LOD0 +37 % over the kit budget (wall 4780 tris), battery level .121 against EXIT's .166 with the ends glowing; all accepted.
- **A script that cannot hold EXIT's stroke** (11-04 09:0x): where the short word's stacked strokes close at 19.05 mm in the floor's 152.4 mm (their gaps under half the stroke), it keeps the height and takes the thinnest stroke that opens them to the face's own proportion, never under 10 mm (the Hong Kong code's reading for a CJK sign). Korean **출구** (叙事 signed the short form 08:52): 출 stacks six horizontals, so 14.5 mm (the ㄹ's gaps 8.5 mm, 0.59 × the stroke; at 19.05 they would be 3.5 mm). Drawn as 出口 on Noto Sans KR Bold's skeleton, square ends; ㅅ's legs single arcs landing level, as the face sweeps them; a free end the face aligns with a stem follows the constructed stem; no counters, no ties; 长体 96.0 %, sides 21.1 mm (`exit_dieface_ko.swift`, sheet `images/exit_dieface_ko.jpg`, VL505). The face decides per language: ja 出口 is built on Noto Sans JP (its 出 stands both lower feet, 11 % of the ink against SC); zh-Hant reuses zh-Hans (TC's 出口 = SC's).

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
- **Big type on MAIN** (the 7-row block headline and the 5-row second line, built from █ and the half blocks ▀ ▄, which C20 has since 11-03 22:25): C20's 5 × 7 and 4 × 5 block letters stand two cells a pixel, so a line holds 6 headline letters or 8 second-line characters. A language whose line runs longer sets it condensed, one column a pixel, as a halfblock band of the same shapes (de and es / pt-BR MITTEILUNGEN, MEMORANDOS and both second lines; fr's second line, NOTES stays wide). An accented capital is shortened at half-row steps with its accent inside the line's rows, as the VGA's CP850 capitals are (Í Ã Ë). ru draws its own capitals: the headline at two columns a pixel and one column apart (ЗАПИСКИ, 76 of 80), the second line 5 × 5 (four columns cannot hold Ж Ш Щ Ю). CJK and Hangul draw a pixel glyph with half-block cells: a 12-px glyph is 12 columns × 6 rows (the headline: at most 6 characters across 78 columns), a 10-px glyph is 10 × 5 (the second line: at most 7). Source: Fusion Pixel 12 px and 10 px (OFL 1.1, already on this Mac).
- **Atlas:** one per language, cut to the characters the terminal files use (as the UI fonts are cut to the tables). A 16 × 16 glyph fills its two cells edge to edge, as on the CJK DOS screens. The engine needs two things from 游戏视觉 and 交互设计: glyph codes past 255, and a wide glyph that spans two cells.
- **Downloads for Red's OK:** Unifont, Shinonome, Neo둥근모 (one ask, with sizes). Fusion Pixel is on disk.

## 4. Status

| Item | zh-Hans | Other languages |
|---|---|---|
| W1 evacuation plan, all 24 sheetA* variants | **In game 11-03 22:28** as 游戏视觉's patches (the room prints redrawn 20:28 included); art in `Tools/print/ink/art_from_graphic/placard/zh-Hans/` (`tp_gen … lang=zh-Hans`, English byte-identical) | **In game 11-03 22:28**, all eight as patches from the 20:37 art (`~/FrontRoomsVisualWork/placard_tutorial/world/langs/<code>/`; de prints AUSGANG; de and fr have their own B10+ title piece); ru on Roboto Condensed until PT Sans, then re-render and 游戏视觉 re-packs |
| W13 annunciator | **In game 11-03** (`Resources/WorldText/zh-Hans/…/Prop_AnnunciatorLabel_A.png`) | **all nine landed 11-03** |
| W3 key plate | **In game 11-03**, all nine (`station_paper/plate_lang.py`) | all nine |
| W10 business-hours plate | 信息设计's art (+ 游戏视觉's normal and smoothness): handed to 信息设计 11-03 | |
| W11 tent card | in the W12 atlas (cell tent_card) | |
| W12 station paper | needs R2 and R4 per script (downloads) | |
| W4 key tag, W16 plates, W17 door signs, labels and crate stencils | **In game 11-03 21:49** (游戏视觉's patches). Type calls 22:3x (§3, the block after the tokens): fob 宋体 600 + Courier Prime A, cards and tab 宋体 700, spines 竖排, 配电室 centred on the title rows (as landed), 易碎 centred on FRAGILE's place, no stencil ties; 游戏视觉 rebuilds the four changed copies (workflow wf_078001a3-640) | the same rules when each language comes |
| EXIT (W2) | **Red picked C 11-04 08:06** (`research/exit_sign/80_world_text_options.md` §0): the constructed 出口 at 19.05 mm, in game 08:1x; the plans and legend say it (08:39) | Latin and Cyrillic legends in EXIT's die face (§3, VL503): de 59 %, fr 83 %, es 76 %, pt-BR 86 %, ru 79 % of EXIT's height, in game 09:5x (world_text_exit_latin; built sheet passed, long-word ends dim with the box's two lamps). **de at 100 % in a wider housing** (Red 11-04 10:1x, §3, VL506): legend ready; 游戏视觉's build of the 590 × 212 kits started 11:5x. zh-Hant 出口 reuses zh-Hans (in game 08:5x); ja 出口 on Noto Sans JP and ko 출구 at 14.5 mm (VL505), built sheet passed 11:3x, in game 11:5x (world_text_exit_jako, with the zh-Hans / zh-Hant LOD1 plate fix). Plans: all four print the short word (09:00) |
| W18 terminal faces | face and set picked (§3b); the atlas after the download | **de, es, fr, pt-BR and ru in main 11-03** (`Resources/Terminal/Faces/`, `Tools/terminal_face/face_lang.py`), C20's ▀ ▄ 22:25; zh-Hant, ja and ko after the downloads |

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
4. FHWA, Standard Alphabets for Traffic Control Devices (2000), Series B spacing chart, 4 in capitals (E 1.521, D G N R S U 1.721, O 1.841, A 2.161, I .520 in): https://mutcd-dr.fhwa.dot.gov/SHSe/Alphabets.pdf [read 11-04]
5. Saskatchewan Office of the Fire Commissioner, Technical Advisory "Exit Doors" (2002): an internally lit EXIT at least 114 mm high with a 19 mm stroke: https://bac-lac.wayback.archive-it.org/web/20061105072815/http:/www.cps.gov.sk.ca/Safety/fire/pdf/TA_%20ExitDoors.pdf [read 11-04]
6. Lumacell LSRLMCSU "Sortie" all-metal exit sign: 6 in by 3/4 in letters, housing 19-1/8 × 8-3/4 in (tnb.ca lu_407 catsheet; Franklin Empire listing) [search summary 11-04, the sheet not opened]
