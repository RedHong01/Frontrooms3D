# Localization research: sources

All read 2026-10-30 unless noted. Links only; nothing downloaded. Third-party names and data belong to their owners.

| # | Source | What it gives | How it was read |
|---|---|---|---|
| 1 | https://store.steampowered.com/hwsurvey/ | Steam Hardware & Software Survey, September 2026, language table | opened |
| 2 | https://partner.steamgames.com/doc/store/localization | Interface / Full Audio / Subtitles options; store and game independent; "Steam favors products that are localized" | opened |
| 3 | https://partner.steamgames.com/doc/store/localization/languages | Steam's language list with API codes and native names | opened |
| 4 | https://newsletter.gamediscover.co/p/case-study-how-localization-amps | Academia: School Simulator, revenue by language before and after localization | opened |
| 5 | https://store.steampowered.com/api/appdetails?appids=739630&filters=basic | Phasmophobia languages | opened |
| 6 | …appids=3241660 | R.E.P.O. languages (21 listed in the read; the reader's note said 23) | opened |
| 7 | …appids=1943950 | Escape the Backrooms languages | opened |
| 8 | …appids=1092790 | Inscryption languages | opened |
| 9 | …appids=1562430 | DREDGE languages | opened |
| 10 | …appids=2653790 | The Exit 8 languages | opened |
| 11 | …appids=1295920 | The Mortuary Assistant languages | opened |
| 12 | …appids=1721470 | Poppy Playtime languages | opened |
| 13 | …appids=1262350 | SIGNALIS languages | opened |
| 14 | …appids=1944430 | Amnesia: The Bunker languages | opened |
| 15 | …appids=1966720 | Lethal Company languages | opened |
| 16 | …appids=1846170 | Iron Lung languages | opened |
| 17 | https://developer.apple.com/help/app-store-connect/reference/app-information/app-store-localizations | App Store's 50 metadata languages and locales | opened |
| 18 | https://www.gamedeveloper.com/mobile/8-000-games-have-been-pulled-from-the-app-store-in-china-over-missing-isbns | 2020 China App Store approval-number requirement and removals | search summary; 2020 data, nothing newer found |
| 19 | https://allcorrectgames.com/localization-pricing/ | Vendor per-word and LQA rates from English, EUR | opened |
| 20 | https://alconost.com/en/blog/steam-language-mix-indies | Tiers and low-end costs for about 2,500 words; vendor blog built on a podcast, cites no Valve data | opened |
| 21 | https://github.com/adobe-fonts/source-han-serif and its LICENSE.txt | Pan-CJK fonts; SIL Open Font License 1.1 | opened |
| 22 | https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Addressables.html | String and asset tables stored in Addressable groups | opened |
| 23 | https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html | Features: strings, assets, pseudo-localization, XLIFF, CSV, Google Sheets, Smart Strings | opened |
| 24 | https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-colour-alone/ | State not by colour alone | cited through source 25 |
| 25 | `We'are Car/research/uiux/uiux-language-flags-2026-10-30.md` (another project, read-only) | Flags vs names in 7 AAA titles and indie cases; risks; Red's option D there | opened |
| 26 | https://www.w3.org/International/questions/qa-site-conneg | Advice against flags for languages | cited through source 25 |
| 27 | https://designsystem.digital.gov/patterns/select-a-language/three-or-more-languages/ | Same | cited through source 25 |
| 28 | https://www.videogameschronicle.com/news/valve-has-significantly-rewritten-steams-rules-for-how-developers-much-disclose-ai-use and https://finalboss.io/valve-s-2026-ai-disclosure-update-actionable-guide | Steam's January 2026 AI disclosure change; translation in scope per press | search summary; Valve's own wording not opened |

Measured here, not from the web: font coverage (`~/FrontRoomsInteractionWork/settings/localization/font_coverage.py` on the TTFs in main), word counts (`string_inventory.py` + sorting by hand).
