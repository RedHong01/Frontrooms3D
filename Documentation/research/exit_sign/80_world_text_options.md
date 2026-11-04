# EXIT sign in another language: options and the build (world text, 简体中文 first)

Line 游戏视觉, world-text workflow, stage EXIT (account B, under the 游戏视觉 session local_a9a1dce2), 2026-11-03 20:24–21:2x. Status: **options rendered, WAIT-RED; the recommended option is built and packaged, nothing landed in main.**
Revision (account G, under the 游戏视觉 session local_1fc9f09c, 2026-11-03 23:12–23:34, sheet and doc 11-04 05:5x (the machine was overloaded in between), after 平面视觉's type calls of 22:31 in `research/localization/WORLD_TYPE.md` §3): **C's legend is now constructed like EXIT's die face, not traced (§3b); all four kits and the seven map patches rebuilt, rendered against the English and the traced C (§4b); the apply package re-packed with it 11-04 06:04 (§7); still WAIT-RED, nothing landed.**
Correction (account G, same session, 2026-11-04 06:12–06:5x, after 平面视觉's readings of 11-04 06:3x in `WORLD_TYPE.md` §3, "Readings confirmed or set 11-04"): **C's ink now stands EXIT's 152.4 mm (em 162.6 mm) and ends of one kind are drawn to one length (口's two feet equal); legend, kits, maps and swap check redone (§3b), the changed rows rendered (§4b), the package re-packed 11-04 06:47 and again 06:55 over main's newer manifest (§7); still WAIT-RED, nothing landed.**
Side air (account G, same session, 2026-11-04 07:01–07:2x, after 平面视觉's rule "Air at the sides" of 11-04 07:0x in `WORLD_TYPE.md` §3): **C's skeleton is narrowed to 长体 94.9 % with the stroke, ties and feet unchanged, so its sides stand 21.0 mm, 1.25 × the 16.8 mm top and bottom; legend, kits, maps and swap check redone (§3b), the changed rows rendered (§4b), the package re-packed 11-04 07:28 over main's newer manifest (§7); still WAIT-RED, nothing landed.**

**Red's ask (2026-11-03, 叙事设计 chat, verified there):** "目前切换成不同的语言，应该游戏里比如说对应的广告，平面视觉的type应该也需要做对应的改变，也就是所有的diegetic narratives 需要adapt 语言的变化。" Then "全部按推荐" on `research/narrative/95_world_text_localization.md` §6. That doc's Q2 (a), now decided, says the EXIT is translated (`安全出口`, `非常口`, `AUSGANG`) and "The red colour, the box and the light stay."

What this stage did: real-source research (web pages read, nothing downloaded), the legend traced from the WORLD_TYPE face, three constructions built as kits and maps, all rendered in the clone `W/proj_alarmtell` against today's English sign, one Shot sheet. It then built the recommendation for every EXIT kit and wrote the per-language model swap, gated with Roslyn mac + iOS. What it did not do: land anything, run Unity on main, download anything, or make a sound.

Reused, not redone: `10_spec.md` (construction, the 1990 codes S1–S12, lamp field), `30_integration.md` §2 (letter-share method), the world-text inventory fit numbers (`W/worldtext/art/journal.txt` 18:31), `research/localization/WORLD_TYPE.md` (faces and the sizing rule, 平面视觉's).

---

## 0. 给 Red：EXIT 的中文版怎么做（请选一个）

**Red 已回答（2026-11-04 08:07，游戏视觉对话，原话）：“按推荐选 C”。** 即第 1 问选 C（构造的“出口”，94.9 % 长体），第 2 问按推荐：请叙事设计签“出口”作 EXIT 的简体中文短式。签字之后：W/apply/world_text_exit_zh 去掉 HOLD、在当时的主工程上重打并落地；平面视觉把 24 张简体中文疏散图的出口字样改成“出口”（EXIT_SHORT），游戏视觉重打补丁；叙事设计改 TEXT 面板里那一行（一个门只用一个词）。以下是当时的提问，保留原样。

**怎么读、怎么回：** 先看图 `sheets/exit_world_text_options_zh-Hans.jpg`。第一列是今天的英文 EXIT，后面三列是三种中文做法，每一行的相机、灯光完全一样，只换牌子。推荐的 C 现在的样子看 `sheets/exit_constructed_C_sideair_zh-Hans.jpg`（11-04 07:2x 两侧留白以后，和英文、改之前的 C 并排）；墨高改正的那一版在 `sheets/exit_constructed_C_ink_zh-Hans.jpg`。可以直接回“全部按推荐”，也可以逐项回答下面两个问题。

**背景：** 英文 EXIT 是一块钢板，上面冲穿了 6 英寸（152 mm）高的字母，笔画 3/4 英寸（19 mm），后面是红色灯片和两个灯泡。95 号文件已经定了：中文版要翻译，但红色、盒子和灯光不变。问题是中文字比 EXIT 复杂。WORLD_TYPE 的字号规则是“中文字身 = 拉丁大写高 ÷ 0.70”，算下来字身约 218 mm。“安全出口”四个字需要 871 mm 宽，压到 85 % 的长体也要 740 mm，可盒子的开口只有 320 × 186 mm。规则的顺序是：先长体到 85 %，再请叙事设计签一个更短的词，最后才缩小字号。在这个盒子里，“安全出口”和“出口”都只能走到最后一步：“安全出口”缩到规则字号的 40 %，“出口”缩到 75 %，字形再压成 94.9 % 的长体（11-04 前是 70 %、不压；见下面两段 11-04 的更新）。

| 选项 | 玩家看到什么 | 3 m | 8 m | 断电（电池两灯） | 近看 0.35 m | 成本 |
|---|---|---|---|---|---|---|
| A 全词 · 镂空 + 连筋 | “安全出口”冲穿钢板。“口”的中间、“全”的屋顶三角、“安”里的小三角会掉出来，所以留 4 条 3 mm 的连筋。字高 85 mm，是 EXIT 的 56 %。 | 能读 | 糊成一条红线 | 两头亮、中间暗（和英文一样，看得出在用电池） | 能看到连筋和孔壁，“全”的屋顶被劈开 | 三角面 4967 / 2261（超预算 42 %） |
| B 全词 · 印刷灯片 | 同样大小。不冲孔，灯片上印一层白墨，字是透光的红。没有连筋，字形完整。 | 能读 | 同样糊 | 同 A | 平的，没有孔壁的立体感，近看是另一种工艺的牌子 | 2952 / 926（预算内） |
| **C 短词 · 镂空（推荐）** | 两个字“出口”。字的墨高 152.4 mm，和 EXIT 的字母一样高（11-04 起；之前是 143 mm，EXIT 的 94 %）；字形压成 94.9 % 的长体，两个字一共 278 mm 宽，两侧各留 21 mm，是上下留白（16.8 mm）的 1.25 倍（11-04 07:0x 起；之前 292 mm 宽，两侧只剩 14 mm）。笔画 19.05 mm，和英文是同一个 3/4 英寸。只有“口”要两条 5.7 mm 的连筋，“口”的两只脚一样长。字按英文 EXIT 冲模的做法构造（见下面 11-03 23:33 和两段 11-04 的说明）。 | 能读 | 能读，和 EXIT 一样 | 两个字一起变暗（没有“中间”，电池信号弱一些） | 和英文同一种冲孔工艺，笔画粗细和英文一致 | 3302 / 1036（预算内） |

**11-03 23:33 更新（平面视觉 22:31 的字体决定第 6 条）：** C 的“出口”不再从字体轮廓描下来，而是像英文 EXIT 的冲模一样“构造”：沿思源黑体粗体（Noto Sans SC 700）的笔画骨架，每一笔都用同一个 19.05 mm（英文的 3/4 英寸）笔画宽，方头，内角是冲头的 R0.5 圆角，“口”的连筋仍在上下。原来描出来的字笔画在 17.7–20.5 mm 之间不等，现在全部统一，和 EXIT 一样。3 m、8 m 看起来和之前一样，近看笔画粗细和英文一致。四种挂法的模型、7 张贴图已经按此重做，新图 `sheets/exit_constructed_C_zh-Hans.jpg` 把英文、原来描的 C、构造的 C 并排放在一起。你要选的问题没有变。

**11-04 更新（平面视觉 11-04 06:3x 的确认，WORLD_TYPE §3）：** 平面视觉看了构造的 C 以后确认了三点，C 已经按此重做：
- **字高按“墨”算，不按字身算。** 生命安全标志“不低于英文字母”的下限，量的是字实际印出来的高度。所以“出口”的字身从 152.4 mm 放大到 162.6 mm，墨高正好 152.4 mm，和 EXIT 的字母一样高；墨宽 291.9 mm，就是英文 EXIT 连两个箭头的宽度；上下各留 16.8 mm，也和英文一样。笔画仍是构造的 19.05 mm，连筋仍是 5.7 mm（笔画的 0.30 倍，至少 3 mm）。（07:0x 的“两侧留白”规则又把字形压成 94.9 % 的长体，墨宽变成 278.0 mm，墨高不变，见下面 07:0x 的更新。）
- **同一种笔画末端画成一样长。** “口”的两只脚原来照字体是 12.3 和 12.0 mm，现在都是 13.6 mm；“出”上下两个“凵”的竖头也各自拉齐（原来差 0.13 和 0.09 mm，肉眼看不出）。
- **自由端保持字体自己的方头**，不变。

四种挂法的模型、7 张贴图都已重做，Unity 里的切换检查也重跑了：切换 55 张、拒绝 0，挂着的牌子实时换成“出口”，电池状态的亮度 0.2287 → 0.2390。三角面数不变（3302 / 1036）。新图 `sheets/exit_constructed_C_ink_zh-Hans.jpg` 把英文、05:51 的 C、改正后的 C 并排放，只放有变化的行（墨框、细节、4K 近看、8 m、3 m、成本）。**诚实的缺点（07:0x 已按新规则解决，见下一段）：** 两个字当时把开口填到离两边只有 14 mm，英文是字母加两侧箭头才占满，所以中文牌子看起来更满、更重。你要选的问题没有变。

**11-04 07:0x 更新（平面视觉的新规则“两侧留白”，WORLD_TYPE §3）：** 针对上面这个缺点，平面视觉定了一条新规则：字放到下限高度时，左右两侧的留白至少是上下留白的 1.25 倍；做法是把字的骨架横向压窄（长体），笔画宽度不变，最多压到 85 %。C 已经按此重做：
- “出口”压成 94.9 % 的长体。墨高还是 152.45 mm，墨宽从 291.9 mm 变成 278.0 mm，两侧各留 21.0 mm，上下各 16.8 mm，正好 1.25 倍。笔画 19.05 mm、连筋 5.7 mm、“口”的两只脚 13.6 mm 都没变。
- 规则里写的是 95 %。按 95 % 实际量，两侧是 20.87 mm，只有 1.244 倍，每边差 0.10 mm。所以工具取了满足 1.25 倍的最宽一档：94.9 %。这一点请平面视觉确认（用 94.9 %，还是 95 % 加一个容差），不影响你要选的问题。
- 四种挂法的模型、7 张贴图都已重做，Unity 里的切换检查也重跑了：切换 59 张、拒绝 0，挂着的牌子实时换成“出口”，电池状态的亮度 0.1655 → 0.2314（压窄之前是 0.2390）。三角面数不变（3302 / 1036）。
- 新图 `sheets/exit_constructed_C_sideair_zh-Hans.jpg` 把英文、压窄之前的 C（100 % 宽）、两侧留白的 C 并排放，只放有变化的行：墨框和四边留白、4K 近看（整体、左字、右字、电池）、断电、8 m（亮、暗、手机）、成本。近看时两个字的左右两边各多出约 7 mm 钢板，字不再顶到开口边上；8 m 看两版几乎一样（宽度只差约 1 个像素）。
- **诚实的缺点：** 压窄以后“口”的内框从 88.6 × 81.4 mm 变成 83.1 × 81.4 mm；灯照到“口”的亮度是“出”的 0.93（之前 0.96），两个字的亮度差稍大一点。你要选的问题没有变。

**推荐 C，理由：** 它是唯一在 8 m（1080p）还读得出来的中文做法，和英文同一种工艺、同一个盒子、同样的成本。而且 WORLD_TYPE 规则本身的下一步就是“请叙事设计签一个短词”。“EXIT”本身就只是“出口”。香港的双语出口牌写的正是“出口 EXIT”（香港消防处工作守则 §5.10）。

**诚实的缺点：**
- C 要叙事设计签“出口”这个短词，就像德语已经签了 AUSGANG 作 NOTAUSGANG 的短式。中国大陆的消防标志用的是“安全出口”。如果叙事设计不签，推荐退回 B：字形完整、成本低，代价是 8 m 读不出、近看是平的。
- 三种都去掉了英文板上那两个封着的箭头刻线，因为中文一行占满了开口。
- 字号都小于 WORLD_TYPE 的规则字号。平面视觉 22:31 已经定了：开口固定、放不下规则字号时，最后一步按盒子定字号；生命安全标志至少保持它替换的拉丁字母的高度和笔画（香港守则的做法），并写明“出口”满足这条。原来这里留了一个问题：“出口”的字身 152.4 mm 等于 EXIT 的字母高，但墨高只有 143 mm。11-04 平面视觉确认了按墨高算，所以字身已经放大到 162.6 mm，墨高 152.4 mm（上面 11-04 的更新）。当时的代价是两个字几乎填满开口；07:0x 的“两侧留白”规则把字形压成 94.9 % 的长体，两侧留 21 mm，这一条已经解决，新的代价是“口”的内框窄了 5.5 mm。
- （打包阶段补充，11-03 21:4x）平面视觉在 20:28–20:45 给 WORLD_TYPE 加了一条“一个门只用一个词”（德语的 AUSGANG 就是这样定的）：一种语言一旦用了签过的短词，这种语言的每一张疏散图都印这个短词，TEXT 面板的同一行也说同一个词。95 号文件 §6 Q2 也写了“疏散图、图例和出口牌要一致”。所以选 C，不只是出口牌写“出口”：主工程里已有的简体中文疏散图（W1）楼梯门上印的“安全出口”要改印“出口”（平面视觉重出图，游戏视觉重新打补丁）；TEXT 面板“EXIT（楼梯门上方）”的两行，以及图例里的“楼梯（安全出口）”，也要请叙事设计改成“出口”。做完以后，牌子、疏散图、近看文字三处都说“出口”。选 B 的话这些都不用动，它们本来就写“安全出口”。

**请回答：**
1. 选 A、B 还是 C？
2. 如果选 C：是否请叙事设计签“出口”作 EXIT 的中文短式？签了以后，简体中文疏散图和 TEXT 面板也跟着改成“出口”（一个门只用一个词）。
（原来的第 3 问“若选 A，‘全’的连筋放哪里”已由平面视觉 22:31 定了：从左边撇的脚穿过，不放屋顶尖；A 也要按构造的做法重画。所以不再问你。）

**已经做好、等你选：** C 已经按最高规格（11-03 23:33 起是构造的字；11-04 起墨高 152.4 mm、“口”的两只脚等长；11-04 07:0x 起 94.9 % 长体，两侧留白是上下的 1.25 倍）做完四种挂法（墙挂、端挂/旗挂、吊挂、贴顶吊挂）的中文模型、7 张贴图，以及切换代码（切换语言时已经挂着的牌子也会实时换）。全部打包好（11-04 07:28 重新打包），**没有落地**。A 和 B 的墙挂款已经做好（用于渲染），选了以后用同一条命令出全套，大约 10 分钟。英文和 WebGL 完全不变。

---

## 1. Why the rule cannot hold in the kept box

| | Value | Source |
|---|---|---|
| English legend | letters 152.4 mm (6 in), strokes 19.05 mm (3/4 in), legend 223 mm, 292 mm with both chevrons, in a 320 × 186 mm opening | `10_spec.md` §5, `exit_sign_geom.py` |
| WORLD_TYPE size | CJK em = Latin cap / 0.70 = 217.7 mm; too long → 长体 to 85 %, then a shorter signed word, then a smaller size, never a cut | `WORLD_TYPE.md` §3 |
| 安全出口 at the rule size | 870.9 mm at 100 %, 740.3 mm at 85 % | Noto Sans SC 700 advance 1 em per character, no tracking |
| 出口 at the rule size | 435.4 mm at 100 %, 370.1 mm at 85 % | same |
| Decided | the box stays (95 §6 Q2 (a)) | `95_world_text_localization.md` §6 |

Both words need the rule's last step. The largest size the kept box holds:
- 安全出口: 85 % width, em 89.5 mm, ink 291.9 × 85.1 mm. That is 41 % of the rule's em; the ink is 56 % of EXIT's letter height.
- 出口: em 162.6 mm, so its ink stands 152.4 mm, EXIT's letter height (平面视觉 11-04, WORLD_TYPE §3: "Height means ink"), and since 11-04 07:0x 长体 94.9 % of the skeleton (WORLD_TYPE §3 "Air at the sides": the side margins at least 1.25 × the top and bottom ones): ink 277.98 × 152.45 mm in the 320 × 186 mm opening, sides 21.01 mm, top and bottom 16.78 mm. That is 75 % of the rule's em; the ink is 100 % of EXIT's height. At 100 % width (11-04 06:2x) the ink was 291.9 × 152.45 mm, the English legend's own width with both chevrons, sides 14.05 mm. Until 11-04: em 152.4 mm (= the English cap), ink 276.0 × 142.9 mm traced (274.8 × 142.9 constructed), 70 % of the rule's em, 94 % of EXIT's height.

Strokes (measured on the traced face at em 152.4): 安全出口 9.7 mm vertical, 10.4 mm horizontal; 出口 19.7 / 18.4 mm. The English is 19.05 mm. Since the 22:31 type call a die-cut legend is constructed at the English stroke, so C's strokes are 19.05 mm everywhere (§3b); at em 162.6 the face's own strokes would be 18.9–21.9 mm (median vertical 21.7, horizontal 19.6), and the construction keeps 19.05.

## 2. Research: what the codes and the period say

- **US 1988–1990 (reused, S1–S6):** NFPA 101-1988 and UL 924-1989 ask for 6 in letters and 3/4 in strokes (S3, S4). UL 924 defines a stencil face as translucent letters on an opaque background (S4). By that definition a lens printed with an opaque ground and clear letters counts photometrically as a stencil (option B). In the period drawings S6, Type 606 is an edge-lit clear acrylic panel and Type 604 has red letters on a white diffuser. Printed and panel legends existed beside the cut-through stencil.
- **A real bilingual exit-sign rule (new, S13):** Hong Kong's Fire Services Department Code of Practice, §5.10 as amended by Circular Letter 2/2000, read as text. Its words: "The signs shall bear the words in English and Chinese of not less than 125 mm high … the Chinese characters shall be with width of vertical strokes not less than 15 mm and with width of horizontal strokes not less than 10 mm". Its Figure 1 pairs the words "EXIT" and "出口". It asks for translucent surrounds, with white lettering preferred for internally lit signs.
  - That size rule is not among the circular's three changes, so it stood in the Code before April 2000. Its first edition is not verified.
  - Read for us: a working code set the Chinese characters at the same height as the English letters, not 1.43 ×, and let the Chinese strokes go thinner than the Latin ones (15 / 10 mm against our 19 mm).
  - Against it: A and B are 85 mm tall with 9.7 / 10.4 mm strokes, below HK's 125 mm and 15 mm. C (ink 152.4 mm since 11-04, 143 mm before; 19.05 mm strokes constructed) clears HK on both counts and matches the US 6 in letter and 3/4 in stroke.
- **The word:** the zh-Hans table signs `EXIT` → `安全出口` (95 §5). HK's bilingual sign uses `出口`, the word EXIT itself means "exit", and 95 §5 already gives German a signed short form (NOTAUSGANG, short AUSGANG). So 出口 is the short form that WORLD_TYPE's step 2 asks 叙事设计 for. It is not signed yet.
- **Stencil ties (bridges):** EXIT has no closed counters (`10_spec.md` §5). The traced face has 3 islands in 安全出口: 安 0.025 em², the 全 roof triangle 0.047 em², 口 0.264 em². 出口 has one, 口. Ties run where the stroke between a counter and the open plate is thinnest. A small island gets one tie; an island of at least 0.06 em² gets two opposite ties. A tie is 0.30 × the vertical stem, at least 3 mm (confirmed by 平面视觉 11-04, WORLD_TYPE §3): 3.0 mm in A, 5.9 mm in the traced C, 5.7 mm (0.30 × 19.05) in the constructed C. Every reflex corner takes the punch's R 0.5 fillet (`exit_sign_geom.HOLE_FILLET`). WORLD_TYPE §3 (22:31) since fixes the places: 口 top and bottom in every option, 全 through the foot of the left 撇, never through a defining join.

## 3. The options as built

All three keep the housing, frame, screws, lamps, the diffuser and its lamp field, the materials and the driver. They differ only in the plate (or film), the face maps and the diffuser's insects. Face: Noto Sans SC VF at wght 700 (WORLD_TYPE R1 黑体). W/venv has no fontTools, so it is rendered by FreeType at 30 px/mm and traced: marching squares, Douglas–Peucker 0.04 mm, edges within 0.15 mm of an axis snapped square (`tools/exit_legend.py`).

| | A full word, stencil + ties | B full word, printed lens | C short word, stencil (REC) |
|---|---|---|---|
| Word | 安全出口 (signed) | 安全出口 (signed) | 出口 (needs 叙事) |
| Width, em | 85 %, 89.5 mm | 85 %, 89.5 mm | 94.9 %, 162.6 mm (100 % until 11-04 07:0x; em 152.4 until 11-04) |
| Ink box | 291.9 × 85.1 mm, margins 14.1 / 50.4 | same | 277.98 × 152.45 mm, margins 21.01 / 16.78 = 1.25 × (EXIT with chevrons 292.25 × 152.4, 13.9 / 16.8 = 0.83 ×); at 100 % width (11-04 06:2x) 291.9 × 152.45, 14.05 / 16.78 = 0.84 ×; until 11-04 274.8 × 142.9, 22.6 / 21.6 (traced: 276.0 × 142.9, 22.0 / 21.6) |
| Strokes | 9.7 / 10.4 mm | 9.7 / 10.4 mm | 19.05 mm every stroke, constructed (traced: 19.7 / 18.4) |
| Islands, ties | 3 islands, 4 ties of 3.0 mm (安 1 through 女's bar, 全 1 up through the roof apex, 口 2 left/right) | none: counters print whole | 1 island, 2 ties of 5.7 mm (口 top and bottom; traced 5.9); 口's feet 13.62 / 13.62 mm (until 11-04 12.30 / 12.03); 口's counter 83.1 × 81.4 mm (88.6 × 81.4 at 100 % width) |
| Construction | holes cut through the 0.9 mm plate, 0.2 chamfer, R 0.5 reflex fillets | no plate; an opaque white ink film 0.4 mm proud of the diffuser, characters cut out, counters left as islands, on the face material | as A |
| Chevron scores | dropped (no room) | dropped | dropped |
| Lamp field, rel. to the brightest character | 安 .38 全 .92 出 1 口 .32; max/min 9.3 (UL ≤ 40) | same | 出 1 口 .93; max/min 9.3 (100 % width .96; until 11-04 .93, traced .92) |
| BATTERY (DC pair) | 安 .27 全 .06 出 .06 口 .24: ends glow, middle dims, like EXIT | same | 出 .21 口 .24: even, dimmer (100 % width .23 / .24; until 11-04 .21 / .24) |
| Letter shares A / B / DC | .486 / .514 / .938 | .490 / .510 / .938 | .5515 / .4485 / .9257 (100 % width .541 / .459 / .956; until 11-04 .549 / .451 / .915; traced .556 / .444 / .914) |

English: shares .459 / .541 / .662. The driver now reads a language copy's shares from its sidecar, so the battery Level of C is 0.231 (0.239 at 100 % width, 0.229 until 11-04) against English 0.166.

### 3b. C constructed (revision, 平面视觉's type call 6 of 2026-11-03 22:31)

WORLD_TYPE §3 "Stencils": "A die-cut legend (EXIT) is constructed as the English one is: one stroke width equal to the English stroke (19.05 mm), drawn on the CJK face's skeleton with square ends and the punch's fillet." `tools/exit_legend.py` now does this for `--mode stencil` (`--traced` keeps the 21:32 path, byte for byte the same A, B and C files as before):
- **The skeleton** is read from Noto Sans SC VF at wght 700, rendered at 30 px/mm (no fontTools in W/venv): the long vertical and horizontal ink runs (≥ 30 mm), grouped into strokes; each stroke's centre line and its two ends measured to the sub-pixel from the coverage (medians along the stroke). Check: the union of these axis strokes at the face's own widths covers the face's ink to 2 px in 8.6 M (出) and 0 px (口), so both characters are pure axis strokes. A character with diagonal or curved strokes is refused (安: 52 % of its ink is not axis strokes), so a constructed A needs its skeleton drawn by hand.
- **The strokes:** each is a rectangle 19.05 mm wide on its centre line. An end that stops in another stroke (a corner or a T) runs to the far side of that stroke's 19.05 mm, so a corner is a full square, as in EXIT's E. Any other end keeps the face's own end, cut square (出's foot at the lower right stays as long as Noto draws it; 口's two feet did too until the 11-04 correction below drew ends of one kind to one length). The union is exact, on the rectangles' own coordinates; the ties are plate rectangles cut out of it; every reflex corner gets R 0.5.
- **出** = 5 verticals and 2 bars. The face's stems are 19.4–20.5 mm and its bars 17.7–18.2 mm; all are 19.05 now. **口** = 2 sides and 2 bars; face 19.1 / 20.0 and 18.6 / 18.8 mm, now 19.05. The skeleton (centre lines, ends and how each end was set) is in the legend file, `construction.skeleton`.
- **Result (11-04 05:51, em 152.4; corrected below):** ink 274.78 × 142.88 mm (traced 275.96 × 142.87), 3 holes, 80 vertices as before, hole area 17,075 mm² (−1.3 %), ties 5.72 mm at x 237.44 (traced 5.91 at 237.31). The 7 maps change only round the holes (Face_A 1.3 % of texels, Face_S 2.4 %, the diffuser maps 0.2 %: the insects follow the new feet); Face_N and the enamel maps do not change. Kits: the same tris (3302 / 1036, 6090, 6834, 6586); the sidecars change only `strokeM` (0.01841 → 0.01905) and `letterShares`. Proof in Blender: each kit's LOD0 plate carries one 80-vertex ring equal to the legend's holes to 0.0000 mm.

**Correction of 2026-11-04 (平面视觉's readings of 11-04 06:3x, WORLD_TYPE §3 "Readings confirmed or set 11-04").** Their words: "Height means ink. A floor in letter height (the life-safety rule) is met by the CJK ink, not its em: 出口's em becomes 162.6 mm so the ink stands 152.4 mm, EXIT's letters; its 292 mm width is EXIT's legend with both chevrons." and "a free end keeps the face's own end, cut square; ends of one kind are drawn to one length (口's two feet equal)." Ties stay 0.30 × the stroke, at least 3 mm.
- **The tool:** `tools/exit_legend.py` (now 6177f97c) gains `--ink-floor MM` (refuses a legend whose ink stands lower; 0.05 mm is the raster's reading) and `--kind-tol` (the free ends of one character on one axis and side that lie within this fraction of the em, 0.01 by default, are drawn to their mean: `construction.endKinds`). `--kind-tol 0` at em 152.4 rebuilds the 05:51 legend exactly.
- **The legend** (`exit_legend_zh-Hans.json` 51af83dd): em 162.6 mm, ink 291.90 × 152.45 mm, margins 14.05 across and 16.78 up and down (EXIT's legend with both chevrons: 292.25 × 152.40, 13.88 / 16.80), stroke 19.05 mm, 1 island, 80 hole vertices, ties 5.71 mm top and bottom of 口 at x 242.62, counter 7205 mm² (6141 at em 152.4). Ends of one kind: 口's feet 19.863 / 20.150 → 20.006 (each 13.62 mm below the bottom bar; the face draws 13.76 / 13.48), and, the rule read as stated for every kind, 出's upper 凵 tops 155.517 / 155.644 → 155.581 and lower 凵 tops 87.596 / 87.688 → 87.642. Alone, kept: 出's centre stem top (the ink top) and its lower right foot (the ink bottom).
- **Maps** against the 05:51 build: Face_A 11.03 % of texels, Face_S 3.25 %, the four diffuser maps 0.28–0.35 % (the insects follow the new feet); Face_N unchanged (the normal carries no legend), the enamel maps = main. Lamp field 出 1 口 .96, max/min 9.3; battery 出 .23 口 .24; letter shares .541 / .459 / .956.
- **Kits:** all four modules and their _Dead variants rebuilt in Blender: the same tris (3302 / 1036, 6090, 6834, 6586), 80 hole vertices; the sidecars change only `letterHeightM` 0.1429 → 0.1525, `legendEmM` 0.1524 → 0.1626 and `letterShares`. The FBX plate check finds one 80-vertex ring on Kit_ExitSign_LOD0 and Kit_ExitSign_Hanging_LOD0 equal to the legend's holes to 0.01 mm (the check's grid).
- **Honest flaw (met by 平面视觉's side-air rule of 07:0x, below):** the two characters filled the opening to 14 mm of its sides. EXIT reaches that width only with its chevrons, so 出口 read heavier than EXIT's letters between chevrons.

**Air at the sides, 2026-11-04 07:0x (平面视觉's rule in WORLD_TYPE §3).** Their words: "at the floor height a legend may not crowd its opening: its side margins stay at least 1.25 × its top and bottom ones. Narrow the skeleton (长体, constructed strokes unchanged) to get there, never below 85 %." and "at 95 % its sides come back to about 21 mm against 16.8 up and down." They also confirmed "ends of one kind" as the general reading (出's 凵 tops evened, as built above).
- **The tool:** `tools/exit_legend.py` (now a7de2951) gains `--side-air X` (refuses a legend whose sides are under X × its top and bottom, and names the width that would meet it), `--width auto` (a bisection on 0.001 steps from 100 % down to 85 %; under 85 % it refuses: a shorter word signed by 叙事 comes next) and a `construction.sideAir` record. Run with the old arguments it rebuilds the 06:2x legend byte for byte. The width scales the skeleton across before the stroke is drawn (vertical centres × w, joined bar ends to the scaled stem's far side, free bar ends × w); the stroke, the ties, the feet and the end kinds stay as they were.
- **95 % or 94.9 %:** measured on the holes, 95.0 % leaves sides of 20.87 mm = 1.244 ×, 0.10 mm a side short of 1.25 ×. The rule says "at least", so the build takes the widest 0.1 % step that meets it, 94.9 %: sides 21.01 mm = 1.252 ×. Open for 平面视觉: confirm 94.9 %, or restate 95 % with a tolerance (§7).
- **The legend** (`exit_legend_zh-Hans.json` b052f24c): em 162.6 mm, 长体 94.9 %, ink 277.98 × 152.45 mm, margins 21.01 across and 16.78 up and down (1.2522 ×), stroke 19.05 mm, ties 5.71 mm top and bottom of 口 at x 238.41, 口's feet 13.62 / 13.62 mm, 出's 凵 tops as above, 1 island, 80 hole vertices. The gap between 出 and 口 is 31.2 mm (33.9 at 100 %), 口's counter 83.1 × 81.4 mm (88.6 × 81.4), counter area 6759 mm² (7205).
- **Maps** against the 100 % build: Face_A 6.76 % of texels, Face_S 1.93 %, the four diffuser maps 0.23–0.30 %; Face_N unchanged, the enamel maps = main. Lamp field 出 1 口 .927 (.960), max/min 9.3; battery 出 .21 口 .24; letter shares .5515 / .4485 / .9257, so the battery Level is 0.2314 (0.2390).
- **Kits:** all four modules and their _Dead variants rebuilt in Blender (silent): the same tris (3302 / 1036, 6090 / 1928, 6834 / 2101, 6586 / 2042); the sidecars change only `legendWidth` 1.0 → 0.949 and `letterShares`. The FBX plate check finds one 80-vertex ring on Kit_ExitSign_LOD0 and Kit_ExitSign_Hanging_LOD0 equal to the legend's holes (0.00 mm).
- **Honest flaw:** narrowing takes 5.5 mm out of 口's counter, and the lamps now light 口 at .93 of 出 (.96 at 100 %). At 8 m the two builds read the same (the width differs by about one pixel at 1080p).

B's first build printed the white ink into the diffuser's albedo. It rendered dark red, because `ExitSign_Diffuser.mat` tints its albedo by _BaseColor (1, .248, .248) and a swap copy can change only textures, not a material. So B's print is geometry on the face material (§3 table).

## 4. Renders

Sheet `sheets/exit_world_text_options_zh-Hans.jpg` (1920 × 3841, WAIT-RED, fit file ends ALL LABELS FIT). Harness `FrontRoomsExitOptionsShots` (clone only). The scene is a lookdev corridor 5 × 11.5 m, ceiling 2.74 m, Office surfaces and grade. The sign hangs as `FrontRoomsMapWorld.MountExitSign` hangs the floor's true EXIT: bottom 2.18 m over a 0.914 × 2.134 m door, EmissionGain 2.

| Row | Camera | What it shows |
|---|---|---|
| EYE 3 M LIT | eye 1.62 m, 3 m out, desktop Ultra 1920 × 1080, FOV 76, 1:1 crop | all four read |
| EYE 8 M LIT, + ZOOM | 8 m, 1:1 and the same pixels ×3 nearest | EXIT and C read. A and B smear: their ink is 7.4 px tall at 1080p |
| DARK 8 M LIT | room lights and ambient off, sign LIT (the true EXIT is LIT-locked, C23) | same as above, red on black |
| DARK 3 M BATTERY | room off, sign on its DC pair | EXIT, A and B: ends glow; C: both characters dim |
| PHONE 3 M / 8 M ZOOM / 3 M BATTERY | phone tier: Unity quality Medium (iPhone default, lodBias 0.7), FrontRoomsHandheld forced to the iPhone 16 Pro, 2622 × 1206, FOV 76; LOD1 at 8 m | as desktop |
| HERO LIT / BATTERY / OFF | 0.35 m square on, LOD0, scaled ×0.48 on the sheet | A: the ties at 安, 全's apex and 口. B: flat print, whole counters. C: two ties in 口. OFF: the unlit legend reads dark on the face in all four |

Not measured: frame time (rule 25). Raw frames are in `W/worldtext/art/renders/exit_options` (JPG). The first run, with B before its fix, is in `renders/exit_options_r1`.

### 4b. Revision renders: TODAY | C traced | C constructed

Sheet `sheets/exit_constructed_C_zh-Hans.jpg` (2880 × 9087, WAIT-RED, fit file ends ALL LABELS FIT). Same harness, scene and cameras as §4, one Unity process in `W/proj_alarmtell` (`logs/exit_constructed_2332.log`, 0 error CS), plus a 3840 × 2160 hero tier.
- Rows: the legend drawn from each legend file (the constructed one with its skeleton), a detail at 7.4 px/mm with widths measured on the holes (EXIT 19.05 / 19.05; traced 口 top bar 18.57, right side 20.01; constructed 19.05 / 19.05; the traced outline drawn as a line over the constructed), the 4K hero whole and per character (lit, battery), hero off, eye 8 m, dark 8 m and phone 8 m (×3), eye 3 m, dark 3 m battery, phone 3 m, COST.
- What it shows: at 3 m and 8 m the constructed C cannot be told from the traced one (the changes are 0.3–1.5 mm, under a pixel there). At 0.35 m its strokes are as even as EXIT's: the 出's bars no longer read lighter than its stems.
- The swap check of the package form ran in the same process (§6).

Correction renders (11-04): TODAY | C BEFORE (05:51, em 152.4) | C CORRECTED (em 162.6). Sheet `sheets/exit_constructed_C_ink_zh-Hans.jpg` (2880 × 8492, WAIT-RED, fit file ends ALL LABELS FIT). One Unity process in `W/proj_alarmtell`, 06:22:59–06:23:31 (`logs/exit_inkfloor_0622.log`, 0 error CS), same harness, scene and cameras; only the rows that change:
- INK BOX (each legend drawn whole from its file, the ink box dashed, height and width measured on the holes) and DETAIL (the feet, the bar and the bottom margin; for EXIT the I's stem and the baseline margin). Measured: EXIT 292.25 × 152.40, margins 13.88 / 16.80, stem 19.05; C before 274.78 × 142.88, 22.61 / 21.56, feet 12.30 / 12.03, bar 19.05; C corrected 291.90 × 152.45, 14.05 / 16.78, feet 13.62 / 13.62, bar 19.05.
- Hero 0.35 m at 4K, lit whole and per character, battery; hero off at 1080p; eye, dark and phone at 8 m (×3); eye 3 m, dark 3 m battery, phone 3 m (1:1); COST.
- What it shows: the corrected 出口 stands as tall as EXIT's letters and as wide as EXIT with its chevrons; at 3 m and 8 m it reads a little larger than the 05:51 build and fills the opening more than EXIT does.
- Level in the battery tiles is English's 0.1655 in every column: the harness spawns each option kit under its own id, not through the language swap. In the game's code Level drives only the sign's optional red light (the EXIT tests also read it), and the corridor sign has none (the glow comes from each option's own lamp-field map), so the tiles show what the game shows; the game's zh-Hans Level is 0.2390 (§6).
- Frames: `W/worldtext/art/renders/exit_inkfloor` and `renders/exit_inkfloor_swap` (JPG q92).

Side-air renders (11-04 07:12): TODAY | C 100 % WIDTH (the 06:2x build) | C SIDE AIR 94.9 % (REC). Sheet `sheets/exit_constructed_C_sideair_zh-Hans.jpg` (2880 × 6157, WAIT-RED, fit file ends ALL LABELS FIT). One Unity process in `W/proj_alarmtell`, 07:12:00–07:12:23 (`logs/exit_sideair_0712.log`, 0 error CS), same harness, scene and cameras; only the rows that change:
- INK BOX AND MARGINS: each legend drawn whole from its file, the ink box dashed, the four margins tinted and figured, measured on the holes (sides blue, top and bottom sand). EXIT 292.25 × 152.40, sides 13.88 = 0.83 × 16.80 (measured to its thin chevron scores; its letters alone are 223 mm wide); C 100 % 291.90 × 152.45, 14.05 = 0.84 × 16.78; C side air 277.98 × 152.45, 21.01 = 1.25 × 16.78.
- Hero 0.35 m at 4K: lit whole, left character, right character, battery; hero off at 1080p; eye, dark and phone at 8 m (×3); COST. The 3 m frames were rendered too (in the frames folder) and are left off the sheet.
- What it shows: close up, the side-air 出口 keeps a band of plate either side of the characters, about 7 mm more a side, and no longer runs to the opening's edge; at 8 m the two C builds read the same.
- Level in the battery tiles is English's 0.1655 in every column, as in the correction renders (the harness spawns each option kit under its own id); the game's zh-Hans Level is 0.2314 (§6).
- Frames: `W/worldtext/art/renders/exit_sideair` and `renders/exit_sideair_swap` (JPG q92).

## 5. Cost

| | TODAY | A | B | C |
|---|---|---|---|---|
| Kit_ExitSign tris LOD0 / LOD1 | 3136 / 984 | 4967 / 2261 (+42 % / +106 %) | 2952 / 926 | 3302 / 1036 |
| Other kits, LOD0 | _End 5758, _Hanging 6502, _HangingFlush 6254 | not built yet (plate +~1830 per face) | not built yet | 6090, 6834, 6586 (all ≤ +15 %) |
| Maps | main's 10 | 7 copies: Face A/N/S, Diffuser A/E/F/S | same | same |
| Build size per language, desktop BC7 | 0 | full copies 10.7 MiB; in patch form C ships 7 patches (31 files, 1.29 MB) and 4 model copies with sidecars (1.10 MB); the whole package is 2.76 MB, 90 files (11-04 07:28) | same | same |
| Memory at run time | 0 | the bases hold the copies' pixels (no extra resident memory); 4 small model copies (meshes) while the language is on | same | same |
| Draws | 4 slots per renderer | unchanged | unchanged (the film uses the face slot) | unchanged |
| Language model copies | – | 4 FBX + sidecars | 4 | 4 (built) |

## 6. The build of C (packaged, not landed)

Package `W/worldtext/art/overlay_exit_C`, 90 files since the revision (88 before: Face_S now needs a fourth atlas), 3.1 MB, built by `tools/build_exit_overlay.py --opt C --form patch`; the traced set as it was is kept in `W/worldtext/art/tmp/exit/traced_C/`. Corrected 11-04 06:20 (em 162.6): the same 90 paths, every GUID kept, 38 files changed (4 kits + 4 sidecars, the Face_A, Face_S and 4 diffuser patches, `exit_legend.py`, the legend file); the 05:51 set is kept in `W/worldtext/art/tmp/exit/constructed_em152/`. Side air 11-04 07:09 (长体 94.9 %): the same 90 paths, every GUID kept, 35 files changed (4 kits + 4 sidecars, the Face_A, Face_S and 4 diffuser patches, `exit_legend.py`, the legend file, the manifest); the 100 % set is kept in `W/worldtext/art/tmp/exit/sideair/ink100/`.
- `Assets/Resources/WorldText/zh-Hans/Props/Models/Kit_ExitSign{,_End,_Hanging,_HangingFlush}.{fbx,json}`, each with its base's .meta and a fresh GUID. These are the same kit modules built with the 出口 legend.
  - The _Dead variants need no copy: their LOD children carry the live kit's name and meshes.
  - The sidecars carry `exitSign.legend`, `legendFace`, `legendEmM`, `letterHeightM`, `strokeM`, `chevrons: none` and `letterShares`.
- 7 maps as patches (main's `pack_world_text_patch.py`): `ExitSign_Face_A/_N/_S`, `ExitSign_Diffuser_A/_E/_F/_S`.
- Code, all 游戏视觉's files. Roslyn tree main `--all` with the 4 overrides: mac COMPILE OK, 0 errors; iOS COMPILE OK, 0 errors (again at the 11-04 06:04, 06:47, 06:55 and 07:28 re-packs, over main's newer `FrontRoomsWorldText.cs` 866984a7 and `FrontRoomsWorldTextImport.cs` c31cec26; the code did not change in the correction or the side-air rebuild).
  - `FrontRoomsWorldText.cs` gains a "models" kind (manifest list + `Model(path)`).
  - `FrontRoomsWorldTextImport.cs` lists `.fbx` copies that have a base kit.
  - `FrontRoomsKitImporter.cs` imports `WorldText/<code>/Props/Models/` exactly as a kit.
  - `FrontRoomsExitSign.cs` follows `FrontRoomsWorldText.Applied`. It takes the copy's meshes by LOD child name, puts its own back on English, and reads the copy's letter shares. WebGL never subscribes.
  - `FrontRoomsKitLibrary` is unchanged: it already adds the driver at Spawn, so a sign built in zh-Hans is born with its legend.
- Generators under `Tools/`. In each, English is byte for byte as before: the 10 textures and the 8 sidecars match main; the 8 FBX differ only in the writer's 131 timestamp bytes.
  - `lookdev/exit_legend.py`, new.
  - `lookdev/gen_exit_sign.py` and `Blender/frontrooms_kit/assets/exit_sign_geom.py`, `exit_sign_common.py`: `FR_EXIT_LEGEND=<legend json>` switches the legend.
  - The legend file `lookdev/exit_legend_zh-Hans.json`.
- Manifest over main and the overlay, from `W/worldtext/build_w1_overlay.py`, now extended with models. zh-Hans at the 11-04 07:28 re-pack (over main's 516012be, which lists the no-smoking plate with its engraving and mask and the magazine covers): 32 swap, 27 patch, 18 own, 12 texts, 4 models, manifest 61fa80cf = main's plus exactly our 15 zh-Hans entries; refused 0.

Unity check in the clone (import, then the swap; `logs/exit_swapcheck_2115.log`):
- 0 error CS. Preview(zh-Hans) gave Swapped 46, Refused 0.
- Signs built in English switched live to 出口: Kit_ExitSign 3136 → 3302 tris, _Hanging, _End and _Dead likewise. A sign spawned while zh-Hans is on screen is born with 出口. Back to English, the meshes and shares return.
- After going back to English in edit mode, the face maps render blank. This is the world-text Restore finding the Copies stage reported, an Editor stale-binding effect, not this package.
- Revision check (23:33, constructed legend, the package's C# as merged by G at 22:39 over main's newer `FrontRoomsWorldText.cs` and `FrontRoomsWorldTextImport.cs`): 0 error CS; the manifest Unity rebuilt = the overlay's; Preview(zh-Hans) Swapped 55, Refused 0; the same live switch on all four kits; battery Level 0.1655 → 0.2287 → 0.1655; a sign born in zh-Hans carries 出口. The English frames after the switch back are whole now (main's Rebind fixed the blank faces).
- Correction check (11-04 06:22:59–06:23:31, `logs/exit_inkfloor_0622.log`; the em-162.6 overlay and the package's merged C#): 0 error CS; the manifest as built (zh-Hans 28 swap, 27 patch, 18 own, 12 texts, 4 models, 0 refused); Preview(zh-Hans) Swapped 55, Refused 0; signs built in English switched live (Kit_ExitSign 3136 → 3302 tris, 984 → 1036 on LOD1; _Hanging 6502 → 6834, _End 5758 → 6090, _Dead 3136 → 3302); battery Level 0.1655 → 0.2390 (0.25 × DC .956); a sign born while zh-Hans is on screen carries 出口; back to English, 3136 tris and 0.1655. Since main's manifest listed the no-smoking plate (06:49), zh-Hans had 29 swaps then (Swapped 56; 59 since main's manifest of 07:03, below).
- Side-air check (11-04 07:12:00–07:12:23, `logs/exit_sideair_0712.log`; the 94.9 % overlay over main's manifest 516012be and the package's merged C#): 0 error CS; Preview(zh-Hans) Swapped 59 (32 swap + 27 patch), Refused 0; signs built in English switched live (Kit_ExitSign 3136 → 3302 tris, 984 → 1036 on LOD1; _Hanging 6502 → 6834, _End 5758 → 6090, _Dead 3136 → 3302); battery Level 0.1655 → 0.2314 (0.25 × DC .9257); a sign born while zh-Hans is on screen carries 出口 (3302, 0.2314); back to English, 3136 tris and 0.1655. With main's manifest as it is now (516012be), the combined test should see Swapped 59.

Known limit: the ray-traced glass keeps the meshes it registered. A sign already built when the language changes reflects its old legend in glass until its chunk is rebuilt.

## 7. Open

- **Red:** A, B or C (§0).
- **叙事设计:** sign `出口` as the zh-Hans short form of EXIT, if Red picks C.
- **One word for one door (added by the Package stage, 21:4x):** WORLD_TYPE §3 gained this rule at 11-03 20:28–20:45 (平面视觉; de AUSGANG), after this doc's research read the file: once a language uses its signed short word, every sheet of the language prints it and the TEXT panel's shared row says it too (95 §6 Q2: "the plan, legend and sign agree"). With C, the zh-Hans evacuation plans (W1, in main: 平面视觉's `tutorial_placard.swift` `EXIT_SHORT` / `EXIT_SHORT_EVERYWHERE`, then 游戏视觉 re-packs their patches) and the zh-Hans table rows `EXIT (over the stair door)`, its padlocked twin and the LEGEND rows' 楼梯（安全出口） (叙事) follow 出口. B and A need none of this.
- **平面视觉 (settled 22:31, WORLD_TYPE §3):** a die-cut legend is constructed at 19.05 mm on the face's skeleton (C rebuilt, §3b); the tie places (口 top and bottom, 全 through the left 撇); a kept box sizes the word by the last step, with a floor for life-safety signs of the Latin letter height and stroke, which WORLD_TYPE says 出口 meets.
- **平面视觉 (settled 11-04 06:3x, WORLD_TYPE §3 "Readings confirmed or set 11-04"; C corrected, §3b):** (1) the floor is met by the ink, not the em: 出口 at em 162.6 mm, ink 152.4 mm, 292 mm wide; (2) ties 0.30 × the stroke, at least 3 mm (5.71 mm); (3) a free end keeps the face's own end, cut square, and ends of one kind are drawn to one length (口's feet 13.62 / 13.62 mm). For later languages and packs: a vertical column is centred on its strip; a column too long for its strip goes to 扁体 down to 85 % height, then a shorter signed word, then a smaller em; 长体 with a token narrows only the CJK run.
- **平面视觉 (settled 11-04 07:0x, WORLD_TYPE §3 "Air at the sides"; C rebuilt, §3b):** at the floor height a legend's side margins stay at least 1.25 × its top and bottom ones, reached by narrowing the skeleton (长体) with the constructed stroke unchanged, never below 85 %. C stands at 长体 94.9 %: ink 277.98 × 152.45 mm, sides 21.01 mm against 16.78 (1.252 ×); stroke, ties and feet as before. "Ends of one kind" confirmed as the general reading (出's 凵 tops, built). For later languages, tools only, nothing re-packed: a Latin token at the last step (a smaller em) shrinks with its line (`gen_keytag_atlas.py --signed-short`); tokens in a 竖排 column: one or two digits or letters upright side by side in one em (縦中横: ja 第3四半期報告, ko 3분기), three or more turned 90° clockwise as one run reading down, a Korean word space a half-em gap (`labels_lang.py`); both tools give byte-same English and zh-Hans output. The packer refuses a legend that breaks the side-air rule.
- **平面视觉 (open, 11-04 07:2x; built as read, nothing invented):** WORLD_TYPE names 95 % for 出口, but at 95.0 % the sides measure 20.87 mm = 1.244 ×, 0.10 mm a side short of the rule's 1.25 ×. The build takes the widest 0.1 % step that meets the rule, 94.9 % (1.252 ×). Confirm 94.9 %, or restate 95 % with a tolerance; a change is one legend rebuild and one re-pack.
- **平面视觉 (still open, not settled by WORLD_TYPE; built as read, nothing invented):** a constructed A needs 安 and 全 drawn on a hand skeleton (their 撇 and 点 are not axis strokes); only if Red picks A.
- **Landing order:** the apply package is `W/apply/world_text_exit_zh.sh` (v2, held by `world_text_exit_zh_HOLD.txt`). Since 2026-11-04 07:28 it carries the side-air legend (长体 94.9 %, em 162.6, ink 277.98 × 152.45 mm, sides 1.25 × the top and bottom, ends of one kind equal; the 06:47 and 06:55 packs carried the 100 % build), over main's manifest 516012be (07:03): 90 paths (82 new, 8 replaced: the manifest, the 4 C#, the 3 generators), Roslyn mac + iOS 0 errors, DRY RUN OK, scratch rehearsal MANIFEST OK. The packer now also refuses a legend that misses 平面视觉's 11-04 readings or the side-air rule. It lands the manifest only together with its code. `world_text_ours_zh` landed on 11-03 at 21:49 and its type revision `world_text_ours_zh_types` on 11-04 at 06:09; neither writes nor keeps any of this package's paths, and its dry run ran over both. After any other world-text landing, re-run `W/venv/bin/python W/worldtext/art/tools/package_tracks.py exit` first. It rebuilds the manifest over main as it is and 3-way merges the four C# files if main moved them; G's 22:39 hand merges of `FrontRoomsWorldText.cs` and `FrontRoomsWorldTextImport.cs` are recorded and re-used. Then it re-gates (Roslyn mac + iOS) and dry-runs. Red's Editor also rebuilds the manifest on import.
- **Met at the 11-04 re-pack, not this package's:** 平面视觉 · 信息设计's landing of 11-04 06:23 (`W/apply/bs4_no_smoking_plate`) had put text, not images, into main's `Prop_SignNoSmoking_A.png` and its 9 copies (each file held the W path of its PNG), so the 06:47 pack left those copies out of its manifest. 游戏视觉 re-landed the real PNGs at 06:49 (`bs4_no_smoking_fix`, main's manifest b088141d → 814ea402), and the 06:55 pack lists them. Our manifest builder (`W/worldtext/build_w1_overlay.py`) now refuses a `.png` that is not an image, with the reason, instead of stopping.
- **The other eight languages:** the same pipeline. `exit_legend.py` takes their words and WORLD_TYPE faces once signed (ja 非常口, ko 비상구, zh-Hant 緊急出口; the Latin and Cyrillic words need a constructed legend or Heros, 平面视觉's call).

## 8. Files

- Sheet: `sheets/exit_world_text_options_zh-Hans.jpg` (+ `_fit.txt`), copy of `W/worldtext/art/sheets/`.
- Revision sheet: `sheets/exit_constructed_C_zh-Hans.jpg` (+ `_fit.txt`), copy of `W/worldtext/art/sheets/`; renders `W/worldtext/art/renders/exit_constructed` and `renders/exit_constructed_swap`; the traced set in `W/worldtext/art/tmp/exit/traced_C/`.
- Correction sheet: `sheets/exit_constructed_C_ink_zh-Hans.jpg` (+ `_fit.txt`), copy of `W/worldtext/art/sheets/` (there also `_measure.json`); composer `tools/compose_exit_ink_sheet.py`; renders `W/worldtext/art/renders/exit_inkfloor` and `renders/exit_inkfloor_swap`; the 05:51 constructed set in `W/worldtext/art/tmp/exit/constructed_em152/`.
- Side-air sheet: `sheets/exit_constructed_C_sideair_zh-Hans.jpg` (+ `_fit.txt`), copy of `W/worldtext/art/sheets/` (there also `_measure.json`); composer `tools/compose_exit_sideair_sheet.py`; renders `W/worldtext/art/renders/exit_sideair` and `renders/exit_sideair_swap`; the 100 % set in `W/worldtext/art/tmp/exit/sideair/ink100/`.
- Work: `W/worldtext/art/journal.txt` (every step, sha1s), `tools/` (legend, generators' mirror `exit_root/`, harness, composer, package builder), `tmp/exit/` (legends, maps, kit builds), `renders/`, `overlay_exit_C/`.
- Package: `W/apply/world_text_exit_zh.sh` (+ `_bases`, `_expected`, `_keep`, `_payload/`, `_base/`, `.diff`, `_HOLD.txt`), built by `W/worldtext/art/tools/package_tracks.py exit` (log `W/worldtext/art/logs/package_world_text_exit_zh_20261104_072805.txt`); combined-test frames `W/vl_pending/world_text_exit_zh.md`.
- Sources: `SOURCES.md` S13 (added).
