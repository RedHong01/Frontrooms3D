# Door wood: media candidates (for Red to approve)

Written 2026-10-30 by 游戏视觉 (research stage, session local_5656cffd). Nothing below has been
downloaded. Red ticks what may be fetched; the build stage downloads only ticked rows, into
`/Users/redwang/FrontRoomsVisualWork/` (never into the project until a look is picked).

Sizes are the server's own listing (api.polyhaven.com/files/<id>, ambientcg.com/view) on 2026-10-30.
"Set" = Diffuse + nor_gl + Rough + Displacement, JPG. MB = 10^6 bytes.

## A. CC0 scan sets (texture source for the leaf faces)

| ok? | Set | URL | Licence | Max res | Physical size | Wanted res and size | Use |
|---|---|---|---|---|---|---|---|
| [ ] | Poly Haven `red_oak_veneer` (raw, scanned 2026-08) | https://polyhaven.com/a/red_oak_veneer | CC0 | 8K | 1000 x 1000 mm | 4K set 30.8 MB (2K set 7.9 MB) | Look 1 (plain-sliced red oak) |
| [ ] | Poly Haven `oak_veneer_01` ("fine vertical grain") | https://polyhaven.com/a/oak_veneer_01 | CC0 | 16K | 1830 x 1830 mm | 4K set 43.1 MB (8K set 152.6 MB) | Look 3 (rift/straight oak); 1.83 m covers a whole leaf height with no repeat |
| [ ] | Poly Haven `white_maple_veneer` | https://polyhaven.com/a/white_maple_veneer | CC0 | 8K | 1000 x 1000 mm | 4K set 23.9 MB | Look 2 stand-in (no CC0 birch scan found); check the cut on the page first |
| [ ] | Poly Haven `cherry_veneer` | https://polyhaven.com/a/cherry_veneer | CC0 | 8K | 1000 x 1000 mm | 4K set 23.8 MB | Spare (late-era cherry, executive floors) |
| [ ] | Poly Haven `white_oak_veneer` | https://polyhaven.com/a/white_oak_veneer | CC0 | 8K | 500 x 500 mm | not sized yet | Only if a white-oak look is wanted; 0.5 m tile means 4+ repeats per leaf |
| [ ] | ambientCG `Wood049` (oak, PROCEDURAL Substance, not a scan) | https://ambientcg.com/view?id=Wood049 | CC0 | 8K | ca. 800 x 800 mm | 2K zip 22 MB / 4K zip 83 MB | Fallback only; listed for completeness |

Not CC0 (reference only, do not buy without Red): Poliigon slip-match / random-match oak veneer sheets,
e.g. https://www.poliigon.com/texture/slip-match-oak-wood-veneer-texture-light/3945 (shows how archviz
scans a whole matched sheet rather than a tile).

## B. Reference images (look only; not shipped, not copied into the game)

| ok? | What | URL | Rights | Size |
|---|---|---|---|---|
| [ ] | Lynden Door product sheet, plain-sliced red oak door face (book-matched flitch) | https://www.lyndendoor.com/wp-content/uploads/2023/02/Lynden-Door-Wood-Veneer-web-product-sheets_R_PS-Red-Oak.pdf | manufacturer, (c) | 1 PDF, size not checked |
| [ ] | Lynden Door product sheet, rift red oak | https://www.lyndendoor.com/wp-content/uploads/2022/08/Lynden-Door-Wood-Veneer-web-product-sheets_AC_Rift-Red-Oak.pdf | manufacturer, (c) | 1 PDF, size not checked |
| [ ] | VT Industries bulletin: veneer face match and assembly | https://www.vtindustries.com/webres/File/architectural-doors/Technical%20Bulletins/VT-Technical-Bulletin-Veneer-Face-Match-and-Assembly.pdf | manufacturer, (c) | 1.3 MB PDF (image-only; needs a PDF viewer) |
| [ ] | VT Industries bulletin: natural veneer and variations | http://www.vtindustries.com/webres/File/architectural-doors/Technical%20Bulletins/VT-Technical-Bulletin-Natural-Veneer-and-Variations.pdf | manufacturer, (c) | 1.1 MB PDF (image-only) |
| [ ] | VT Industries bulletin: quarter vs rift red and white oak | https://www.vtindustries.com/webres/File/architectural-doors/Technical%20Bulletins/VT-Technical-Bulletin-Quarter-Vs-Rift-Sliced-Red-and-White-Oak.pdf | manufacturer, (c) | not checked |
| [ ] | Masonite Architectural Cendura veneer sheets 161/162/164/165 (species x cut swatches) | https://www.arcat.com/datasheets/masonite_architectural/161_Cendura_Veneer.pdf (and 162/164/165) | manufacturer, (c) | not checked |
| [ ] | USF Gandy Photographs, office interiors c. 1980 (check each for doors) | https://digitalcommons.usf.edu/gandy_commercial/785 , https://digitalcommons.usf.edu/gandy_commercial/8831 , https://digitalcommons.usf.edu/gandy_commercial/8839 | per item: CC BY 3.0 or CC BY-NC 3.0 | single JPGs |
| [ ] | Melbourne School of Design archive, "Flush panel doors" (2014 photo, construction reference, not era) | https://archive.msd.unimelb.edu.au/items/show/8551 | CC BY-NC-SA | single JPG |
| [ ] | The Silence of the Lambs (1991) stills, FBI office interiors (frame look only) | https://screenmusings.org/movie/dvd/The-Silence-of-the-Lambs/pages/The-Silence-of-the-Lambs-0051.htm | (c) Orion; reference only | stills; doors not yet verified in frame |

## C. Technique sources (papers/code; no media needed)

| What | URL | Licence |
|---|---|---|
| Marschner et al. 2005, finished-wood appearance; released diffuse / fiber-direction / fiber-colour / highlight-width maps | https://www.cs.cornell.edu/~srm/publications/SG05-wood.html | research data, check terms before any use |
| Larsson et al. 2022, procedural solid wood with knots (code on GitHub) | https://github.com/marialarsson/marialarsson.github.io/blob/main/procedural_knots.html | code licence to check |
| Burley 2019, histogram-preserving blending for randomized tiling (JCGT) | https://www.jcgt.org/published/0008/04/02/paper.pdf | paper |
