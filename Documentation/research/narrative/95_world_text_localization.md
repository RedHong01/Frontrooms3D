# 95 · World text follows the language: rules, inventory, glossary (叙事设计, 2026-11-03)

**Red, 2026-11-03, 叙事设计 chat:** "目前切换成不同的语言，应该游戏里比如说对应的广告，平面视觉的type应该也需要做对应的改变，也就是所有的diegetic narratives 需要adapt 语言的变化。"

This **replaces D2 of `research/localization/README.md`** ("the world stays English", Red 10-30 20:47 "全部按推荐"). From now on, everything printed, lettered, lit or broadcast inside the building is shown in the language the player picked. That README's §3 table, rows "Office terminals", "Printed sheets…", "TV ad", is superseded by §3 here; 设置菜单 owns that file and updates it.

Words are this line's. Faces, layout and art are 平面视觉's (and its three sub-lines). Textures and swapping are 游戏视觉's. Tables are 设置菜单's. The TV ad is 广告's. Terminal pages are this line's JSON; their face is 平面视觉's.

## 1. The rule: a dubbed film, not a moved building

**Frame A (Red, 2026-11-03, "全部按推荐"):** the building is still a US office of 1990, and only the language of its words changes, the way a dubbed film keeps its city and changes its speech.

| Changes with the language | Stays as it is in every language |
|---|---|
| Every word a sign, plate, plan, form, screen, card, tag or ad says | Proper names: `FRONT ROOMS` (store and game), `WTCZ-TV 61`, `FAIRVIEW`, `TRI-COUNTY`, people's initials (`D.K.`) |
| The floor word: the world uses **the HUD's own row** (`LEVEL B{0}` → `第 B{0} 层`, `EBENE B{0}`, `УРОВЕНЬ B{0}`), so the stair sign, the plan, the tag and the HUD agree (one name per thing) | Tokens: `B1`, `A-2`, the stair letter `A`, sheet and zone numbers |
| The EXIT sign (§6 Q2) | US conventions: `555-01xx` numbers, `$499`, dates `11/89`, 24-hour log times `0200`, inches and feet |
| Translated typography in the period look of that script (§4) | Keys and hardware: phone keypads, key caps, the stencilled `0` and `B1` numerals |
| | The FRONTROOMSSS logo; the wallpaper (no words) |

- **Why A:** it keeps every signed fact true: UBC and LL5 forms, WTCZ, the 1990 era lock and the US floor scheme. Only the words move. B (a Japanese building in Japan, a German one in Germany) would need a new plan language, new codes, a new TV station and new research for each of nine languages.
- **Truth rules hold in every language:** never RUN or HIDE as advice, nothing that says the dark hides you, never "relay", no live threat information, flat building voice. A translation that adds advice or drama is wrong even when it reads well.
- **The TEXT panel stays** (accessibility, close-ups). It now shows the same words the object shows. Its rows that say "EXIT stays" are rewritten (§5).

## 2. What the player must find on the first floor, in their language

Order of importance for the first build, so a partial delivery still teaches the game:

1. The evacuation plan (sheet A, all variants) and its four lines.
2. The EXIT sign, the key plate (`STAIR KEY` / `SILENT ALARM…`), the key tag.
3. Stair signs: ID sign, `NO REENTRY` plates, `NOT AN EXIT`, `AREA OF RESCUE ASSISTANCE`.
4. The TV ad supers and `STAY TUNED.`
5. Station paper, terminals, the annunciator, the business-hours plate, the ALARM ZONE card, and the alarm door placard when it ships.

## 3. Inventory: every world text, where it lives, who changes it

Checked in main 2026-11-03 (`Assets/Resources`, `Assets/StreamingAssets`, `Tools/print`, research docs). "Baked" = letters drawn into a texture, so each language needs its own texture or a glyph layer.

| # | Item | Asset today | Strings (source) | Pipeline | Art and type | Words |
|---|---|---|---|---|---|---|
| W1 | Evacuation plan, every sheet: room, room Phase 0, landing, landing Phase 0, B2–B9, B10+ | `Surfaces/Textures/Prop_EvacPlan_*` (+ `_E` glow); source `Tools/print/ink/art_from_graphic/placard/` | `40` §3, §9 | baked, generator `tutorial_placard.swift` | 平面视觉 | ~60 |
| W2 | EXIT sign face | `ExitSign_Face_*`, `ExitSign_Diffuser_*` | `EXIT` | baked, lit | 游戏视觉 + 平面视觉 | 1 |
| W3 | Key board plate | `Prop_StationKeyPlate_A`, `Prop_SignEngraved_*` | `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` (`40` §9) | baked, engraved | 平面视觉 | 6 |
| W4 | Key tag and desk fob | `Prop_KeyTagNo_A` (atlas band) | `STAIR A` (`40` §4) | baked, `gen_keytag_atlas.py` | 平面视觉 → 游戏视觉 | 2 |
| W5 | Stair ID sign, terminus, floor | `Print/Stair/id_*_glyphs`, `rt_id_*` | `STAIR A` / `LEVEL 0` / `LEVELS 0 TO B1` / `NO ROOF ACCESS` (`40` §5) | glyph atlas + json | 平面视觉 · 信息设计 | 8 |
| W6 | Re-entry plates | `Print/Stair/exit_door_to_glyphs`, `far_door_*`, `rt_exit_door_*` | `NO REENTRY` / `NEAREST REENTRY ON LEVEL B1`, `REENTRY ON THIS FLOOR` | glyph atlas + baked | 信息设计 | 9 |
| W7 | `NOT AN EXIT` | `Print/Stair/not_an_exit_*`, `Run_StencilNotExit_A` | `40` §5 | baked | 信息设计 | 3 |
| W8 | Rescue sign | `Print/Stair/rescue_face_*` | `AREA OF RESCUE ASSISTANCE` + ISA symbol | baked, lit | 信息设计 | 4 |
| W9 | Stair letter | `Print/Stair/stair_letter_A_*` | `STAIR A` | baked | 信息设计 | 2 |
| W10 | Business-hours plate | `Prop_PlateBusinessHours_*` | `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` | baked, engraved | 平面视觉 | 8 |
| W11 | Desk lettering, tent card | station kit | `SECURITY`, `PLEASE SIGN IN` | baked | 平面视觉 | 3 |
| W12 | Station paper: register, sign-out, log, phone list, post orders | `Prop_StationPaper_A` + `Prop_StationPaper_Dates.json` | `80` §3, `station/01` §6 | baked + runtime date slots | 平面视觉 | ~150 |
| W13 | Annunciator labels | `Prop_AnnunciatorLabel_A` | `90` §1 | baked, `make_labels.py` | 平面视觉 | 12 |
| W14 | ALARM ZONE card | `Print/AlarmTell/alarm_zone_card_*` | `ALARM` / `ZONE` (`90` §2) | baked, lit | 信息设计 → 游戏视觉 | 2 |
| W15 | Alarm door placard (flag off) | `Print/AlarmDoor/alarm_door_placard_albedo` | `ALARM WILL SOUND` (`90` §4) | baked | 信息设计 | 3 |
| W16 | Electrical room plate (Run!) | `Run_PlateElectrical_A` | `ELECTRICAL` / `ROOM` / `AUTHORIZED PERSONNEL ONLY` (§5.2) | baked | 游戏视觉 | 5 |
| W17 | `NO SMOKING`, other small labels | `Prop_SignEngraved_*`, `Prop_Label_*`, `Prop_VendingHeader_*` | `EMPLOYEES ONLY`, `STAFF ONLY`, `STORAGE`, `NO SMOKING`, the vending header, box stencils, file tabs; maker's plates stay (§5.2) | baked | 游戏视觉 / 平面视觉 | ~30 |
| W18 | Office terminals | `Terminal/Content/{interoffice,terminal_1..3}.json` | `70` | runtime text in the C20 8×16 face | strings 叙事; face 平面视觉 | ~2,000 |
| W19 | TV ad, station slate, `STAY TUNED.` | `StreamingAssets/FrontRooms_Ad_01.mp4`, `Media/CRT_AdSurface.mat` | `screens/30_screen_strings.md` | video (supers burned in? 广告 to confirm) + audio | 广告 | ~60 |
| W20 | Playtest forms | `Print/` (InDesign) | `80` §1 | print | 印刷与演示 | not world: they stay English for the 11-03/04 round |

**Pipeline (游戏视觉, LANDED 2026-11-03 17:08: `FrontRoomsWorldText`, `Editor/Rendering/FrontRoomsWorldTextImport`, stair-sign re-bake).**
- A language's copy of baked art lives at `Resources/WorldText/<code>/<the base's path under Resources>`.
- Non-readable bases (W1-W4, W10-W17: plans, signs, plates, cards, tags) are swapped into the base on the GPU, so each copy **keeps the base's pixel size**.
- Readable ones (W5-W9: stair plates and glyph atlases) are loaded by code, may differ in size, and carry their slot layout json beside them.
- The language switch is live. English reloads from disk. WebGL gets nothing.
- Cost: outside English the swapped bases stay resident, and each language's copies ship in the build, so size grows per language.
- For the words: nothing changes. A string that will not fit a fixed-size base comes back to this line for a shorter fallback.

Not world text, already translated: the HUD, prompts, captions, CAUGHT, settings, the TEXT panel (tables).

## 4. Type: the period look in each script (平面视觉's call; this is the brief)

- Each script needs the face a 1990 sign shop or office in that language would have used, holding the same role as today's face (Heros for signs, Courier Prime for typed paper, the engraved plate face, C20 for the CRT). CJK, Hangul and Cyrillic signs of 1990 used gothic (sans) faces for signage and Mincho or Song for typed or printed text.
- **Terminals:** the C20 face is ASCII by design. A 1990 terminal in each language had its own bitmap face: PC-9801 JIS kanji 16×16 (ja), CCDOS GB2312 16×16 (zh-Hans), ETen Big5 (zh-Hant), a KS C 5601 Hangul set (ko), code page 866 (ru), code page 850 (de, fr, es, pt-BR). A full-width glyph takes two 8×16 cells. That keeps the era and the grid. Licences are 平面视觉's check (no GPL fonts in the build).
- Lengths: German and Russian run longer, CJK shorter. Every fixed plate (EXIT, ALARM ZONE, key plate, annunciator strip) needs a fit check per language. Where a word cannot fit, the fallback column in §5 is shorter and still signed.

## 5. Glossary: the world words in nine languages

The table rows already used by the HUD and the TEXT panel win: the world reuses them word for word. New words below are **AI drafts by 叙事设计** (Red 10-31: "10 种语言都用 AI 初稿直接上线"). 简体中文 is the reviewed column once Red reads it.

**Rows to change in the tables (设置菜单):** `EXIT (over the stair door)` and its padlocked twin (note says "EXIT stays"; now the sign says the word below); `EVACUATION PLAN · LEVEL 0` and `LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89` keep `LEVEL 0` in English and should use the floor row (`第 0 层`); `STAIR (EXIT)` inside the LEGEND rows.

| English (signed) | zh-Hans | zh-Hant | ja | ko | de | fr | es | pt-BR | ru |
|---|---|---|---|---|---|---|---|---|---|
| `EXIT` | 出口 (signed short form, see below; full form 安全出口) | 出口 (signed short form, see below; full form 緊急出口) | 出口 (short; full form 非常口) | 출구 (short; full form 비상구) | AUSGANG (signed; full form NOTAUSGANG) | SORTIE | SALIDA | SAÍDA | ВЫХОД |
| `NOT AN EXIT` | 非出口 | 非出口 | 出口ではありません | 출구 아님 (was 비상구 아님 until 11-04) | KEIN AUSGANG | PAS UNE SORTIE | NO ES SALIDA | NÃO É SAÍDA | НЕ ВЫХОД |
| `AREA OF RESCUE ASSISTANCE` | 救援等候区 | 救援等候區 | 救助待機場所 | 구조 대기 구역 | WARTEBEREICH FÜR RETTUNG | ZONE D'ATTENTE DES SECOURS | ÁREA DE ESPERA DE RESCATE | ÁREA DE ESPERA DE RESGATE | ЗОНА ОЖИДАНИЯ СПАСАТЕЛЕЙ |
| `NO ROOF ACCESS` | 不通屋顶 | 不通屋頂 | 屋上出入不可 | 옥상 출입 불가 | KEIN ZUGANG ZUM DACH | PAS D'ACCÈS AU TOIT | SIN ACCESO A LA AZOTEA | SEM ACESSO À COBERTURA | ВЫХОДА НА КРЫШУ НЕТ |
| `DN` (the stair's run arrow on the plan; no table row yet) | 下 | 下 | 下 | 아래 | AB | DESC. | BAJA | DESCE | ВНИЗ |
| `LEVEL` (stair ID sign: small label over the big floor token, which stays) | 楼层 | 樓層 | 階 | 층 | EBENE | NIVEAU | NIVEL | NÍVEL | ЭТАЖ |
| `LEVELS 0 TO B1` | 第 0 层至第 B1 层 | 第 0 層至第 B1 層 | 0階〜B1階 | 0층~B1층 | EBENEN 0 BIS B1 | NIVEAUX 0 À B1 | NIVELES 0 A B1 | NÍVEIS 0 A B1 | УРОВНИ 0–B1 |
| `NEAREST REENTRY ON LEVEL B1` | 最近返回点：第 B1 层 | 最近返回點：第 B1 層 | 最寄りの再入場：B1階 | 가장 가까운 재진입: B1층 | NÄCHSTER WIEDEREINTRITT: EBENE B1 | RETOUR LE PLUS PROCHE : NIVEAU B1 | REINGRESO MÁS CERCANO: NIVEL B1 | REENTRADA MAIS PRÓXIMA: NÍVEL B1 | БЛИЖАЙШИЙ ВХОД: УРОВЕНЬ B1 |
| `REENTRY ON THIS FLOOR` | 可由此返回本层 | 可由此返回本層 | この階へ再入場可 | 이 층으로 재진입 가능 | WIEDEREINTRITT AUF DIESER EBENE | RETOUR À CET ÉTAGE | REINGRESO EN ESTE PISO | REENTRADA NESTE ANDAR | ВХОД НА ЭТОТ ЭТАЖ |
| `SILENT ALARM: REMOVAL SIGNALS PANEL` | 无声警报：取下即通报控制盘 | 無聲警報：取下即通報控制盤 | 無音警報：取り外すと盤に通報 | 무음 경보: 제거 시 패널에 신호 | STILLER ALARM: ENTNAHME MELDET AN ZENTRALE | ALARME SILENCIEUSE : RETRAIT SIGNALÉ AU TABLEAU | ALARMA SILENCIOSA: RETIRARLA AVISA AL PANEL | ALARME SILENCIOSO: RETIRAR AVISA A CENTRAL | ТИХАЯ ТРЕВОГА: СНЯТИЕ ПЕРЕДАЁТ СИГНАЛ НА ПУЛЬТ |
| `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` | 营业时间内此门不得上锁 | 營業時間內此門不得上鎖 | 営業時間中は施錠しないこと | 영업시간 중 이 문을 잠그지 마십시오 | DIESE TÜR WÄHREND DER GESCHÄFTSZEIT NICHT VERSCHLIESSEN | CETTE PORTE DOIT RESTER DÉVERROUILLÉE PENDANT LES HEURES D'OUVERTURE | ESTA PUERTA DEBE PERMANECER SIN LLAVE EN HORARIO COMERCIAL | ESTA PORTA DEVE FICAR DESTRANCADA NO HORÁRIO COMERCIAL | В РАБОЧЕЕ ВРЕМЯ ДВЕРЬ НЕ ЗАПИРАТЬ |
| `SECURITY` (desk) | 保安 | 警衛 | 警備 | 경비 | SICHERHEITSDIENST | SÉCURITÉ | SEGURIDAD | SEGURANÇA | ОХРАНА |
| `PLEASE SIGN IN` | 请登记 | 請登記 | ご記帳ください | 방문 기록을 남겨 주십시오 | BITTE EINTRAGEN | VEUILLEZ SIGNER LE REGISTRE | FIRME AL ENTRAR | FAVOR ASSINAR | ОТМЕТЬТЕСЬ, ПОЖАЛУЙСТА |
| `ALARM` / `ZONE` (card, two lines) | 警报 / 区域 | 警報 / 區域 | 警報 / 区域 | 경보 / 구역 | ALARM- / ZONE | ZONE / D'ALARME | ZONA DE / ALARMA | ZONA DE / ALARME | ЗОНА / ТРЕВОГИ |
| `ALARM WILL SOUND` | 开门即报警 | 開門即報警 | 開けると警報が鳴ります | 문을 열면 경보가 울립니다 | ÖFFNEN LÖST ALARM AUS | ALARME À L'OUVERTURE | SONARÁ LA ALARMA | O ALARME VAI SOAR | СРАБОТАЕТ СИГНАЛИЗАЦИЯ |
| `TROUBLE` | 故障 | 故障 | 故障 | 고장 | STÖRUNG | DÉFAUT | AVERÍA | FALHA | НЕИСПРАВНОСТЬ |
| `POWER` | 电源 | 電源 | 電源 | 전원 | BETRIEB | SOUS TENSION | ENCENDIDO | ENERGIA | ПИТАНИЕ |
| `SPARE` | 备用 | 備用 | 予備 | 예비 | RESERVE | RÉSERVE | RESERVA | RESERVA | РЕЗЕРВ |
| `SPRINKLER FLOW` | 喷淋水流 | 灑水水流 | スプリンクラー流水 | 스프링클러 유수 | SPRINKLER-DURCHFLUSS | DÉBIT SPRINKLEURS | FLUJO ROCIADORES | FLUXO SPRINKLER | ПОТОК СПРИНКЛЕРОВ |
| `STAY TUNED.` | 敬请锁定。 | 敬請鎖定。 | チャンネルはそのまま。 | 채널 고정! | BLEIBEN SIE DRAN. | RESTEZ À L'ÉCOUTE. | NO SE VAYA. | NÃO SAIA DAÍ. | ОСТАВАЙТЕСЬ С НАМИ. |

**zh-Hans EXIT = 出口, signed 2026-11-04 (叙事设计).** Red chose C in 游戏视觉's chat, 11-04 08:07: "按推荐选 C" (verified in that transcript). C is the die-cut legend 出口 at EXIT's ink height (`research/exit_sign/80_world_text_options.md` §0). 安全出口 is the full form and fits the kept box only at 40 % of the rule size. So, as de's AUSGANG for NOTAUSGANG, the short form is signed (WORLD_TYPE §3's step two).
- Why it holds: EXIT itself only says "way out", and 出口 says the same. Hong Kong's bilingual signs read 出口 EXIT. It is a real sign word, not a coinage. It stays distinct from `NOT AN EXIT` 非出口, which a player sees as the same word negated.
- **One word for one door:** in zh-Hans, every place that names the stair door says 出口. That covers the sign (游戏视觉, `W/apply/world_text_exit_zh`), the 24 evacuation sheets and the legend's STAIR (EXIT) (平面视觉 re-prints, EXIT_SHORT), and the table rows. The table rows `EXIT (over the stair door)` and its padlocked twin, both LEGEND rows and the world row `EXIT` are 出口 (landed 叙事设计 11-04 08:08; no 安全出口 left in zh-Hans.txt).
- **zh-Hant EXIT = 出口 too, signed 2026-11-04.** 游戏视觉 measured 緊急出口 at EXIT's ink height: it gets about 26 % of the width it needs, even at 长体 85 %, so it fails the way 安全出口 does. 出 and 口 are the same characters in both scripts, so zh-Hant reuses the zh-Hans build. The zh-Hant rows are changed the same way (landed 叙事设计 11-04; no 緊急出口 left in zh-Hant.txt). zh-Hant's sheets and legend print 出口 too.
- **ja EXIT = 出口, ko EXIT = 출구, signed 2026-11-04.** 平面视觉 measured 非常口 and 비상구 at option C's floor (em 162.6 mm, 长体 85 % at most): 414 mm against a 278 mm box. Japanese 出口 uses the zh-Hans legend unchanged. For ko 출구, 平面视觉 draws 출 (proof first).
  - The trade, weighed: in Japan and Korea the green emergency-exit sign says 非常口 / 비상구, and 出口 / 출구 is the station or shop "way out". That is still the right word here. The sign is the American red `EXIT`, and the English says "exit", not "emergency exit". Frame A translates what the object says, not the local sign code. And the player's most important word must read at 8 m.
  - **ko `NOT AN EXIT` becomes 출구 아님**, so that one door has one word. 信息设计 re-bakes `WorldText/ko/Print/Stair/not_an_exit_*`. ja's 出口ではありません already matches.
  - Table rows changed in ja.txt and ko.txt (EXIT ×2, LEGEND ×2, world EXIT; ko NOT AN EXIT). No 非常口 / 비상구 is left. Landed 叙事设计 11-04.
- **de EXIT = AUSGANG everywhere, signed 2026-11-04.** 总控 decided it on 11-03 at 20:42, on 平面视觉's recommendation: the W1 placard and the EXIT kits set AUSGANG, because NOTAUSGANG does not fit. One word for one door, so the de table says AUSGANG too. That covers the TEXT panel's EXIT ×2, both LEGEND rows `TREPPE (AUSGANG)` and the world row `EXIT`. `KEIN AUSGANG` already matches. No NOTAUSGANG is left in de.txt (landed 叙事设计 11-04).

**YOU ARE HERE (平面视觉's question, 2026-11-03).** The plan prints it twice, beside the dot and in the legend, so both must match. The standalone rows win: zh-Hans `您所在位置` (the usual wording on a Chinese evacuation plan), zh-Hant `您在此處`. The `LEGEND: …` rows in those two tables change from `你在这里` and `你在這裡`. The other seven languages already match.

**The floor label (信息设计's question, 2026-11-03).**
- On the stair ID sign the word stands alone over a 5 in token, so it is the bare noun, not the HUD's `第 {0} 层` frame.
- ja, ko and ru take the building word (`階`, `층`, `ЭТАЖ`). A building there says that; the tables' `レベル`, `레벨`, `УРОВЕНЬ` read like a game.
- The HUD rows should follow, for one name per thing: ja `B{0}階`, ko `B{0}층`, ru `ЭТАЖ B{0}`, and the matching `0` rows. That is 设置菜单's table change, signed here.
- **Line breaks signed as 信息设计 set them (zh-Hans):** 可由此返回 / 本层; 禁止返回 / 最近返回点： / 第 B1 层; 救援 / 等候区; 警报 / 区域. Alarm door placard: 开门即报警, lettered.

Already in the tables, reuse as they are: `STAIR A`, `STAIR KEY`, the four plan lines, `IN CASE OF ALARM`, `YOU ARE HERE`, `SECURITY DESK`, `NO REENTRY`, `THIS FLOOR MAY DIFFER FROM PLAN`, the warden line, `LEVEL 0`, `LEVEL B{0}`.

**Fit revisions (信息设计's measurements, 2026-11-03; these win over the rows above):**

| Sign | Language | Signed now | Why |
|---|---|---|---|
| Alarm door placard `ALARM WILL SOUND` | ja | 開けると警報 | 6 characters; the usual short sign form |
| | ko | 열면 경보 울림 | sign register, shorter |
| | de | ALARMGESICHERT | the standard German door sign, 14 letters |
| | fr | SOUS ALARME | as on French doors; 11 letters |
| | ru | НА СИГНАЛИЗАЦИИ | the usual door sign, 15 letters |
| | es, pt-BR | unchanged | tracking only |
| Exit-door verdict `NO REENTRY` | de | KEIN ZUTRITT | a standard sign; the plates' family becomes ZUTRITT |
| | ru | ВХОДА НЕТ | a standard sign |
| Far-door verdict `REENTRY ON THIS FLOOR` | de | ZUTRITT / AUF DIESER EBENE | same family |
| Exit-door detail `NEAREST REENTRY ON LEVEL B1` | de | NÄCHSTER ZUTRITT: EBENE B1 | same family |
| `NOT AN EXIT` | ja | 出口ではありません, unchanged | set 2 % smaller; the polite form is the sign form |
| Floor words | ru | ЭТАЖИ 0–B1, БЛИЖАЙШИЙ ВХОД: ЭТАЖ B1 | the tables' ЭТАЖ (floor label above) wins over this file's УРОВНИ / УРОВЕНЬ |

**Plan fixes (平面视觉's nine-language build, 2026-11-03):**
- **One word for the security desk.** The plan prints it twice, beside the room and in the legend, and line 4, the terminals and the station paper already use the desk word. So `SECURITY DESK` follows line 4: pt-BR `POSTO DE SEGURANÇA` (not PORTARIA), ja `警備デスク` (not 守衛室), ko `경비 데스크` (not 경비실). The other six already agree.
- **es line 4, shortened to set at full size:** `4 PUERTAS DE ESCALERA CERRADAS. LLAVE EN EL PUESTO DE SEGURIDAD.` The key in the same line still says why they are shut. The TEXT panel row is the same key, so it changes too.

**Business-hours plate (W10) line breaks:** 信息设计's breaks are signed for eight languages as set. pt-BR keeps `NO` with its noun: `ESTA PORTA DEVE / FICAR DESTRANCADA / NO HORÁRIO COMERCIAL`, the same pattern as es. The words are unchanged.

The TEXT panel rows `NO REENTRY (the door into this floor)` change with them: de `KEIN ZUTRITT (die Tür in dieses Stockwerk)`, ru `ВХОДА НЕТ (дверь на этот этаж)`. 信息设计's line breaks are signed as set: the rescue sign in three lines (two in de), the far door split as listed in its message, ja and ko keep their word order.

### 5.2 Signs read off the art (W16, W17)

Read off the textures in main 2026-11-03. Manufacturer plates stay as they are, like key caps: `MODEL 4417-B`, `MADE IN U.S.A.`, `QC 03 / 1979`, `MODEL CT-2104`, `120V~ 60Hz 95W`, `SER.`, `ASSET 004193`, `INSPECTED No. 7`. Filing ranges `A – C` … `L – P`, the years, and the pictograms stay too.

| English | zh-Hans | zh-Hant | ja | ko | de | fr | es | pt-BR | ru |
|---|---|---|---|---|---|---|---|---|---|
| `ELECTRICAL` / `ROOM` | 配电室 | 配電室 | 電気室 | 전기실 | ELEKTRORAUM | LOCAL ÉLECTRIQUE | CUARTO ELÉCTRICO | SALA ELÉTRICA | ЭЛЕКТРОЩИТОВАЯ |
| `AUTHORIZED PERSONNEL ONLY` | 非工作人员禁止入内 | 非工作人員禁止進入 | 関係者以外立入禁止 | 관계자 외 출입금지 | ZUTRITT NUR FÜR BEFUGTE | ACCÈS RÉSERVÉ AU PERSONNEL AUTORISÉ | SOLO PERSONAL AUTORIZADO | SOMENTE PESSOAL AUTORIZADO | ТОЛЬКО ДЛЯ ПЕРСОНАЛА |
| `EMPLOYEES ONLY` | 员工专用 | 員工專用 | 従業員専用 | 직원 전용 | NUR FÜR MITARBEITER | RÉSERVÉ AUX EMPLOYÉS | SOLO EMPLEADOS | SOMENTE FUNCIONÁRIOS | ТОЛЬКО ДЛЯ СОТРУДНИКОВ |
| `STAFF ONLY` | 闲人免进 | 閒人免進 | スタッフ専用 | 직원 외 출입금지 | NUR PERSONAL | PERSONNEL UNIQUEMENT | SOLO PERSONAL | APENAS PESSOAL | СЛУЖЕБНОЕ ПОМЕЩЕНИЕ |
| `STORAGE` | 储藏室 | 儲藏室 | 倉庫 | 창고 | LAGER | RÉSERVE | ALMACÉN | DEPÓSITO | КЛАДОВАЯ |
| `NO SMOKING` | 禁止吸烟 | 禁止吸菸 | 禁煙 | 금연 | RAUCHEN VERBOTEN | DÉFENSE DE FUMER | PROHIBIDO FUMAR | PROIBIDO FUMAR | НЕ КУРИТЬ |
| `SNACKS • CANDY • CHIPS` | 零食 • 糖果 • 薯片 | 零食 • 糖果 • 洋芋片 | スナック • キャンディ • チップス | 스낵 • 사탕 • 칩 | SNACKS • SÜSSES • CHIPS | EN-CAS • BONBONS • CHIPS | SNACKS • DULCES • PAPITAS | SALGADINHOS • DOCES • BATATAS | СНЕКИ • КОНФЕТЫ • ЧИПСЫ |
| `FRAGILE` | 易碎 | 易碎 | ワレモノ | 깨짐 주의 | ZERBRECHLICH | FRAGILE | FRÁGIL | FRÁGIL | ХРУПКОЕ |
| `THIS SIDE UP` | 此面向上 | 此面朝上 | 天地無用 | 이쪽이 위 | OBEN | HAUT | ESTE LADO ARRIBA | ESTE LADO PARA CIMA | ВЕРХ |
| `KEEP DRY` | 防潮 | 防潮 | 水濡れ注意 | 습기 주의 | VOR NÄSSE SCHÜTZEN | CRAINT L'HUMIDITÉ | MANTENER SECO | MANTER SECO | БЕРЕЧЬ ОТ ВЛАГИ |
| `PAYROLL` | 工资 | 薪資 | 給与 | 급여 | LOHN | PAIE | NÓMINA | FOLHA | ЗАРПЛАТА |
| `MISC` | 杂项 | 雜項 | その他 | 기타 | DIVERSES | DIVERS | VARIOS | DIVERSOS | РАЗНОЕ |
| `Q3 REPORTS` | 第三季度报告 | 第三季報告 | 第3四半期報告 | 3분기 보고서 | BERICHTE Q3 | RAPPORTS T3 | INFORMES T3 | RELATÓRIOS T3 | ОТЧЁТЫ ЗА 3 КВ. |
| `MINUTES` | 会议记录 | 會議記錄 | 議事録 | 회의록 | PROTOKOLLE | PROCÈS-VERBAUX | ACTAS | ATAS | ПРОТОКОЛЫ |
| `CLIENT FILES` | 客户档案 | 客戶檔案 | 顧客ファイル | 고객 파일 | KUNDENAKTEN | DOSSIERS CLIENTS | ARCHIVOS DE CLIENTES | ARQUIVOS DE CLIENTES | ДЕЛА КЛИЕНТОВ |

### 5.3 Station paper (W12; `build_atlas.py` strings, 平面视觉 re-bakes)

Initials, times, key numbers, room numbers and `911` stay. The visitors' columns are handwriting (Rock Salt today), so each script needs a hand face too (§4). `SECURITY DESK` reuses the table's line-4 word in each language.

| English | zh-Hans | zh-Hant | ja | ko | de | fr | es | pt-BR | ru |
|---|---|---|---|---|---|---|---|---|---|
| Register heads `DATE · NAME · COMPANY · VISITING · TIME IN · TIME OUT` | 日期 · 姓名 · 单位 · 拜访 · 进入 · 离开 | 日期 · 姓名 · 公司 · 拜訪 · 進入 · 離開 | 日付 · 氏名 · 会社名 · 訪問先 · 入館 · 退館 | 날짜 · 성명 · 회사 · 방문처 · 입실 · 퇴실 | DATUM · NAME · FIRMA · BESUCH BEI · EIN · AUS | DATE · NOM · SOCIÉTÉ · VISITE · ARRIVÉE · DÉPART | FECHA · NOMBRE · EMPRESA · VISITA A · ENTRADA · SALIDA | DATA · NOME · EMPRESA · VISITA · ENTRADA · SAÍDA | ДАТА · ФАМИЛИЯ · ОРГАНИЗАЦИЯ · К КОМУ · ПРИХОД · УХОД |
| Companies `COURIER · COPIER SVC · HVAC SVC · WATER SVC · OFFICE SUPPLY · PEST CONTROL · VENDING` | 快递 · 复印机维修 · 暖通维修 · 饮水服务 · 办公用品 · 除虫 · 售货机 | 快遞 · 影印機維修 · 空調維修 · 飲水服務 · 辦公用品 · 除蟲 · 販賣機 | 宅配便 · コピー機保守 · 空調保守 · 給水サービス · 事務用品 · 害虫駆除 · 自販機 | 택배 · 복사기 수리 · 냉난방 수리 · 정수기 · 사무용품 · 방역 · 자판기 | KURIER · KOPIERER-SERVICE · KLIMA-SERVICE · WASSERSERVICE · BÜROBEDARF · SCHÄDLINGSBEK. · AUTOMATEN | COURSIER · SAV COPIEUR · SAV CLIM · FONTAINES À EAU · FOURNITURES · DÉSINSECTISATION · DISTRIBUTEURS | MENSAJERÍA · SERV. COPIADORA · SERV. CLIMA · SERV. AGUA · PAPELERÍA · CONTROL DE PLAGAS · EXPENDEDORAS | MENSAGEIRO · ASSIST. COPIADORA · MANUT. AR-COND. · SERV. ÁGUA · MAT. ESCRITÓRIO · DEDETIZAÇÃO · MÁQ. DE VENDA | КУРЬЕР · РЕМОНТ КОПИРОВ · ВЕНТИЛЯЦИЯ · ДОСТАВКА ВОДЫ · КАНЦТОВАРЫ · ДЕЗИНСЕКЦИЯ · АВТОМАТЫ |
| Places `MAILROOM · RM 214 · BLDG SVCS · FRONT OFFICE · STOCK RM 3 · RM 118` | 收发室 · 214 室 · 楼宇服务 · 行政办公室 · 3 号库房 · 118 室 | 收發室 · 214 室 · 大樓服務 · 行政辦公室 · 3 號庫房 · 118 室 | 郵便室 · 214号室 · 設備管理 · 総務 · 第3倉庫 · 118号室 | 우편실 · 214호 · 시설관리 · 총무실 · 3번 창고 · 118호 | POSTSTELLE · RAUM 214 · HAUSTECHNIK · SEKRETARIAT · LAGER 3 · RAUM 118 | COURRIER · SALLE 214 · SERV. GÉNÉRAUX · ADMINISTRATION · RÉSERVE 3 · SALLE 118 | CORRESPONDENCIA · SALA 214 · SERV. EDIFICIO · OFICINA PRINCIPAL · ALMACÉN 3 · SALA 118 | CORRESPONDÊNCIA · SALA 214 · SERV. PREDIAIS · ADMINISTRAÇÃO · ALMOX. 3 · SALA 118 | ЭКСПЕДИЦИЯ · КОМН. 214 · ХОЗЧАСТЬ · ПРИЁМНАЯ · СКЛАД 3 · КОМН. 118 |
| Log `NIGHT DESK` | 夜班值班台 | 夜班值班台 | 夜間受付 | 야간 데스크 | NACHTDIENST | POSTE DE NUIT | GUARDIA NOCTURNA | PLANTÃO NOTURNO | НОЧНОЙ ПОСТ |
| `ON DUTY D.K.` | 上岗 D.K. | 上崗 D.K. | 勤務開始 D.K. | 근무 시작 D.K. | DIENSTBEGINN D.K. | PRISE DE SERVICE D.K. | EN SERVICIO D.K. | EM SERVIÇO D.K. | ЗАСТУПИЛ D.K. |
| `TOUR 1 COMPLETE  ALL SECURE` | 第 1 次巡查完毕  一切正常 | 第 1 次巡邏完畢  一切正常 | 巡回1 完了  異常なし | 1차 순찰 완료  이상 없음 | RUNDGANG 1 BEENDET  ALLES IN ORDNUNG | RONDE 1 TERMINÉE  RAS | RONDA 1 TERMINADA  TODO EN ORDEN | RONDA 1 CONCLUÍDA  TUDO EM ORDEM | ОБХОД 1 ЗАВЕРШЁН  ВСЁ В ПОРЯДКЕ |
| `BLDG SVCS CALLED  LAMPS OUT STOCK RM 3` | 已呼叫楼宇服务  3 号库房灯灭 | 已通知大樓服務  3 號庫房燈熄 | 設備管理に連絡  第3倉庫 消灯 | 시설관리 호출  3번 창고 소등 | HAUSTECHNIK GERUFEN  LICHT AUS LAGER 3 | SERV. GÉNÉRAUX APPELÉS  LAMPES HS RÉSERVE 3 | SERV. EDIFICIO AVISADO  SIN LUZ ALMACÉN 3 | SERV. PREDIAIS ACIONADOS  SEM LUZ ALMOX. 3 | ВЫЗВАНА ХОЗЧАСТЬ  НЕТ СВЕТА СКЛАД 3 |
| `TOUR 3` | 第 3 次巡查 | 第 3 次巡邏 | 巡回3 | 3차 순찰 | RUNDGANG 3 | RONDE 3 | RONDA 3 | RONDA 3 | ОБХОД 3 |
| `KEY CONTROL` | 钥匙管理 | 鑰匙管理 | 鍵管理 | 열쇠 관리 | SCHLÜSSELAUSGABE | CONTRÔLE DES CLÉS | CONTROL DE LLAVES | CONTROLE DE CHAVES | УЧЁТ КЛЮЧЕЙ |
| `ALL KEYS MUST BE SIGNED OUT` | 领用钥匙必须登记 | 領用鑰匙必須登記 | 鍵の持出しは必ず記入 | 모든 열쇠는 반출 기록 필수 | ALLE SCHLÜSSEL AUSTRAGEN | TOUTE CLÉ SORTIE DOIT ÊTRE SIGNÉE | FIRMAR LA SALIDA DE TODA LLAVE | TODA CHAVE RETIRADA DEVE SER ASSINADA | ВЫДАЧА КЛЮЧЕЙ ТОЛЬКО ПОД РОСПИСЬ |
| Key heads `DATE · KEY NO. · NAME · OUT · IN · INIT.` | 日期 · 钥匙号 · 姓名 · 借出 · 归还 · 签名 | 日期 · 鑰匙號 · 姓名 · 借出 · 歸還 · 簽名 | 日付 · 鍵番号 · 氏名 · 持出 · 返却 · 印 | 날짜 · 열쇠 번호 · 성명 · 반출 · 반납 · 서명 | DATUM · SCHL.-NR. · NAME · AUS · ZURÜCK · KÜRZEL | DATE · N° CLÉ · NOM · SORTIE · RETOUR · VISA | FECHA · N.º LLAVE · NOMBRE · SALIDA · ENTRADA · INIC. | DATA · Nº CHAVE · NOME · SAÍDA · ENTRADA · RUBR. | ДАТА · № КЛЮЧА · ФАМИЛИЯ · ВЫДАН · СДАН · ПОДП. |
| `PHONE LIST` | 电话表 | 電話表 | 内線表 | 전화번호 | TELEFONLISTE | LISTE TÉLÉPHONIQUE | TELÉFONOS | TELEFONES | ТЕЛЕФОНЫ |
| `FIRE · POLICE` | 火警 · 警察 | 火警 · 警察 | 消防 · 警察 | 소방 · 경찰 | FEUERWEHR · POLIZEI | POMPIERS · POLICE | BOMBEROS · POLICÍA | BOMBEIROS · POLÍCIA | ПОЖАРНЫЕ · ПОЛИЦИЯ |
| `BOILER RM` | 锅炉房 | 鍋爐房 | ボイラー室 | 보일러실 | HEIZUNGSRAUM | CHAUFFERIE | SALA DE CALDERAS | CASA DE CALDEIRAS | КОТЕЛЬНАЯ |
| `CONTROL RM` (the locked central control station of `40` §1) | 消防控制室 | 消防控制室 | 防災センター | 방재실 | BRANDMELDEZENTRALE | PC SÉCURITÉ | CENTRAL DE CONTROL | CENTRAL DE CONTROLE | ЦЕНТРАЛЬНЫЙ ПОСТ |
| `POST ORDERS` | 岗位守则 | 崗位守則 | 勤務規程 | 근무 수칙 | DIENSTANWEISUNG | CONSIGNES DE POSTE | ÓRDENES DE PUESTO | NORMAS DO POSTO | ИНСТРУКЦИЯ ПОСТА |

### 5.4 The TV (W19; `screens/30_screen_strings.md`, 广告 sets and re-renders)

The voice stays English with translated `[TV]` captions (Q3). `FRONT ROOMS`, `WTCZ-TV 61`, `FAIRVIEW`, `TRI-COUNTY`, `RT. 41 AT MAIN ST.`, `555-0147` and the prices stay. 广告 checks each line against its CG slot; a CJK character counts as two.

| English (slot) | zh-Hans | zh-Hant | ja | ko | de | fr | es | pt-BR | ru |
|---|---|---|---|---|---|---|---|---|---|
| `BACK ROOM CLEARANCE` (CG 24) | 后仓清仓特卖 | 後倉清倉特賣 | 倉庫一掃セール | 창고 정리 세일 | LAGERRÄUMUNG | LIQUIDATION DU STOCK | LIQUIDACIÓN DE BODEGA | QUEIMA DE ESTOQUE | РАСПРОДАЖА СО СКЛАДА |
| `3-PC SOFA SET` (CG 36) | 三件套沙发 | 三件式沙發 | 3点ソファセット | 소파 3종 세트 | 3-TLG. SOFAGARNITUR | SALON 3 PIÈCES | SALA 3 PIEZAS | CONJUNTO 3 PEÇAS | ГАРНИТУР 3 ПРЕДМ. |
| `NOW $499  WAS $899` (CG 24) | 现价 $499  原价 $899 | 現價 $499  原價 $899 | 特価 $499  通常 $899 | 할인가 $499  정가 $899 | JETZT $499  STATT $899 | $499 AU LIEU DE $899 | AHORA $499  ANTES $899 | POR $499  ERA $899 | ТЕПЕРЬ $499  БЫЛО $899 |
| `90 DAYS SAME AS CASH` (CG 24) | 90 天免息分期 | 90 天免息分期 | 90日間 金利ゼロ | 90일 무이자 | 90 TAGE ZINSFREI | 90 JOURS SANS FRAIS | 90 DÍAS SIN INTERESES | 90 DIAS SEM JUROS | 90 ДНЕЙ БЕЗ ПРОЦЕНТОВ |
| `CALL 555-0147` (CG 36) | 请致电 555-0147 | 請致電 555-0147 | お電話は 555-0147 | 전화 555-0147 | ANRUFEN: 555-0147 | APPELEZ LE 555-0147 | LLAME AL 555-0147 | LIGUE 555-0147 | ЗВОНИТЕ 555-0147 |
| `OPEN 9 TO 9  7 DAYS` (CG 24); dealer tag `OPEN 9 TO 9` | 每天早 9 点至晚 9 点; 早 9 点至晚 9 点 | 每天早 9 點至晚 9 點; 早 9 點至晚 9 點 | 毎日 朝9時〜夜9時; 朝9時〜夜9時 | 매일 오전 9시~오후 9시; 오전 9시~오후 9시 | TÄGLICH 9–21 UHR; 9–21 UHR | 7J/7 DE 9H À 21H; DE 9H À 21H | 7 DÍAS DE 9 A 9; DE 9 A 9 | TODO DIA DAS 9 ÀS 21; DAS 9 ÀS 21 | ЕЖЕДНЕВНО С 9 ДО 21; С 9 ДО 21 |
| `FREE DELIVERY` (CG 24) | 免费送货 | 免費送貨 | 配送無料 | 무료 배달 | LIEFERUNG GRATIS | LIVRAISON GRATUITE | ENVÍO GRATIS | ENTREGA GRÁTIS | БЕСПЛАТНАЯ ДОСТАВКА |
| `ALWAYS MORE` / `IN THE BACK` (CG 24, two lines) | 后面 / 总有更多 | 後面 / 總有更多 | 奥には / まだまだ | 안쪽엔 / 늘 더 있어요 | HINTEN / GIBT'S IMMER MEHR | TOUJOURS PLUS / AU FOND | SIEMPRE HAY MÁS / AL FONDO | SEMPRE TEM MAIS / LÁ NO FUNDO | В ГЛУБИНЕ / ВСЕГДА ЕСТЬ ЕЩЁ |
| `STAY TUNED.` (CG 30) | 敬请锁定。 | 敬請鎖定。 | チャンネルはそのまま。 | 채널 고정! | BLEIBEN SIE DRAN. | RESTEZ À L'ÉCOUTE. | NO SE VAYA. | NÃO SAIA DAÍ. | ОСТАВАЙТЕСЬ С НАМИ. |
| `PLEASE STAND BY` (CG 36) | 请稍候 | 請稍候 | しばらくお待ちください | 잠시 기다려 주십시오 | BITTE WARTEN | VEUILLEZ PATIENTER | UN MOMENTO | AGUARDE | ПОДОЖДИТЕ |
| `TECHNICAL DIFFICULTIES` (CG 24) | 技术故障 | 技術故障 | 技術的な問題 | 기술적 문제 | TECHNISCHE STÖRUNG | INCIDENT TECHNIQUE | DIFICULTADES TÉCNICAS | PROBLEMAS TÉCNICOS | ТЕХНИЧЕСКИЕ НЕПОЛАДКИ |
| `WTCZ 61 SIGNS OFF AT 2 AM` (CG 18) | WTCZ 61 凌晨 2 点停播 | WTCZ 61 凌晨 2 點收播 | WTCZ 61 午前2時 放送終了 | WTCZ 61 새벽 2시 방송 종료 | WTCZ 61 SENDESCHLUSS 2 UHR | WTCZ 61 FIN DES ÉMISSIONS 2 H | WTCZ 61 CIERRA A LAS 2 AM | WTCZ 61 ENCERRA ÀS 2H | WTCZ 61: КОНЕЦ ЭФИРА В 2:00 |
| Thought for today, four lines | 今日一思 / 稳步而行的人 / 比奔跑的人 / 走得更远。 | 今日一思 / 穩步而行的人 / 比奔跑的人 / 走得更遠。 | 今日のことば / 着実に歩む者は / 走る者より / 遠くへ行く。 | 오늘의 한마디 / 꾸준히 걷는 이가 / 달리는 이보다 / 더 멀리 간다. | GEDANKE DES TAGES / WER STETIG GEHT, / KOMMT WEITER ALS / WER RENNT. | PENSÉE DU JOUR / QUI MARCHE SÛREMENT / VA PLUS LOIN QUE / CELUI QUI COURT. | PENSAMIENTO DEL DÍA / QUIEN CAMINA FIRME / LLEGA MÁS LEJOS / QUE QUIEN CORRE. | PENSAMENTO DO DIA / QUEM ANDA FIRME / VAI MAIS LONGE / QUE QUEM CORRE. | МЫСЛЬ ДНЯ / ИДУЩИЙ РОВНО / УЙДЁТ ДАЛЬШЕ / БЕГУЩЕГО. |

The sign-off notice (`30` §4.1) is not on screen yet; it is translated when it ships.

### 5.5 Terminals (W18): written, 2026-11-03

- **Files:** `research/narrative/terminal_sets/<code>/{interoffice,terminal_1,terminal_2,terminal_3}.json`, nine languages, the same shape as the English sets in `Assets/Resources/Terminal/Content/`. 游戏视觉 / 交互设计 copy them into Resources once the engine reads them.
- **Source and check:** `~/FrontRoomsVisualWork/narrative_terminal/` (`terminal_strings.py`, the words; `build_terminal_langs.py`, which builds and checks). It reports **0 problems** for all nine:
  - every string fits its slot in cells (CJK and Hangul = 2, from `FrontRoomsTerminal.cs`);
  - every string stays inside its 1990 character set (GB 2312, Big5, JIS X 0208, modern Hangul, CP866, CP850);
  - the tokens are kept.
  - Re-run it after any wording change.
- **Kept as they are:**
  - `OFFICE MEMO` (the program's name), the queue `MAIN`, the printer `LASER 2`;
  - the initials, times (`2300`, `0700`, `1800`), job numbers and the dates (`{DATE_NOW}` keeps the US `MM/DD/YY`);
  - key names on row 25. The words beside them translate (`words.select` …); `softSelect` `Enter` is a key and stays.
- **World words match the signs:** the security desk (保安台, 警衛台, 警備デスク …), Stair A, the floor warden and the posted plan read as in §5 and the tables.
- **Installed (游戏视觉, LANDED 2026-11-03 18:07):** `Resources/WorldText/<code>/Terminal/Content/*.json`, byte-identical to `terminal_sets/`. Widths count cells. The drill tokens follow the language; the drill is always a Tuesday (星期二, 火曜日, 화요일). A language's terminals switch over once 平面视觉's face for it lands, and stay English until then.
- **After any wording change:** re-run the builder, then copy `terminal_sets/<code>/` to the WorldText path (CLAIM in LANDINGS), or tell 游戏视觉.
- Engine needs as asked (all four done by the landing above):
  1. glyph codes past 255, and a wide glyph spanning two cells (平面视觉, WORLD_TYPE §3b);
  2. `Wrap` counts cells, not chars. CJK body lines are written at ≤ 76 cells so they never wrap, but `Put`'s clip widths and `PutRight` must count cells too;
  3. the memo index pads the name to 18 cells, not chars (built in the files);
  4. the drill tokens in the language. CJK: `{DRILL_MONTH}` as a number and `{DRILL_WEEKDAY}` as 星期三 / 水曜日 / 수요일. de, fr, es, pt-BR, ru: month and weekday names in lower case as each language writes them (ru month in the genitive: `сентября`).

### 5.6 Magazine covers (BS4 waiting-room rack, `Kit_MagazineRack`; asked by 信息设计 2026-11-04)

**Signed English, with checks:**
- **Mastheads** are proper names. They stay English, and the dates follow `station/01` §5 (1989).
  - `FLOORPLATE`, `TRI-COUNTY BUSINESS` and `WEEKENDS`: no magazine of those exact titles turned up in a search (2026-11-04). `TRI-COUNTY BUSINESS` uses the world's market name on purpose (WTCZ's `TRI-COUNTY`).
  - `KEYSTROKE`: no magazine found, but `KEYSTROKE` was a 1983 software trademark (Brock Software Products; trademarks.justia.com). It is accepted only because its masthead is always hidden behind C1. If the rack ever shows it, rename it.
- **Every cover line is signed as drafted.**
  - `THE EMPTY FLOOR` stays: the 1989 office glut is real, and the echo is the quiet kind the building allows.
  - `Still waiting?` is the right deck.
  - None of the lines gives advice, names the threat or uses the game's words.
- **Kept in every language:** mastheads, prices, the year, `19%`, `$60`, `25`, `12`, `50`, `'85`-`'90` on the chart.
- A fallback after "/" is only for where the line runs over about 1.4 × the English.

| Slot | zh-Hans | zh-Hant | ja | ko | de | fr | es | pt-BR | ru |
|---|---|---|---|---|---|---|---|---|---|
| `NOVEMBER 1989` | 1989年11月 | 1989年11月 | 1989年11月号 | 1989년 11월호 | NOVEMBER 1989 | NOVEMBRE 1989 | NOVIEMBRE 1989 | NOVEMBRO 1989 | НОЯБРЬ 1989 |
| `DECEMBER 1989` | 1989年12月 | 1989年12月 | 1989年12月号 | 1989년 12월호 | DEZEMBER 1989 | DÉCEMBRE 1989 | DICIEMBRE 1989 | DEZEMBRO 1989 | ДЕКАБРЬ 1989 |
| `OCTOBER 1989` | 1989年10月 | 1989年10月 | 1989年10月号 | 1989년 10월호 | OKTOBER 1989 | OCTOBRE 1989 | OCTUBRE 1989 | OUTUBRO 1989 | ОКТЯБРЬ 1989 |
| `SEPTEMBER 1989` | 1989年9月 | 1989年9月 | 1989年9月号 | 1989년 9월호 | SEPTEMBER 1989 | SEPTEMBRE 1989 | SEPTIEMBRE 1989 | SETEMBRO 1989 | СЕНТЯБРЬ 1989 |
| C0 `THE EMPTY FLOOR` | 空置的楼层 | 空置的樓層 | 空室のフロア | 텅 빈 층 | DIE LEERE ETAGE | L'ÉTAGE VIDE | EL PISO VACÍO | O ANDAR VAZIO | ПУСТОЙ ЭТАЖ |
| C0 `Downtown vacancy tops 19%. Who will fill the new towers?` | 市中心空置率突破 19%。新大楼谁来填满？ | 市中心空置率突破 19%。新大樓誰來填滿？ | 都心の空室率が19%を突破。新しいビルは誰が埋めるのか？ | 도심 공실률 19% 돌파. 새 빌딩은 누가 채울까? | Leerstand in der Innenstadt über 19 %. Wer füllt die neuen Türme? | Plus de 19 % de bureaux vides en ville. Qui remplira les nouvelles tours ? | La desocupación en el centro supera el 19%. ¿Quién llenará las torres nuevas? | Vacância no centro passa de 19%. Quem vai ocupar as novas torres? | В центре пустует более 19% офисов. Кто заполнит новые башни? |
| C0 `OPEN PLAN AT 25` | 开放式办公 25 年 | 開放式辦公 25 年 | オープンプラン25年 | 개방형 사무실 25년 | 25 JAHRE GROSSRAUMBÜRO | LE BUREAU PAYSAGER A 25 ANS / PAYSAGER : 25 ANS | LA OFICINA ABIERTA, 25 AÑOS | ESCRITÓRIO ABERTO: 25 ANOS | ОТКРЫТОЙ ПЛАНИРОВКЕ 25 ЛЕТ / 25 ЛЕТ БЕЗ СТЕН |
| C0 `CARPET TILE: A BUYER'S GUIDE` | 地毯砖选购指南 | 方塊地毯選購指南 | タイルカーペット購入ガイド | 카펫 타일 구매 가이드 | TEPPICHFLIESEN: DER KAUFRATGEBER / TEPPICHFLIESEN IM TEST | DALLES DE MOQUETTE : LE GUIDE D'ACHAT / GUIDE DES DALLES | LOSETAS DE ALFOMBRA: GUÍA DE COMPRA | CARPETE EM PLACAS: GUIA DE COMPRA | КОВРОВАЯ ПЛИТКА: КАК ВЫБРАТЬ |
| C1 `THE 1990 OUTLOOK` | 1990 年展望 | 1990 年展望 | 1990年の展望 | 1990년 전망 | AUSBLICK 1990 | PERSPECTIVES 1990 | PANORAMA 1990 | PERSPECTIVAS 1990 | ПРОГНОЗ НА 1990 |
| C1 `Twelve forecasts for the year ahead` | 来年的十二项预测 | 來年的十二項預測 | 新しい年への12の予測 | 새해를 내다보는 열두 가지 전망 | Zwölf Prognosen für das neue Jahr | Douze prévisions pour l'année qui vient | Doce pronósticos para el año que viene | Doze previsões para o ano que vem | Двенадцать прогнозов на будущий год |
| C1 `TOP 50 EMPLOYERS` | 五十大雇主 | 五十大雇主 | 雇用主トップ50 | 50대 고용주 | DIE 50 GRÖSSTEN ARBEITGEBER / TOP-50-ARBEITGEBER | LES 50 PREMIERS EMPLOYEURS | LOS 50 MAYORES EMPLEADORES / TOP 50 EMPLEADORES | OS 50 MAIORES EMPREGADORES / TOP 50 EMPREGADORES | 50 КРУПНЕЙШИХ РАБОТОДАТЕЛЕЙ / ТОП-50 РАБОТОДАТЕЛЕЙ |
| C1 `RETAIL: THE MALL VS. MAIN STREET` | 零售：购物中心对决主街 | 零售：購物中心對決大街 | 小売：モール対メインストリート | 소매: 쇼핑몰 대 중심가 | HANDEL: MALL GEGEN HAUPTSTRASSE | COMMERCE : GALERIE OU CENTRE-VILLE ? | COMERCIO: EL MALL CONTRA LA CALLE MAYOR | VAREJO: SHOPPING CONTRA RUA PRINCIPAL | ТОРГОВЛЯ: МОЛЛ ПРОТИВ ГЛАВНОЙ УЛИЦЫ |
| C2 `LAKE COUNTRY` | 湖区 | 湖區 | 湖水地方 | 호수 지방 | SEENLAND | AU PAYS DES LACS | TIERRA DE LAGOS | TERRA DOS LAGOS | КРАЙ ОЗЁР |
| C2 `Twelve quiet weekends within three hours of home` | 离家三小时内的十二个安静周末 | 離家三小時內的十二個安靜週末 | 家から3時間以内、静かな週末12選 | 집에서 세 시간 안, 조용한 주말 열두 곳 | Zwölf ruhige Wochenenden, keine drei Stunden von zu Hause | Douze week-ends au calme à moins de trois heures de chez vous | Doce fines de semana tranquilos a menos de tres horas de casa | Doze fins de semana tranquilos a menos de três horas de casa | Двенадцать тихих выходных не дальше трёх часов от дома |
| C2 `INNS UNDER $60 A NIGHT` | 每晚 $60 以下的旅店 | 每晚 $60 以下的旅店 | 1泊$60以下の宿 | 1박 $60 이하 숙소 | GASTHÖFE UNTER $60 DIE NACHT | AUBERGES À MOINS DE $60 LA NUIT | POSADAS POR MENOS DE $60 | POUSADAS ATÉ $60 A DIÁRIA | ГОСТИНИЦЫ ДО $60 ЗА НОЧЬ |
| C3 `THE PAPERLESS OFFICE` | 无纸化办公室 | 無紙化辦公室 | ペーパーレスオフィス | 종이 없는 사무실 | DAS PAPIERLOSE BÜRO | LE BUREAU SANS PAPIER | LA OFICINA SIN PAPEL | O ESCRITÓRIO SEM PAPEL | БЕЗБУМАЖНЫЙ ОФИС |
| C3 `Still waiting?` | 还在等？ | 還在等？ | まだ待ちますか？ | 아직도 기다리나요? | Warten Sie noch? | Toujours en attente ? | ¿Sigue esperando? | Ainda esperando? | Всё ещё ждёте? |
| C3 `12 LAPTOPS TESTED` | 12 款膝上电脑评测 | 12 款膝上型電腦評測 | ラップトップ12機種テスト | 랩톱 12종 테스트 | 12 LAPTOPS IM TEST | 12 PORTABLES À L'ESSAI | 12 PORTÁTILES A PRUEBA | 12 LAPTOPS TESTADOS | 12 ПОРТАТИВНЫХ ПК В ТЕСТЕ |
| C3 `NETWORKING A SMALL OFFICE` | 小型办公室联网 | 小型辦公室聯網 | 小さなオフィスのネットワーク | 작은 사무실 네트워크 | NETZWERK FÜRS KLEINE BÜRO | METTRE UN PETIT BUREAU EN RÉSEAU | UNA RED PARA LA OFICINA PEQUEÑA | REDE PARA O ESCRITÓRIO PEQUENO | СЕТЬ ДЛЯ НЕБОЛЬШОГО ОФИСА |

The cards use the era's words on purpose: zh `膝上电脑` and zh-Hant `膝上型電腦` (1989), not today's `笔记本`. They are AI drafts (Red 10-31), and 简体中文 is the column Red can read.

## 6. Decisions for Red: answered 2026-11-03, 叙事设计 chat, "全部按推荐"

**Decided:** 1 = A (dubbed film); 2 = (a) EXIT translated; 3 = (a) English voice + translated captions, supers translated; 4 = per-language bitmap terminal faces, after the signs; 5 = 简体中文 first, WebGL English.


1. **Frame.** A, a dubbed film: same American building, words in your language (recommended, §1). B, a relocated building: a Chinese player plays in a Chinese office, with new codes, plans and TV. B is about nine times the research and redraws every plan. **Recommend A.**
2. **The EXIT sign.** (a) Translate it (`安全出口`, `非常口`, `AUSGANG`): the player's most important word reads in their own language, and the plan, legend and sign agree. (b) Keep `EXIT` everywhere as an icon. A breaks less art, but contradicts Red's "all diegetic". **Recommend (a).** The red colour, the box and the light stay.
3. **The TV ad's voice.** Supers and cards are translated in every case. The voice: (a) stay English with the `[TV]` captions translated (no cost), or (b) dub nine voices (casting, recording, mixing per language: 声音设计 + 广告, days per language). **Recommend (a) now, (b) later if Red wants it.** 广告 confirms whether the supers are burned into the video.
4. **Terminals.** Per-language 1990 bitmap faces (§4), which is era-true and a real type job, versus terminals staying English with the TEXT panel. Red's words cover them. **Recommend the per-language faces, after the signs.**
5. **Order.** 简体中文 first, which Red can check; the other eight follow from the same pipeline. WebGL stays English (separate track).
