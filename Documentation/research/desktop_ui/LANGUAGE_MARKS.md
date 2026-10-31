# Language marks: options for the Settings language page

平面视觉, 2026-10-30. Asked by Red through 总控. Status: **Red picked A · TAG on 2026-10-30. Built by 设置菜单 the same day (LANDED 21:43), and Red changed the page from a list to a rack of tiles** (`research/localization/README.md` §7.2). Checked after landing, below. The base sizes in the table still hold; on the rack the tag and name grow with the tile, up to 1.75 ×.

**Figma:** section `3834:8767`, LANGUAGE MARKS. Each lane has the desktop page, the phone page, and the states and small sizes. Component set `3844:81`, Set / Language tag.

**List and behaviour:** `research/localization/README.md` §7 (设置菜单与画质预设; draft until Red's D1). 14 names, never translated; a second-level page ASSIST / LANGUAGE, opened from the first row of ASSIST; no scrolling; focus is not current; a pick is instant.

## Build spec (A · TAG)

**Assets:** `language_marks/LangTag_Outline@8x.png` and `LangTag_Fill@8x.png`, 320 × 224 px, white on transparent, for a 40 × 28 unit tag. The 4× files are the first export; they run out above about 2.3 × canvas scale at the rack's 1.75 ×. Tint them with `Image.color`. The outline is truly hollow, so the sheet shows through. They come from the B key's own tag vectors.

| | Desktop px | Phone pt | Tablet pt |
|---|---|---|---|
| Tag | 40 × 28 | 34 × 23.8 | 38 × 26.6 |
| Tag centre on the row's centre | 16.9 from the tag's top | 14.4 | 16.1 |
| Token, Latin and Cyrillic | Plex Mono Medium 13 | 11 | 12 |
| Token, CJK | gothic Bold 14 | 12 | 13 |
| Name | Source Serif 20 | 17 | 19 |
| Name's x, from the tag's left | 56 | 48 | 53 |
| Row | 48 | 44 | 44 |

- **Not current:** the outline in paper #F4F1E8, the token in paper.
- **Current:** the fill in paper, the token in ink #141414. The name stays paper.
- **Focused:** the row's usual 4 px yellow bar. The tag does not change.
- **Token:** centred on the tag's body, not on its 28-unit frame.
- **Tokens are constants:** EN 简 РУ ES PT DE FR 日 한 PL 繁 TR УК IT. They never pass through `T()`.
- **First row of ASSIST:** `LANGUAGE`, then 14 units on, the row mark: the outline tag with `A文` (`A` in Plex Mono Medium, `文` in the gothic Bold, 13 / 11 / 12). At the right: the current tag, the current name, the chevron.
- **Below 100 %:** CJK tokens have no smaller size. If a future layout needs a smaller tag, drop the token and keep the name.
- **No keyline.** The sheet is a reading surface.

## Check after landing (2026-10-30, 设置菜单's frames, Game L3)

- **Tag on the rail: PASS.** The tag hangs by its ring, 2 units under the tag's top. On desktop it lands on the same pixels as Figma lane A (x 96–160, y 293–336).
- **1.75 ×: PASS.** Desktop 14 names 1.625 ×, 9 names 1.75 ×. The limit is never passed. On the phone and the iPad the width sets the size first (the longest name).
- **CJK tokens: PASS for now.** All four read at every size shown. They still use the system's face, and `繁` sits about 1.5 px low at 1.625 ×. Check again with the real faces.
- **iPad and Duo inner: fixed by 设置菜单, 21:52.** The rack first stopped at 710 pt on an 1180 pt screen, which held the tag near 1.2 ×. It now runs to the safe area (iPad 1132 pt, Duo inner 903 pt). Figma lane A has the iPad frame as built.

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
- **Files, not sprites.** Four tiny subsets for the 12 name characters, each in its own region's forms, plus gothic subsets for the tokens 简 繁 日 文 and 한. Always loaded.
- **Sources:** Noto Sans SC and Noto Serif SC are on this Mac and have no Hangul (read from their cmaps). Needed for the first 9 languages: Noto Sans JP, Noto Serif JP, Noto Sans KR and Noto Serif KR. Needed with 繁體中文: Noto Serif TC and Noto Sans TC. A SC face cannot stand in for 繁體中文 or 日本語, because each name keeps its own region's forms. Downloads need Red's OK. Real text stays sharp at every render size; a sprite does not.
- **CJK size floor:** desktop 16 px at 1080p, phone 12 pt, tablet 13 pt. Tag tokens 14 px and 12 pt. Measured on desktop at 1×: at 13 px dense characters such as 鍵 and 體 close up, 14 px is marginal, 16 px is clear. The phone and tablet values are a judgement and need a check on a device.
- The sizes for CJK labels are a first read and need checking in game.
