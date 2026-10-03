# Period type kit (1988–93): game-safe fonts

FrontRooms is set in 1990. This kit covers in-world graphics: store signs, price tags, posters, catalogues, the TV ads and shopping-channel screens. Research behind it:
- `Research/week02/furniture-ads/` (sources, crops, notes);
- the Figma section "FRONTROOMS · 1990 FURNITURE MEDIA · TYPE + GRID" on page 2099:76.

Owner: the graphic-visual chat (平面视觉) curates the list. The visual and UI chats choose where the fonts are used. The UI fonts in `Assets/Resources/Fonts` (Bayon, IBM Plex Mono, Source Serif 4) are a separate system and are not changed by this kit.

**Files:** `Assets/Fonts/Period1990/<Family>/`.
- Static `.ttf`/`.otf` files only, with the licence next to each family.
- Unity imports a variable font at its default instance only, so the condensed, expanded and soft cuts were baked from the variable masters as separate files.
- Nothing here is in `Resources/`, so a font ships only if a scene or asset uses it.

**Licences:**
- SIL OFL 1.1 for most families: free to embed, bundle and modify. A modified version may not keep a Reserved Font Name.
- GUST Font License for TeX Gyre (LPPL-style, free to embed).
- Apache 2.0 for Yellowtail.

None of the original period faces can ship. They are either commercial (ITC, Linotype, Monotype) or licensed only with macOS or Office. If the exact face is ever wanted, buy an app/game embedding licence from its foundry; the stand-ins keep working until then.

## Period face → game-safe stand-in

| Period face (where we saw it) | Original status | Use in the game | Settings / note |
|---|---|---|---|
| Helvetica Regular/Bold (Natuzzi, Hekman, W&K, Barrons; most TV CG supers) | Linotype; Mac system | `TeXGyre/texgyreheros-*.otf` | Metric clone of Helvetica |
| Helvetica Black and Black Oblique (Levolor, HG "Venice Rising", Steelcase, reff; Levitz CG) | Linotype | `Archivo/Archivo-Black.ttf`, `-BlackItalic.ttf` | Track −10 to −20 for the 1990 tight look |
| Helvetica Condensed Black/Bold (Trane, illbruck/Luxo, Huffman Koos CG, La-Z-Boy "PICK A PAIR") | Linotype | `Archivo-CondensedBlack`, `-ExtraCondensedBlack`, `-ExtraCondensedBold`, `-CondensedBlackItalic`; `TeXGyre/texgyreheroscn-*` | The ExtraCondensed cut matches TV "EVERYTHING IS ON SALE" supers |
| Helvetica Extended Black (Johnson Controls, Allsteel, Heilig-Meyers prices) | Linotype | `Archivo-ExpandedBlack`, `-ExpandedBold` | |
| Futura Bold/Heavy (HG decks, IKEA logo base, Haworth) | Neufville/URW; Mac system | `Jost/Jost-Medium`, `-Bold`, `-ExtraBold`, `-BoldItalic`; `LeagueSpartan-ExtraBold` | Letterspace +80 to +120 for HG-style decks. There is no free Futura Condensed: use Jost at 85 % width or Archivo Condensed |
| ITC Avant Garde Gothic (Paoli "Contempo 2", Levolor "TODAY", Home masthead, Office Specialty) | ITC/Monotype | `TeXGyre/texgyreadventor-*.otf` | Clone of ITC Avant Garde (via URW Gothic). The original ExtraLight has no free equivalent, so use Regular at large sizes |
| ITC Garamond (Hickory White, Jenn-Air, Edgar B, Leather Center, KI, FIRE!) | ITC/Monotype | `CrimsonPro/CrimsonPro-*` | Closest free x-height. Set tight (tracking −20 to −40, "tight but not touching"). For condensed uses, scale width to 85 % |
| Garamond, classic body (HG body, Steelcase body) | Office/Monotype | `EBGaramond/EBGaramond-*` | |
| Goudy Old Style (Baker, McGuire) | Office/Monotype | `SortsMillGoudy/SortsMillGoudy-Regular`, `-Italic` | Goudy Italic for Baker-style copy |
| Times (Roche-Bobois, Steelcase tagline, Rooms To Go; serif CG in Seaman's, Krause's, Okum's) | Linotype/Monotype; Mac system | `TeXGyre/texgyretermes-*.otf` | Metric clone of Times |
| ITC Cheltenham Bold Condensed (Four Seasons, Oxford, Country Living cover lines; Wickes TV) | ITC/Monotype | `RobotoSerif/RobotoSerif-CondensedExtraBold`, `-SemiCondensedBold` | No free Cheltenham exists. This is the closest sturdy condensed serif, not a clone |
| Didone display (HG masthead and "Notes", Natuzzi logo, Elle Decor, Home "Traditional", Rooms To Go "3") | Linotype Didot / Bodoni; Mac system | `BodoniModa-DisplayBold`, `-DisplayBlack`, `-DisplayBoldItalic`; `PlayfairDisplay-Black`, `-BoldItalic`; `AbrilFatface`; `LibreBodoni` for text | Display cuts are baked at opsz 96 |
| Condensed Didone, Onyx-like (Nancy Corzine, House Beautiful masthead) | Monotype Onyx | `InstrumentSerif/InstrumentSerif-*` | |
| Friz Quadrata (American Seating, Office Specialty "Mo Knows Filing") | ITC/Monotype | `Marcellus/Marcellus-Regular` | Flared-serif stand-in, not a clone |
| Art-Nouveau display: Benguiat / Belwe type (Karastan, Sherrill) | ITC/Monotype | `Fraunces/Fraunces-WonkySemiBold`, `-WonkyLight`; `YoungSerif` | Flavour stand-in only. The free "Belwe" online is the blackletter Belwe Gotisch, so it was not used |
| Cooper Black (Seaman's logo, QVC Gift Shop sign, Room Plus "Just Round The Corner") | Office/various | `Fraunces/Fraunces-SoftBlack`, `-SoftBlackItalic` | Baked at SOFT 100, WONK 0, opsz 144, wght 900 |
| Eurostile Bold Extended (Dial-A-Mattress, Krause's, Room Plus, Seaman's "SAVINGS") | Linotype/Nova; Office regular only | `Saira-ExpandedBold`, `-ExpandedBlack`, `-Bold`, `-Regular`; `Michroma` | |
| Franklin Gothic Heavy/Condensed | Office/ATF | `LibreFranklin-Bold`, `-Black`, `-BlackItalic`; `Oswald-Regular`, `-Bold`; `Anton` | Anton matches the BHG and TV heavy condensed cover lines |
| Century Schoolbook / Bookman (dealer copy, Warner sub-line) | Office | `TeXGyre/texgyreschola-*`, `texgyrebonum-*` | |
| Engraved script (Baker, Charles Barone, Karastan logos) | Mac Snell Roundhand / Kuenstler | `PinyonScript/PinyonScript-Regular` | |
| Brush script (Merillat, Kirschman's, Heilig-Meyers) | Mac Brush Script | `Yellowtail/Yellowtail-Regular` | Apache 2.0 |
| TV character generator, typewriter, teletext (dealer tags, HSC item specs, phone numbers, QVC panels) | broadcast CG hardware | `VT323/VT323-Regular`, `CourierPrime/CourierPrime-*` | Add a 2–4 px black outline and drop shadow, as on period CG |
| Modern No. 20 (Met Home "The Gathering Room", 1992; WhatTheFont match) | Stephenson Blake / Bitstream; Office copy local only | `OldStandardTT/OldStandard-Regular`, `-Italic`, `-Bold` | Condensed Scotch/modern face; for compressed headlines scale width to 80–85 % |
| Wide slab, Figgins Antique type (Met Home "STYLE PREVIEW", 1989; WhatTheFont match) | commercial | `HoltwoodOneSC/HoltwoodOneSC-Regular` | Caps only; track +40 to +80 |
| Gill Sans Bold type (Home pull quotes ≈18/22 with ■■■■■ end mark) | Monotype; Office copy local only | `Cabin/Cabin-Bold`, `-SemiBold`, `-CondensedBold` | Humanist stand-in, not a clone |
| Clarendon / slab (Interiors masthead, Waterbed City and Gallery Furniture TV) | commercial | `ZillaSlab/ZillaSlab-*` | Masthead caps tracked +200 to +400 |

## Body copy (measured 2026-10-03; see the FINDINGS section "Body copy, measured")
These rows replace the guessed body settings. Sizes are the printed point sizes; scale them to the texture or world unit, and keep the ratios.

| Period face (where we saw it) | Original status | Use in the game | Settings / note |
|---|---|---|---|
| Helvetica Condensed (Sears catalogue copy, 1993) | Linotype | **Roboto Condensed** Regular, Bold (static cuts pending download). `TeXGyre/texgyreheroscn-*` is the closer clone if the Figma match doesn't matter | 8/9, tracking 0, justified, about 1 line in 9 hyphenated, 14p0 columns with 1p2 gutters. Bold only for the key letter, the item name, the catalogue number and the prices. Key letter + em space + run-in name. Items 6 pt apart. Notes 7/8 |
| Italic old-style price figures, italic aside (Sears display price and headline) | ITC Garamond Light Italic type | **Cormorant Garamond Light Italic** (pending) | Old-style figures by default. Price ≈28 pt, raised $ and cents ≈13 pt, top-aligned. Headline aside 22 pt next to Archivo Black 20 pt |
| Helvetica Oblique (Sears price labels) | Linotype | `Archivo/Archivo-Italic` | 8/8, two lines beside the price |
| Cochin (Met Home body copy 1989–92) | Linotype; macOS only | **Cormorant Garamond Bold** for text, **Cormorant SC Bold** for the lead-in, **Cormorant Infant Bold** for the digits (lining) (all pending) | 9½/13, tracking +50 (Cochin is wider), ragged right, about 1 line in 4 hyphenated, 9p9½ columns with 1p5 gutters. No Th ligature: Cochin has none, and TMP doesn't apply ligatures anyway. Prices in parentheses without cents |
| Trade Gothic Bold Condensed No. 20 / Condensed No. 18 (Met Home keywords and decks) | Linotype | `Oswald/` Medium for keywords, Light for decks | Keywords at body size with x-height 1.3× the serif's (8.2 pt Oswald beside 9½ pt Cormorant), no tracking. Decks +3 to +4 %, staggered |
| Gill Sans SemiBold (Met Home editor's page) | Monotype; macOS/Office | `Cabin/Cabin-SemiBold` | ≈11/16, +50, justified, 2 columns |
| Bodoni price lockup (Met Home "$278,311.00 / RICH AND FAMOUS") | Bauer/ITC Bodoni type | `BodoniModa/` Regular + `Oswald/` Medium caps on a reversed tab | Cents raised at about 42 % of the figure size. The tab's caps are spaced +34 % |

**Static cuts still to add** (Google Fonts static TTFs, OFL; download pending Red's approval):
- Roboto Condensed Regular, Bold and Italic;
- Cormorant Garamond Light Italic and Bold;
- Cormorant SC Bold;
- Cormorant Infant Bold.

The Mac has Roboto Condensed and Cormorant Garamond only as variable fonts, and Unity would import those at their default instance.

## Using them in Unity
- **Legacy Text / TextMesh:** assign the imported Font.
- **TextMeshPro:**
  1. Open Window ▸ TextMeshPro ▸ Font Asset Creator.
  2. Pick the `.ttf`/`.otf` as the source font.
  3. Set the atlas to 1024 or 2048 and the character set to ASCII + Latin-1.
  4. Save `<File> SDF.asset` next to the font. Outline and shadow live in the material, so CG-style supers need no extra textures.
- **World graphics** (posters, price tags, TV screens): render the text into the texture, or use TMP in world space. Follow the period rules in the Figma section: superscript $ and cents, outline plus drop shadow on TV supers, letterspaced small caps, and tight-set serif headlines.

## Where things are
- Variable masters (for Figma and Blender), specimens and all downloaded licences: `Research/week02/furniture-ads/fonts/`.
- Download sources: Google Fonts repository (github.com/google/fonts), the Google Fonts CSS API (static instances), and CTAN `fonts/tex-gyre` plus the GUST licence (gust.org.pl).
