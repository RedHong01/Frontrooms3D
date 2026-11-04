# EXIT sign in another language: options and the build (world text, 简体中文 first)

Line 游戏视觉, world-text workflow, stage EXIT (account B, under the 游戏视觉 session local_a9a1dce2), 2026-11-03 20:24–21:2x. Status: **options rendered, WAIT-RED; the recommended option is built and packaged, nothing landed in main.**

**Red's ask (2026-11-03, 叙事设计 chat, verified there):** "目前切换成不同的语言，应该游戏里比如说对应的广告，平面视觉的type应该也需要做对应的改变，也就是所有的diegetic narratives 需要adapt 语言的变化。" Then "全部按推荐" on `research/narrative/95_world_text_localization.md` §6. That doc's Q2 (a), now decided, says the EXIT is translated (`安全出口`, `非常口`, `AUSGANG`) and "The red colour, the box and the light stay."

What this stage did: real-source research (web pages read, nothing downloaded), the legend traced from the WORLD_TYPE face, three constructions built as kits and maps, all rendered in the clone `W/proj_alarmtell` against today's English sign, one Shot sheet. It then built the recommendation for every EXIT kit and wrote the per-language model swap, gated with Roslyn mac + iOS. What it did not do: land anything, run Unity on main, download anything, or make a sound.

Reused, not redone: `10_spec.md` (construction, the 1990 codes S1–S12, lamp field), `30_integration.md` §2 (letter-share method), the world-text inventory fit numbers (`W/worldtext/art/journal.txt` 18:31), `research/localization/WORLD_TYPE.md` (faces and the sizing rule, 平面视觉's).

---

## 0. 给 Red：EXIT 的中文版怎么做（请选一个）

**怎么读、怎么回：** 先看图 `sheets/exit_world_text_options_zh-Hans.jpg`。第一列是今天的英文 EXIT，后面三列是三种中文做法，每一行的相机、灯光完全一样，只换牌子。可以直接回“全部按推荐”，也可以逐项回答下面三个问题。

**背景：** 英文 EXIT 是一块钢板，上面冲穿了 6 英寸（152 mm）高的字母，笔画 3/4 英寸（19 mm），后面是红色灯片和两个灯泡。95 号文件已经定了：中文版要翻译，但红色、盒子和灯光不变。问题是中文字比 EXIT 复杂。WORLD_TYPE 的字号规则是“中文字身 = 拉丁大写高 ÷ 0.70”，算下来字身约 218 mm。“安全出口”四个字需要 871 mm 宽，压到 85 % 的长体也要 740 mm，可盒子的开口只有 320 × 186 mm。规则的顺序是：先长体到 85 %，再请叙事设计签一个更短的词，最后才缩小字号。在这个盒子里，“安全出口”和“出口”都只能走到最后一步：“安全出口”缩到规则字号的 40 %，“出口”缩到 70 %。

| 选项 | 玩家看到什么 | 3 m | 8 m | 断电（电池两灯） | 近看 0.35 m | 成本 |
|---|---|---|---|---|---|---|
| A 全词 · 镂空 + 连筋 | “安全出口”冲穿钢板。“口”的中间、“全”的屋顶三角、“安”里的小三角会掉出来，所以留 4 条 3 mm 的连筋。字高 85 mm，是 EXIT 的 56 %。 | 能读 | 糊成一条红线 | 两头亮、中间暗（和英文一样，看得出在用电池） | 能看到连筋和孔壁，“全”的屋顶被劈开 | 三角面 4967 / 2261（超预算 42 %） |
| B 全词 · 印刷灯片 | 同样大小。不冲孔，灯片上印一层白墨，字是透光的红。没有连筋，字形完整。 | 能读 | 同样糊 | 同 A | 平的，没有孔壁的立体感，近看是另一种工艺的牌子 | 2952 / 926（预算内） |
| **C 短词 · 镂空（推荐）** | 两个字“出口”。字高 143 mm，是 EXIT 的 94 %；笔画 19 mm，和英文是同一个 3/4 英寸。只有“口”要两条 6 mm 的连筋。 | 能读 | 能读，和 EXIT 一样 | 两个字一起变暗（没有“中间”，电池信号弱一些） | 和英文同一种冲孔工艺 | 3302 / 1036（预算内） |

**推荐 C，理由：** 它是唯一在 8 m（1080p）还读得出来的中文做法，和英文同一种工艺、同一个盒子、同样的成本。而且 WORLD_TYPE 规则本身的下一步就是“请叙事设计签一个短词”。“EXIT”本身就只是“出口”。香港的双语出口牌写的正是“出口 EXIT”（香港消防处工作守则 §5.10）。

**诚实的缺点：**
- C 要叙事设计签“出口”这个短词，就像德语已经签了 AUSGANG 作 NOTAUSGANG 的短式。中国大陆的消防标志用的是“安全出口”。如果叙事设计不签，推荐退回 B：字形完整、成本低，代价是 8 m 读不出、近看是平的。
- 三种都去掉了英文板上那两个封着的箭头刻线，因为中文一行占满了开口。
- 字号都小于 WORLD_TYPE 的规则字号。这是 WORLD_TYPE 没覆盖到的情况，留给平面视觉确认。
- （打包阶段补充，11-03 21:4x）平面视觉在 20:28–20:45 给 WORLD_TYPE 加了一条“一个门只用一个词”（德语的 AUSGANG 就是这样定的）：一种语言一旦用了签过的短词，这种语言的每一张疏散图都印这个短词，TEXT 面板的同一行也说同一个词。95 号文件 §6 Q2 也写了“疏散图、图例和出口牌要一致”。所以选 C，不只是出口牌写“出口”：主工程里已有的简体中文疏散图（W1）楼梯门上印的“安全出口”要改印“出口”（平面视觉重出图，游戏视觉重新打补丁）；TEXT 面板“EXIT（楼梯门上方）”的两行，以及图例里的“楼梯（安全出口）”，也要请叙事设计改成“出口”。做完以后，牌子、疏散图、近看文字三处都说“出口”。选 B 的话这些都不用动，它们本来就写“安全出口”。

**请回答：**
1. 选 A、B 还是 C？
2. 如果选 C：是否请叙事设计签“出口”作 EXIT 的中文短式？签了以后，简体中文疏散图和 TEXT 面板也跟着改成“出口”（一个门只用一个词）。
3. 如果选 A：“全”的连筋放在屋顶尖（左右对称，现在的做法），还是从左边的撇穿过？

**已经做好、等你选：** C 已经按最高规格做完四种挂法（墙挂、端挂/旗挂、吊挂、贴顶吊挂）的中文模型、7 张贴图，以及切换代码（切换语言时已经挂着的牌子也会实时换）。全部打包好，**没有落地**。A 和 B 的墙挂款已经做好（用于渲染），选了以后用同一条命令出全套，大约 10 分钟。英文和 WebGL 完全不变。

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
- 出口: 100 % width, em 152.4 mm (= the English cap), ink 276.0 × 142.9 mm. That is 70 % of the rule's em; the ink is 94 % of EXIT's height.

Strokes (measured on the traced face): 安全出口 9.7 mm vertical, 10.4 mm horizontal; 出口 19.7 / 18.4 mm. The English is 19.05 mm.

## 2. Research: what the codes and the period say

- **US 1988–1990 (reused, S1–S6):** NFPA 101-1988 and UL 924-1989 ask for 6 in letters and 3/4 in strokes (S3, S4). UL 924 defines a stencil face as translucent letters on an opaque background (S4). By that definition a lens printed with an opaque ground and clear letters counts photometrically as a stencil (option B). In the period drawings S6, Type 606 is an edge-lit clear acrylic panel and Type 604 has red letters on a white diffuser. Printed and panel legends existed beside the cut-through stencil.
- **A real bilingual exit-sign rule (new, S13):** Hong Kong's Fire Services Department Code of Practice, §5.10 as amended by Circular Letter 2/2000, read as text. Its words: "The signs shall bear the words in English and Chinese of not less than 125 mm high … the Chinese characters shall be with width of vertical strokes not less than 15 mm and with width of horizontal strokes not less than 10 mm". Its Figure 1 pairs the words "EXIT" and "出口". It asks for translucent surrounds, with white lettering preferred for internally lit signs.
  - That size rule is not among the circular's three changes, so it stood in the Code before April 2000. Its first edition is not verified.
  - Read for us: a working code set the Chinese characters at the same height as the English letters, not 1.43 ×, and let the Chinese strokes go thinner than the Latin ones (15 / 10 mm against our 19 mm).
  - Against it: A and B are 85 mm tall with 9.7 / 10.4 mm strokes, below HK's 125 mm and 15 mm. C (143 mm, 19.7 / 18.4 mm) clears HK on both counts and matches the US 3/4 in stroke.
- **The word:** the zh-Hans table signs `EXIT` → `安全出口` (95 §5). HK's bilingual sign uses `出口`, the word EXIT itself means "exit", and 95 §5 already gives German a signed short form (NOTAUSGANG, short AUSGANG). So 出口 is the short form that WORLD_TYPE's step 2 asks 叙事设计 for. It is not signed yet.
- **Stencil ties (bridges):** EXIT has no closed counters (`10_spec.md` §5). The traced face has 3 islands in 安全出口: 安 0.025 em², the 全 roof triangle 0.047 em², 口 0.264 em². 出口 has one, 口. Ties run where the stroke between a counter and the open plate is thinnest. A small island gets one tie; an island of at least 0.06 em² gets two opposite ties. A tie is 0.30 × the vertical stem, at least 3 mm: 3.0 mm in A, 5.9 mm in C. Every reflex corner, the ties' included, takes the punch's R 0.5 fillet (`exit_sign_geom.HOLE_FILLET`).

## 3. The options as built

All three keep the housing, frame, screws, lamps, the diffuser and its lamp field, the materials and the driver. They differ only in the plate (or film), the face maps and the diffuser's insects. Face: Noto Sans SC VF at wght 700 (WORLD_TYPE R1 黑体). W/venv has no fontTools, so it is rendered by FreeType at 30 px/mm and traced: marching squares, Douglas–Peucker 0.04 mm, edges within 0.15 mm of an axis snapped square (`tools/exit_legend.py`).

| | A full word, stencil + ties | B full word, printed lens | C short word, stencil (REC) |
|---|---|---|---|
| Word | 安全出口 (signed) | 安全出口 (signed) | 出口 (needs 叙事) |
| Width, em | 85 %, 89.5 mm | 85 %, 89.5 mm | 100 %, 152.4 mm |
| Ink box | 291.9 × 85.1 mm, margins 14.1 / 50.4 | same | 276.0 × 142.9 mm, margins 22.0 / 21.6 |
| Strokes | 9.7 / 10.4 mm | 9.7 / 10.4 mm | 19.7 / 18.4 mm |
| Islands, ties | 3 islands, 4 ties of 3.0 mm (安 1 through 女's bar, 全 1 up through the roof apex, 口 2 left/right) | none: counters print whole | 1 island, 2 ties of 5.9 mm (口 top and bottom) |
| Construction | holes cut through the 0.9 mm plate, 0.2 chamfer, R 0.5 reflex fillets | no plate; an opaque white ink film 0.4 mm proud of the diffuser, characters cut out, counters left as islands, on the face material | as A |
| Chevron scores | dropped (no room) | dropped | dropped |
| Lamp field, rel. to the brightest character | 安 .38 全 .92 出 1 口 .32; max/min 9.3 (UL ≤ 40) | same | 出 1 口 .92 |
| BATTERY (DC pair) | 安 .27 全 .06 出 .06 口 .24: ends glow, middle dims, like EXIT | same | 出 .21 口 .24: even, dimmer |
| Letter shares A / B / DC | .486 / .514 / .938 | .490 / .510 / .938 | .556 / .444 / .914 |

English: shares .459 / .541 / .662. The driver now reads a language copy's shares from its sidecar, so the battery Level of C is 0.229 against English 0.166.

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

## 5. Cost

| | TODAY | A | B | C |
|---|---|---|---|---|
| Kit_ExitSign tris LOD0 / LOD1 | 3136 / 984 | 4967 / 2261 (+42 % / +106 %) | 2952 / 926 | 3302 / 1036 |
| Other kits, LOD0 | _End 5758, _Hanging 6502, _HangingFlush 6254 | not built yet (plate +~1830 per face) | not built yet | 6090, 6834, 6586 (all ≤ +15 %) |
| Maps | main's 10 | 7 copies: Face A/N/S, Diffuser A/E/F/S | same | same |
| Build size per language, desktop BC7 | 0 | full copies 10.7 MiB; in patch form C ships 7 patches, 3.1 MB in the package | same | same |
| Memory at run time | 0 | the bases hold the copies' pixels (no extra resident memory); 4 small model copies (meshes) while the language is on | same | same |
| Draws | 4 slots per renderer | unchanged | unchanged (the film uses the face slot) | unchanged |
| Language model copies | – | 4 FBX + sidecars | 4 | 4 (built) |

## 6. The build of C (packaged, not landed)

Package `W/worldtext/art/overlay_exit_C`, 88 files, 3.1 MB, built by `tools/build_exit_overlay.py --opt C --form patch`.
- `Assets/Resources/WorldText/zh-Hans/Props/Models/Kit_ExitSign{,_End,_Hanging,_HangingFlush}.{fbx,json}`, each with its base's .meta and a fresh GUID. These are the same kit modules built with the 出口 legend.
  - The _Dead variants need no copy: their LOD children carry the live kit's name and meshes.
  - The sidecars carry `exitSign.legend`, `legendFace`, `legendEmM`, `letterHeightM`, `strokeM`, `chevrons: none` and `letterShares`.
- 7 maps as patches (main's `pack_world_text_patch.py`): `ExitSign_Face_A/_N/_S`, `ExitSign_Diffuser_A/_E/_F/_S`.
- Code, all 游戏视觉's files. Roslyn tree main `--all` with the 4 overrides: mac COMPILE OK, 0 errors; iOS COMPILE OK, 0 errors.
  - `FrontRoomsWorldText.cs` gains a "models" kind (manifest list + `Model(path)`).
  - `FrontRoomsWorldTextImport.cs` lists `.fbx` copies that have a base kit.
  - `FrontRoomsKitImporter.cs` imports `WorldText/<code>/Props/Models/` exactly as a kit.
  - `FrontRoomsExitSign.cs` follows `FrontRoomsWorldText.Applied`. It takes the copy's meshes by LOD child name, puts its own back on English, and reads the copy's letter shares. WebGL never subscribes.
  - `FrontRoomsKitLibrary` is unchanged: it already adds the driver at Spawn, so a sign built in zh-Hans is born with its legend.
- Generators under `Tools/`. In each, English is byte for byte as before: the 10 textures and the 8 sidecars match main; the 8 FBX differ only in the writer's 131 timestamp bytes.
  - `lookdev/exit_legend.py`, new.
  - `lookdev/gen_exit_sign.py` and `Blender/frontrooms_kit/assets/exit_sign_geom.py`, `exit_sign_common.py`: `FR_EXIT_LEGEND=<legend json>` switches the legend.
  - The legend file `lookdev/exit_legend_zh-Hans.json`.
- Manifest over main and the overlay, from `W/worldtext/build_w1_overlay.py`, now extended with models. zh-Hans: 28 swap, 18 patch, 18 own, 12 texts, 4 models; refused 0.

Unity check in the clone (import, then the swap; `logs/exit_swapcheck_2115.log`):
- 0 error CS. Preview(zh-Hans) gave Swapped 46, Refused 0.
- Signs built in English switched live to 出口: Kit_ExitSign 3136 → 3302 tris, _Hanging, _End and _Dead likewise. A sign spawned while zh-Hans is on screen is born with 出口. Back to English, the meshes and shares return.
- After going back to English in edit mode, the face maps render blank. This is the world-text Restore finding the Copies stage reported, an Editor stale-binding effect, not this package.

Known limit: the ray-traced glass keeps the meshes it registered. A sign already built when the language changes reflects its old legend in glass until its chunk is rebuilt.

## 7. Open

- **Red:** A, B or C (§0).
- **叙事设计:** sign `出口` as the zh-Hans short form of EXIT, if Red picks C.
- **One word for one door (added by the Package stage, 21:4x):** WORLD_TYPE §3 gained this rule at 11-03 20:28–20:45 (平面视觉; de AUSGANG), after this doc's research read the file: once a language uses its signed short word, every sheet of the language prints it and the TEXT panel's shared row says it too (95 §6 Q2: "the plan, legend and sign agree"). With C, the zh-Hans evacuation plans (W1, in main: 平面视觉's `tutorial_placard.swift` `EXIT_SHORT` / `EXIT_SHORT_EVERYWHERE`, then 游戏视觉 re-packs their patches) and the zh-Hans table rows `EXIT (over the stair door)`, its padlocked twin and the LEGEND rows' 楼梯（安全出口） (叙事) follow 出口. B and A need none of this.
- **平面视觉:** the sizes below WORLD_TYPE's rule size in a kept box (§1), which WORLD_TYPE does not settle. Also: Noto Sans SC 700 outlines as the stencil legend, where the English legend is a constructed die face (`10_spec.md` §5 asks 平面视觉 to review the face). And the tie places.
- **Landing order:** the apply package is `W/apply/world_text_exit_zh.sh` (v2, 88 paths, DRY RUN OK on main at 21:39, held by `world_text_exit_zh_HOLD.txt`). It lands the manifest only together with its code. Its manifest cannot merge over `world_text_ours_zh`'s (one conflict at the end of the zh-Hans patch list), so after any other world-text landing re-run `W/venv/bin/python W/worldtext/art/tools/package_tracks.py exit` first: it rebuilds the manifest over main as it is, 3-way merges the four C# files if main moved them, re-gates (Roslyn mac + iOS) and dry-runs. Red's Editor also rebuilds the manifest on import.
- **The other eight languages:** the same pipeline. `exit_legend.py` takes their words and WORLD_TYPE faces once signed (ja 非常口, ko 비상구, zh-Hant 緊急出口; the Latin and Cyrillic words need a constructed legend or Heros, 平面视觉's call).

## 8. Files

- Sheet: `sheets/exit_world_text_options_zh-Hans.jpg` (+ `_fit.txt`), copy of `W/worldtext/art/sheets/`.
- Work: `W/worldtext/art/journal.txt` (every step, sha1s), `tools/` (legend, generators' mirror `exit_root/`, harness, composer, package builder), `tmp/exit/` (legends, maps, kit builds), `renders/`, `overlay_exit_C/`.
- Package: `W/apply/world_text_exit_zh.sh` (+ `_bases`, `_expected`, `_keep`, `_payload/`, `_base/`, `.diff`, `_HOLD.txt`), built by `W/worldtext/art/tools/package_tracks.py exit`; combined-test frames `W/vl_pending/world_text_exit_zh.md`.
- Sources: `SOURCES.md` S13 (added).
