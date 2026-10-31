# Language marks: options for the Settings language page

平面视觉, 2026-10-30. Asked by Red through 总控. Status: **Red picked A · TAG on 2026-10-30. Built by 设置菜单 the same day as one row (LANDED 23:14).** Red first replaced the page with a rack of tiles, then replaced the rack: "目前还是占据太多空间，目前我希望只有被选择的那个是展开的，其他就是缩减版只有图标". The tag, tokens, swing and fill stand.

**Figma:** section `3834:8767`, LANGUAGE MARKS. Each lane has the desktop page, the phone page, and the states and small sizes. Component set `3844:81`, Set / Language tag.

**List and behaviour:** `research/localization/README.md` §7 (设置菜单与画质预设). The first row of ASSIST: the word, `A文`, then a strip of tags against the row's right edge. The chosen language opens out to tag + own name; the others are tag only. Names are never translated. Focus is not current. A pick is instant. The options below were drawn for the earlier page; lane A in Figma shows the row.

## Build spec (A · TAG)

**Assets:** `language_marks/LangTag_Outline@8x.png` and `LangTag_Fill@8x.png`, 320 × 224 px, white on transparent, for a 40 × 28 unit tag. The 4× files are the first export. Tint them with `Image.color`. The outline is truly hollow, so the sheet shows through. They come from the B key's own tag vectors.

| | Desktop px | Phone pt | Tablet pt |
|---|---|---|---|
| Tag | 40 × 28 | 34 × 23.8 | 38 × 26.6 |
| Tag centre on the row's centre | 16.9 from the tag's top | 14.4 | 16.1 |
| Token, Latin and Cyrillic | Plex Mono Medium 13 | 11 | 12 |
| Token, CJK | gothic Bold 14 | 12 | 13 |
| Name | Source Serif 20 | 17 | 19 |
| Name gap, after its tag | 16 | 13.6 | 15.2 |
| Tag gap | 10, closing to 2 when tight | same | same |
| Row | 48 | 44 | 44 |

- **Not current:** the outline in paper #F4F1E8, the token in paper.
- **Current:** the fill in paper, the token in ink #141414. The name stays paper.
- **Focused:** the row's usual 4 px yellow bar. The tag does not change.
- **Token:** centred on the tag's body, not on its 28-unit frame.
- **Tokens are constants:** EN 简 РУ ES PT DE FR 日 한 PL 繁 TR УК IT. They never pass through `T()`.
- **The row:** `LANGUAGE`, then 14 units on, the row mark: the outline tag with `A文` (`A` in Plex Mono Medium, `文` in the gothic Bold, 13 / 11 / 12). The strip ends at the row's right edge.
- **Open:** the chosen language shows its name. A keyboard or hover can open another tag to read it. That tag stays outlined until Enter or a tap: filled always means current.
- **Below 100 %:** CJK tokens have no smaller size. If a future layout needs a smaller tag, drop the token and keep the name.
- **No keyline.** The sheet is a reading surface.

## Check after landing (2026-10-30, L4, frames from 设置菜单)

**Pass:** tag sizes, tokens, states and `A文` hold on desktop, iPhone and iPad. Oswald Bold labels sit well in the Portuguese frames. Tag and name are drawn at base size, so the 1.75 × limit no longer applies.

**List now:** ten languages (Red added 繁體中文). They fit one line on every screen; the gaps follow the open name, so only `Português (Brasil)` narrows them (7–8 pt), and on the iPad the names set at 17 pt. 繁 now uses Noto Sans TC and 繁體中文 Noto Serif TC: checked on the iPhone and Duo outer frames, PASS. On the iPhone the token sits 0.8 pt below the tag's centre, which is too small to see at that size.

**Closed by 设置菜单, 2026-10-30:**
1. **Portuguese header:** a button now fits its word, the tab gaps close from 18 to 10, then the heading drops.
2. **Open name:** a name gap on both sides, so it no longer reads as the next tag's.
3. **Duo outer target:** on a single-column sheet the word LANGUAGE gives its room to the tags and `A文` alone names the row. Folded tags are 44 pt apart there and on the iPhone.
4. **14 languages:** a second line of tags, written into `research/localization/README.md` §7.2 for Tier 3.

**Real faces, 2026-10-30:** Japanese, Korean and Simplified Chinese frames checked. `简`, `日` and `한` sit centred in their tags and read clearly, and CJK labels, names and meta read well at their sizes on desktop.

## What carries over from We're Car

The language's own name is always written. A mark never stands alone. Colour never carries the choice alone. The round flags and the capsule do not carry over.

## The options

| Option | Mark | Current | Feel | Main cost |
|---|---|---|---|---|
| **A · TAG (recommended)** | the B key's tag, from the same vectors, with a two-character token in the language's own script | tag fills with paper | reads as part of the HUD key family; no colour | the token needs reading, so it scans slower than a flag |
| B · FOB | the Long tag grown into a key fob, with the name on it | fob fills | a key rack; name and mark are one | the loudest page; the hole can read as a radio button |
| C · PRINT FLAG | a hard-edged flag in flat inks inside the tag; 繁體中文 takes A's token | a paper chip behind the row | the strongest at a glance | political risk, new inks, a drawing per language |
| D · FORM | a form box, a typed code and the name | a typed X | the plainest; closest to AAA | little character |

## Small sizes

- **A:** Latin and Cyrillic tokens read down to 75 %. 简 and 繁 do not: at 1× they need 14 px, so 100 % is the floor. Below it the token drops out and the name carries the row.
- **B:** the name scales with the type ramp.
- **C:** the fields stay apart at 75 % and at 50 %. China's star and Korea's bars shrink to specks at 50 %.
- **D:** text only; it follows the type ramp.

## The LANGUAGE row

- **Current state:** the mark fills (A, B), a paper chip sits behind the row (C), or an X is typed (D). All of them differ by shape or fill, not hue, and none uses yellow, which stays focus.
- **Row mark:** `A文` beside the word LANGUAGE, the same in every UI language, so a player stuck in a language they cannot read can still find the row. It is outlined; the current language's mark at the right is filled. A and C draw it in a tag, B on a small plate, D as plain type.

## Motion (A)

- **The swing:** on a pick the new tag rotates about the top of its tab, (20, 2) in tag units, by `FrontRoomsKeyHudMotion.SwingKick(t, 9f)`, the HUD key's own U·3 kick (2 Hz, decay 4/s, peak about 5.5° at 0.125 s, settled in about 1 s). The token rotates with it.
- **The fill:** the old tag's fill and the new tag's fill cross in 120 ms, each token's colour with it. Nothing is overlaid.
- **The name:** it unfolds over 0.16 s.
- **Unscaled time,** because the game is paused behind the sheet.
- **Reduce Motion** (`FrontRoomsHandheld.ReduceMotion`): no swing, and the fill snaps.
- **Repeat picks:** a new pick restarts the swing on the new tag and leaves the old one at rest.
- A pick changes the whole sheet at once, so the swing is the only movement on the page.

## Type by script

| Script | Label (Bayon's role) | Name | Token |
|---|---|---|---|
| Latin, Cyrillic | Oswald Bold 17 | Source Serif 4 | IBM Plex Mono Medium |
| 简体中文 | Noto Sans SC Bold 18 | Noto Serif SC | Noto Sans SC Bold |
| 繁體中文 | Noto Sans TC Bold 18 | Noto Serif TC | Noto Sans TC Bold |
| 日本語 | Noto Sans JP Bold 18 | Noto Serif JP | Noto Sans JP Bold |
| 한국어 | Noto Sans KR Bold 18 | Noto Serif KR | Noto Sans KR Bold |

- **Bayon's stand-in is Oswald Bold at 0.85 × the size.** At 17 px its caps are 13.8 px against Bayon 20's 13.9, and `RESOLUTION` is 2 % wider. Roboto Condensed Bold 20 is 25 % wider. Oswald is already in `Fonts/Period1990`.
- **Noto Serif CJK is the same design as Source Han Serif**, Source Serif's family. Figma has it under the Noto name.
- **Files, not sprites.** One name subset and one token subset per CJK language, each in its own region's forms, always loaded. CID CFF fonts do not merge, so the tokens cannot share one file.
- **Sources:** Noto Sans SC and Noto Serif SC are on this Mac and have no Hangul (read from their cmaps). Needed for the first 9 languages: Noto Sans JP, Noto Serif JP, Noto Sans KR and Noto Serif KR. Needed with 繁體中文: Noto Serif TC and Noto Sans TC. A SC face cannot stand in for 繁體中文 or 日本語, because each name keeps its own region's forms. Downloads need Red's OK. Real text stays sharp at every render size; a sprite does not.
- **CJK size floor:** desktop 16 px at 1080p, phone 12 pt, tablet 13 pt. Tag tokens 14 px and 12 pt. Measured on desktop at 1×: at 13 px dense characters such as 鍵 and 體 close up, 14 px is marginal, 16 px is clear. The phone and tablet values are a judgement and need a check on a device.
- The sizes for CJK labels are a first read and need checking in game.
