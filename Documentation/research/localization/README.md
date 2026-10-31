# Localization: which languages, what translates, how, and the Settings language row

Line 设置菜单与画质预设, 2026-10-30. Research and proposal only: nothing is built. Sources by number are in `SOURCES.md`. Working tools and the raw string list are outside the repo, in `~/FrontRoomsInteractionWork/settings/localization/` (`font_coverage.py`, `string_inventory.py`, `strings_raw.tsv`).

Red, in 总控 B, 2026-10-30: "目前本地化上，我需要你research正常独立游戏应该有什么语言（除了目前有的这些语言以外），然后把所有语言都放在游戏的设设置里，然后你可以参考were car这边做的国旗多语言图标，但是我需要取其精华，具体的平面设计图形语言应该符合我们这个项目".

## 0. Answer in short

- **FrontRooms has no localization today.** No language setting, no string table, no Unity Localization package. Every string is hard-coded English. The "existing languages" are We're Car's six (English, Simplified Chinese, Japanese, Korean, Spanish, French), not ours. A language row does nothing until strings and fonts exist.
- **A normal indie game ships about 10 languages.** The median of 12 comparable horror titles on Steam is 10. Ten of the 12 are localized; two big hits (Lethal Company, Iron Lung) are English only.
- **Recommended target: 9 languages,** which cover 91.5 % of Steam's language share: English, 简体中文, Русский, Español, Português (Brasil), Deutsch, Français, 日本語, 한국어. Five more can follow on evidence: Polski, 繁體中文, Türkçe, Українська, Italiano (96.6 %).
- **The game has very little text:** about 1,500 words of interface and rules, about 2,400 with the office terminals. One language costs €146–340 at a game-translation vendor's list rates. Text is the cheap part.
- **The real work is fonts and code.** Bayon has no accented letters at all, so even French and Spanish labels cannot be set in it. Some 15 string sources across six owners must go through one table.
- **The world stays English.** It is an American office in 1990. What the player must read to play is translated in a reading layer (HUD, menus, captions, the TEXT panel), not in the print on the walls.
- **System:** a small in-house table (English string as key, one file per language) with a font map per language, not the Unity Localization package.
- **The row is ready to specify, not to ship.** It should wait for Red's picks in §9.

## 1. What a normal indie game ships

### 1.1 Who is on Steam (September 2026 survey [1])

| Language | Share | Cumulative in the order below |
|---|---|---|
| English | 34.20 % | 34.20 % |
| Simplified Chinese | 31.39 % | 65.59 % |
| Russian | 9.05 % | 74.64 % |
| Spanish (Spain 4.08 + Latin America 0.75) | 4.83 % | 79.47 % |
| Portuguese-Brazil | 3.70 % | 83.17 % |
| German | 2.48 % | 85.65 % |
| Japanese | 2.41 % | 88.06 % |
| French | 2.08 % | 90.14 % |
| Korean | 1.37 % | 91.51 % |
| Polish | 1.49 % | 93.00 % |
| Traditional Chinese | 1.27 % | 94.27 % |
| Turkish | 1.09 % | 95.36 % |
| Ukrainian | 0.66 % | 96.02 % |
| Italian | 0.56 % | 96.58 % |
| Thai | 0.82 % | not recommended, §2 |
| Czech 0.50, Hungarian 0.32, Portuguese-Portugal 0.31, Vietnamese 0.25, Dutch 0.23, Swedish 0.23, others ≤ 0.19 | | |

Read this with care. The Simplified Chinese figure moved +7.42 points in one month, so the Chinese sample swings. Share of players is not share of revenue; I found no public per-language revenue data. Valve's own guidance is only that "Steam favors products that are localized" and that the store page and the game can be localized independently [2].

### 1.2 What comparable games actually list (Steam store data, read 2026-10-30 [5–16])

| Game | Languages | Which |
|---|---|---|
| Phasmophobia | 28 | nearly everything |
| R.E.P.O. | 21+ | broad, incl. Arabic and Thai |
| Escape the Backrooms | 12 | EN FR DE ES RU 简 JA KO TR PL PT-BR 繁 |
| Inscryption | 12 | EN FR DE ES JA KO RU 简 IT PT-BR 繁 TR |
| DREDGE | 12 | EN FR IT DE ES JA KO RU 简 繁 PT-BR PL |
| The Exit 8 | 10 | EN JA FR IT DE ES PT-BR 简 繁 KO |
| The Mortuary Assistant | 10 | EN FR PT-BR IT DE JA KO 简 TR ES-LatAm |
| Poppy Playtime | 9 | EN FR IT DE PT-BR RU ES-LatAm JA KO |
| SIGNALIS | 8 | EN DE JA KO RU 简 ES-LatAm FR |
| Amnesia: The Bunker | 8 | EN FR IT DE ES PT-BR RU 简 |
| Lethal Company | 1 | EN |
| Iron Lung | 1 | EN |

Median 10. Among the 10 localized titles, how many ship each language: French 10, German 10, Spanish 10 (Spain 7, Latin America 5), Simplified Chinese 9, Japanese 9, Korean 9, Portuguese-Brazil 9, Russian 8, Italian 8, Traditional Chinese 6, Turkish 5, Polish 4, Ukrainian 2. These are today's lists; many were added after launch.

Beyond We're Car's six, the ones a normal indie adds are **Russian, German, Portuguese-Brazil**, then Italian, Traditional Chinese, Turkish, Polish.

### 1.3 Small languages do not always pay

One published case, Academia: School Simulator, tracked revenue six months after adding languages, against a −17 % baseline: Thai +331 %, Turkish +8 %, Polish +8 %, Brazilian Portuguese −6 %, Latin American Spanish −13 %, Russian −20 %, Italian −25 %, Indonesian −34 %. Only Thai clearly paid back within six months [4]. It is one game with stated caveats (see the source), but it argues for adding the long tail on evidence, not in advance.

### 1.4 Platforms

- **Steam:** 31 languages with full client support and about 70 more that a game may list. Each language has three separate checkboxes: Interface, Full Audio, Subtitles [2][3]. FrontRooms has no voiced dialogue, so Interface + Subtitles.
- **App Store:** 50 metadata languages and locales [17]. The list shown on a product page, and iOS's per-app language setting, need localizations declared in the app bundle; check this in the first iOS build (not verified yet).
- **Simplified Chinese does not mean the China App Store.** Apple has required a government approval number for games in mainland China since 2020; about 8,000 games were pulled that year [18]. I found no newer rule, so check before relying on this. Steam's global store reaches Chinese players without it. Simplified Chinese on iOS serves Chinese readers on other storefronts.

## 2. Recommended languages, in tiers

Cost uses one vendor's published list rates from English, in EUR per word, repetitions free [19], times 2,430 words (§3). Treat them as list prices for a real quote. Another published estimate for about 2,500 words is far lower, $80–150 per language plus 15–20 % for LQA [20]. Allcorrect's LQA is €27–46 per hour [19]. The Steam store text adds 356 words per language.

| Tier | Languages | Steam share added | New fonts | Text cost (one vendor's list) |
|---|---|---|---|---|
| Source | English | 34.20 % | none | none |
| 1 | 简体中文 | 31.39 % → 65.59 % | Chinese display + text | €219–292 |
| 2 | Русский, Español, Português (Brasil), Deutsch, Français, 日本語, 한국어 | 25.92 % → 91.51 % | Latin-Cyrillic display face; Japanese and Korean display + text | €1,554–2,260 |
| 3 | Polski, 繁體中文, Türkçe, Українська, Italiano | 5.07 % → 96.58 % | Traditional Chinese set | about €1,069–1,530 (Ukrainian has no list rate; Russian's used) |

**Not recommended:** Thai and Arabic. Unity's legacy `Text` does not shape these scripts, so they need a text engine change. Arabic is 0.00 % on Steam and right-to-left. Latin-script extras (Czech, Hungarian, Dutch, Swedish…) are possible later with no new fonts.

**One Spanish.** Short interface text can be written in neutral Spanish and listed once as Español; on Steam it can then tick both Spain and Latin America. Review that claim with whoever checks the Spanish. English needs no regional split.

## 3. What translates and what stays English

The principle: **the world stays English; the layer the player reads to play is translated.** The building is American, 1990 (era lock, COMMON rule 11). Translating a wall sign breaks the place; not explaining a rule breaks the game. The TEXT panel in close-ups (T6/T7, the transcript of the evacuation plan) is already the bridge: the object stays as printed, and its words appear in plain type.

Words measured 2026-10-30 from main (unique strings per source, sorted by hand from `strings_raw.tsv`).

| Surface | Where | Owner | Words | Proposal |
|---|---|---|---|---|
| Settings rows + explanations | `FrontRoomsSettingsMenu.cs` | this line | 679 | translate |
| Desktop key map page | `Input/FrontRoomsKeyMapView.cs` | 交互设计 | 294 | translate; key caps stay |
| HUD, prompts, pause, caught, title | `FrontRooms3DGame.cs` (关卡设计); `.Mobile.cs`, `Input/FrontRoomsTouchControls*.cs` (交互设计); `Rendering/UI/FrontRoomsKeyHud.cs` (游戏视觉); `FrontRoomsDesktopSettingsView.cs` (this line) | four owners | about 300 | translate |
| Captions | `Audio/*`, `FrontRoomsCaptions.cs` | 声音设计 | about 33 | translate |
| Close-up prompts + evacuation plan transcript | `Rendering/Inspect/FrontRoomsInspectCatalog.cs` | 游戏视觉, strings 叙事设计 | 200 | translate (in the TEXT panel) |
| **Subtotal: interface and rules** | | | **about 1,500** | |
| Office terminals (3 sets + OFFICE MEMO) | `Resources/Terminal/Content/*.json` | 叙事设计 | 921 | CRT stays English; translation in the TEXT panel |
| **Subtotal with terminals** | | | **about 2,430** | |
| Printed sheets, placard art, door signs, EXIT, stair lettering, wallpaper | baked textures and glyph atlases | 平面视觉, 游戏视觉 | n/a | stay English |
| TV ad | video + audio | 广告 | n/a | stays English; the `[TV]` captions translate |
| The FRONTROOMSSS logo | | 广告 | n/a | never translated |
| Store pages | `Steam_Publishing/STORE_PAGE_DRAFT.md`; App Store metadata | App Store · 构建 | 356 + App Store text | translate per language; can go first |

**Why the terminals stay English on the CRT:** the C20 8 × 16 face and the terminal JSON are ASCII by design, and each cell is a fixed glyph. A CJK or Cyrillic terminal would be a new face and a new layout.

**Terminals on phones (游戏视觉, 2026-10-30):** the translation path is `FrontRoomsTerminal.PanelTranscript` → `FrontRoomsInspectCatalog.SetTranscript(TerminalTranscript, lines)`, with the terminal JSON strings as table keys, all four `terminal_<key>.json` sets. That reaches the TEXT panel on desktop and iPad. Phones have no TEXT chip on a terminal close-up (Red 2026-10-30: phone close-up full bleed, no transcript; 交互设计's rule "iPads keep TEXT; phones don't"), so a phone player in another language would read nothing. The proposal is to show the TEXT chip on phones only when the game is not in English, which leaves the English game as Red asked. It bends Red's rule, so it is Red's call, with 交互设计; it waits for step 4.

**Strings that cannot simply be swapped (code work):**
- `FrontRoomsSettingsMenu.TextWidth` is a hard-coded Bayon A–Z width table, and everything else counts as 0.45 em; IBM Plex Mono is 0.6 em a character. Both are wrong for any other script. The segment and control geometry reads them.
- `SentenceCase()` (caught card, `.Mobile.cs`) changes English letter case by rule; it needs table entries.
- Composed strings: `FloorLabel + " / FRONT ROOMS"`, `"LEVEL B" + n`, HUD stats, the `E  ·  VERB` prompts. They need `{0}` patterns.
- Plurals: `KEY/KEYS`, `ZONE/ZONES`, `DOOR BROKEN/DOORS BROKEN` are two branches. Russian and Polish have more forms. Prefer the label style `KEYS  2`, which has no plural.
- Key names in prompts and the title legend stay as printed on the keyboard.

## 4. Fonts

Coverage read from each font's cmap on 2026-10-30.

| Face | Role | Accented Latin (FR DE ES PT PL TR) | Cyrillic | Greek | CJK, Kana, Hangul |
|---|---|---|---|---|---|
| **Bayon** (203 code points) | every label and prompt | **none** | none | none | none |
| IBM Plex Mono | meta, values | all | yes | no | none |
| Source Serif 4 | captions, explanations | all | yes | yes | none |
| Oswald Bold (in `Fonts/Period1990`, 88 KB) | candidate | all | yes | not checked | none |
| Roboto Condensed Bold (in `Fonts/Period1990`, 92 KB) | candidate | all | yes | not checked | none |
| Anton (in `Fonts/Period1990`) | candidate | all | **no** | not checked | none |
| C20 8 × 16 terminal face | CRT | ASCII only | no | no | none |

What follows:
- **Bayon cannot be the label face in any language but English.** Glyph fallback would set "É" in another face in the middle of a word. So the font must change per language, not per missing glyph: a **role map** (label, text, mono) for each language.
- **Latin and Cyrillic languages:** Source Serif 4 and IBM Plex Mono already work. They need one condensed caps face to stand in for Bayon. Oswald Bold and Roboto Condensed Bold cover everything and are already in the project. The pick is 平面视觉's. Neither has `←` `→`: those stay in Plex Mono.
- **Chinese, Japanese, Korean (平面视觉, `research/desktop_ui/LANGUAGE_MARKS.md`, type by script):** labels in Noto Sans SC / JP / KR Bold, text in Noto Serif SC / JP / KR (the same design as Source Han Serif, the family of Source Serif, SIL OFL) [21], meta in the regular gothic. CJK has no capitals, so a label is set in Bold at 18 for Bayon 20. **Size floors:** desktop 16 px at 1080p, phone 12 pt, tablet 13 pt, so today's 13 px / 11 pt meta becomes 16 px / 12 pt in CJK; the width check budgets that. Desktop was measured (13 px closes up dense characters such as 鍵 and 體, 16 px is clear); phone and tablet are a judgement and need a device check.
- **Size.** All game text is authored, so each CJK font can be subset to the characters we use: on the order of 1,000 characters per language, an estimate of under 1 MB per face (measure on the first subset). A full CJK font is far larger. Subsetting needs a tool; none is installed here, so that download needs Red's OK.
- **Legacy `Text` and dynamic fonts.** The HUD and sheets use legacy `Text` (11 script files use UnityEngine.UI; no TextMeshPro). Dynamic fonts with a per-language swap fit that. The logo uses UI Toolkit and does not change.
- **Endonyms in the row** need a very small always-present set of glyphs (§6).

## 5. Text expansion on the phone

German and Russian run longer than English, and Bayon is unusually narrow, so its replacement widens every label on top of that. CJK runs shorter. Slots at risk, with today's sizes:

- **USE disc:** Ø72 pt with the verb inside. English peaks at EXAMINE. It needs a smaller size or an agreed short verb per language.
- **QUALITY row:** BATTERY · BALANCED · QUALITY · ULTRA already uses compact padding to fit a 418 pt list.
- **Header line:** four tabs share the line with RESUME and END RUN on a 678 pt outer display, with camera give-way.
- **Caption card:** 214 pt wide, 10 + 20 × lines tall, and only 1–2 lines fit above the flash line.
- **Prompt and title legend:** `HOLD USE · BREAK GLASS`, `PAST THE RING · RUN`.

Each table row should carry a width budget. The touch harness should check every language for overflow, starting with a pseudo-language at +35 % so problems show before any translation.

## 6. System proposal

| | In-house table (recommended) | Unity Localization 1.5 |
|---|---|---|
| Fits our UI | Text is built in code with legacy `Text`: `Text.text = T("OPEN")` is a one-word change at each call | Its strength is components on scene objects; ours are mostly built in code |
| Dependency | none new | String tables live in Addressable groups [22]; the project uses `Resources` and has no Addressables |
| Risk to desktop | English returns the same string, so it stays byte-identical | new package and build step on every platform |
| Translator tools | one TSV per language | XLIFF, CSV, Google Sheets, pseudo-localization, Smart Strings [23] |
| Size | a few KB of code; tables about 100–150 KB for 9 languages (estimate) | adds package + Addressables to iOS and WebGL builds; not measured |

**Shape of the in-house table:**
- `FrontRoomsLanguage.T("OPEN")`: the English string is the key, so missing entries show English instead of nothing, and owners keep readable code.
- `Resources/Language/<code>.tsv`, LF: key, translation, context note, width budget.
- `FrontRoomsLanguage.Current`, saved in PlayerPrefs. First launch reads `Application.systemLanguage`; no match means English, with no prompt.
- A `Changed` event, like `FrontRoomsSettings.Changed`, so sheets and HUD update live.
- A font role map per language, and real text measurement in place of `TextWidth`.
- One owner per file still wraps its own strings (COMMON rule 3).

**Platforms:**
- **Mac and Windows:** same code. RT and rendering are untouched. English output is unchanged.
- **iOS:** a post-build step beside `FrontRoomsLaunchScreen.cs` declares the languages.
- **WebGL:** separate track (`webgl-changes-webgl-only`). It has no OS fonts, so every font must ship. Stay English until Red asks.

## 7. The Settings language row

**Red's words: every language in Settings. Each in its own name.**

### 7.1 The list (draft until Red's D1; this table is the source)

Each name is exactly as it displays, in this order, in every UI language. The order follows §2 and never changes.

| # | Own name | Code | Tier |
|---|---|---|---|
| 1 | English | en | source |
| 2 | 简体中文 | zh-Hans | 1 |
| 3 | Русский | ru | 2 |
| 4 | Español | es | 2 |
| 5 | Português (Brasil) | pt-BR | 2 |
| 6 | Deutsch | de | 2 |
| 7 | Français | fr | 2 |
| 8 | 日本語 | ja | 2 |
| 9 | 한국어 | ko | 2 |
| 10 | Polski | pl | 3 |
| 11 | 繁體中文 | zh-Hant | 3 |
| 12 | Türkçe | tr | 3 |
| 13 | Українська | uk | 3 |
| 14 | Italiano | it | 3 |

- **Simplified and Traditional Chinese** are separate entries.
- **One Español,** neutral, with no region in its name. `Português (Brasil)` keeps its region because the text is Brazilian.
- **Nothing right-to-left.** Arabic, Thai and Vietnamese are not in the list (§2).
- **Design for 14, by a rule.** Red may pick fewer, and a language can join later.
- **Mixed case, as each language writes itself.** Never forced to capitals: CJK has none, and a sheet of Bayon caps does not apply here.

### 7.2 Row and page behaviour

- **The row:** first row of ASSIST. `LANGUAGE` on the left, the current language in its own name on the right. Tap, click or Enter opens the page. Its mark must not depend on the current language, so a player stuck in a language they cannot read can still find it: last tab, first row, and the row mark `A文`, an outlined tag that stays the same in every UI language (Red picked A · TAG, 2026-10-30; `research/desktop_ui/LANGUAGE_MARKS.md` has the build spec).
- **A page, not IA §1's LIST in place.** It is a second-level page of ASSIST, built like the desktop's `CONTROLS / KEYBOARD & MOUSE` (`Kind.Page`): heading `ASSIST / LANGUAGE`, BACK returns to ASSIST. Touch does not have `Kind.Page` yet, so it must be added. The rows inside a tab cannot hold 14 names at any phone size.
- **A rack of tiles, not a table (Red, 2026-10-30, on the first frames: the phone's space was not used well, "这个也是" for the desktop).** The first version was a list, one or two columns of 44 pt rows: on the phone half of every column was empty, and on the desktop a name sat at the left of a rule 1776 px long. The page is a grid now, the same rule on every screen:
  - As many columns as the longest name allows, up to four; then the fewest lines; then no empty last column.
  - The tiles share the whole width and the whole height. The tag and the name grow with the tile, up to 1.75 ×.
  - Every tag hangs by its ring from the rail above it, like a key on a board; the name sits under the tag. The rail is where the swing starts from.
  - No scrolling, ever: every name is on the page at once, and the arrows move in two dimensions.

| Screen | Rack area | 9 names | 14 names |
|---|---|---|---|
| iPhone 874 × 402 | 710 × 311 pt | 3 × 3 | 4 × 4 |
| Duo outer 678 × 466 | 630 × 362 pt | 3 × 3 | 3 × 5 |
| Duo inner 951 × 669 | 919 × 565 pt | 3 × 3 | 4 × 4 |
| iPad 11" 1180 × 820 | 1148 × 730 pt | 3 × 3 | 4 × 4 |
| Desktop 1920 × 1080 | 1776 × 664 px | 3 × 3 | 4 × 4 |

Rack columns sit on the desktop's 12-column grid (4 or 3 grid columns each). The touch sheet takes the card's whole width, and where the card stops short of the safe area (iPad, Duo inner; 平面视觉's check, 2026-10-30) it runs out to it, SETTINGS and BACK easing out with it (`FrontRoomsTouchLayout.SettingsPageOverhang`). The base sizes of tag, token and name stay `LANGUAGE_MARKS.md`'s; a tile is never smaller than a row's 44 pt target.

- **No AUTO or SYSTEM entry.** While the player has never picked, the game follows the device language at each launch; no match means English. The first pick is saved and stays. An AUTO row would have no own-language name, and it is not needed to get back.
- **A pick is instant, with no KEEP / REVERT.** The page stays open and the sheet changes around it. A wrong pick is undone with one more tap, because the list never changes. KEEP / REVERT is only for desktop display changes.
- **Focus is not current.** The yellow bar means focus, as on every row: keyboard or hover on desktop, and it moves with ↑ ↓ ← →. Enter, Space, a click or a tap applies. Moving focus changes nothing. The current language has its own state beyond colour [24]: its tag fills (the outline becomes a solid tag in paper, the token turns ink). Never yellow, which stays focus.

### 7.3 Own names never change

Every name is always in its own language and own script, whatever the UI language (平面视觉's proposal, confirmed). The system can do this: the names are constants in the language list and never pass through `T()`, so they are not in any table. Each row is its own `Text`, so each can carry its own font.

- **Latin and Cyrillic:** Source Serif 4 is always loaded and sets all ten (measured). Oswald Bold and Roboto Condensed Bold also do, if a condensed look is wanted. Bayon cannot set Español, Português, Français, Türkçe or the Cyrillic names.
- **CJK:** 12 distinct characters: 简 体 中 文 繁 體 日 本 語 한 국 어. Each name must use its own region's glyph forms, so one shared subset is not enough. **Files, not sprites (平面视觉):** four tiny subsets for the names, plus one gothic subset for the tokens 简 繁 日 한 and the mark's 文. Five small files, always loaded in every language; real text stays sharp at every render size.
- **Not translated yet:** see D5 below.

**The mark is 平面视觉's** (told by 总控). What We're Car's research adds [25]: AAA games write the language name; flags appear mostly in indie, mobile and early PAL titles, and then beside the name. Standards advise against flags for languages [26][27]. Flags carry real risk: English has no agreed flag, Spanish and Portuguese span continents, Traditional Chinese is political, and Russian and Ukrainian sit next to each other in this list. The essence worth keeping from We're Car: recognising your language at a glance, the name always present, and one motion when it changes. FrontRooms' version should grow from its own language (keylines, tags, the key tag's typed insert), not from national colours.

## 8. Order of work

Small packages, each landed on its own (rule 23). Status 2026-10-30 22:04:

- **Done:** step 1, the settings part of step 2, and step 6. `FrontRoomsLanguage` (`T`, `F`, `Style`, `Measure`), real text measurement, settings and captions through the tables, all 8 tables at 259 rows (AI drafts), Oswald Bold for ru es pt-BR de fr, the LANGUAGE row and page (`FrontRoomsLanguagePage`). `FrontRoomsLanguageTests` lists any settings row wider than the 418 pt phone list: none, CJK by estimate until the fonts exist. Tables are `.txt` (Unity's TextAsset), not `.tsv`.
- **With owners:** step 4. 交互设计's wrap is in main and in the tables (Mobile, touch view, key map). Still to wrap: 关卡设计 (Game.cs, including the zone-name split: keys like `Level B3 / The maze` have no end), 游戏视觉 (KeyHud, Inspect). A string reaching `T` through a variable is listed in `Tools/localization/passed_keys.txt`.
- **With Red:** the CJK font downloads (step 3): Noto Sans and Serif JP and KR, as Noto Sans SC and Noto Serif SC are on this Mac; fonttools for subsetting. They hold no Hangul and only Simplified forms of 繁體 and 語 (平面视觉 read their cmaps), so Noto Serif TC joins this list if 繁體中文 joins the page.
- **Not started:** steps 5 and 7. Public builds show nothing yet: only English is `Finished`, and the row hides while English is alone. Editor and development builds show the page, unfinished languages dimmed.

1. `FrontRoomsLanguage` + English table + real text measurement + settings strings (this line's files). No visible change.
2. Pseudo-language + harness width check, to find every slot.
3. Font role map, after 平面视觉's picks; font downloads need Red's OK.
4. Owners wrap strings: 关卡设计 (Game.cs), 交互设计 (Mobile, touch, key map), 游戏视觉 (Inspect, KeyHud), 声音设计 (captions). TEXT panel for terminals: 游戏视觉 + 叙事设计.
5. 简体中文 first: table, Red reviews, full pass on the Duo and desktop.
6. LANGUAGE row + page.
7. The other Tier 2 tables; store pages; Steam and App Store listings.

## 9. Decisions for Red

**DECIDED 2026-10-30: Red, "全部按推荐". All five as recommended below; the 9 languages are the list's first 9 rows.**

| | Question | Recommendation | Blocks |
|---|---|---|---|
| D1 | Which languages | 9 (Tier 1 + 2); Tier 3 on evidence after release | tables, fonts |
| D2 | Does the world stay English | Yes: reading layer only | scope of all text work |
| D3 | System | In-house table | step 1 |
| D4 | Who translates | AI draft + Red reviews 简体中文 + paid native check of the rest before a public release | tables |
| D5 | Languages not finished yet | Public builds list only finished languages; test builds show the rest dimmed | the page |

**AI translation:** Steam's disclosure form covers AI-generated content players consume, and press coverage says translation shipped to players is included [28]. I could not open Valve's own text, so read the form when filling it in.
