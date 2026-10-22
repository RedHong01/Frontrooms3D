# Shot: the format for system screenshots (平面视觉, 2026-10-22)

**Red, 2026-10-22, in the 平面视觉 chat:** "我希望以后所有这种图片，首先需要打上Frontroomsss的这个logo水印，然后有一个固定格式，这个应用到你所有的系统内部截图的formatting里，可以适当参考我的typographic system，但是grid system上不需要参考因为这种紧凑的布局是可以的，这个规则应用在所有视觉累的系统截图规范里" (from now on, every image like this first gets the FRONTROOMSSS logo watermark and one fixed format; it applies to all internal system screenshots; it may draw on the typographic system, but not on its grid, because the compact layout is fine; the rule applies to every visual line's system-screenshot spec).

So every system screenshot made by any line from 2026-10-22 on uses this format. **Use the tool, never hand-drawn captions:** `~/FrontRoomsVisualWork/tools/shotframe/shotframe.py`. One tool means one look, and a later change to the format reaches every line at once.

## 1. What it applies to

**Applies to every system screenshot** (the "verification image" of `VERIFICATION_LOG.md` §1.1, wherever it is saved):
- before/after pairs, contact and review sheets, comparison grids;
- harness or Play-mode test frames used as evidence;
- heatmaps, difference images, masks shown to people;
- measurement plates and distance strips;
- look-dev checks and WIP previews someone reviews.

**Does not apply to:**
- art deliverables and their direct renders: textures, print art at print density, logo files, the placard sheets in `Tools/print/…`. These must stay byte-identical to what ships;
- Figma boards (they follow the board system), decks (P1) and marketing or store media (Ad system);
- raw inputs a script still reads. Keep the raw file next to the framed one as `<name>_raw.png`.

## 2. The structure (top to bottom)

1. **Header band**, ink #0A0A0A (the document ink of the Info and Ad systems and the VL slides; not the HUD keyline #141414), full width:
   - **Row 1:** the TITLE on the left, a verdict chip right after it, and the **logo watermark at the right end**;
   - **Row 2:** one sentence: the question or the verdict, in words a reader gets without the log;
   - **Row 3:** META: `VL### · YYYY-MM-DD HH:MM · LINE · path · build / seed / settings`.
2. **Evidence:** the image, or a grid of tiles. Each tile has a caption strip above it: a LABEL and its data on one line.

There is no footer and no outer margin, and the gutters are 4 px. Compact on purpose (Red: no deck grid).

**The evidence is never covered.** The watermark lives in the header band, never on top of the pixels being judged.

## 3. Type (derived from the P1 deck styles; sizes at a 1920 px wide image)

| Role | Face | Size | Colour |
|---|---|---|---|
| Title | Bayon, caps | 44 px | paper `#F4F1E8` |
| Sentence | Source Serif 4 Regular, optical size = the size | 22 px, line pitch 30 | paper |
| Meta | IBM Plex Mono Regular, as written (paths are case-sensitive) | 14 px, line pitch 20 | muted `#BDBAB0` |
| Tile label | Bayon, caps | 22 px | paper |
| Tile data | IBM Plex Mono Regular | 14 px | muted |
| Verdict chip | IBM Plex Mono Medium, caps | 14 px in a 24 px box | §5 |

- **Scale:** every size and gap scales with the image width, u = width ÷ 1920, never below 0.6.
- **Digits and "·" inside Bayon text are set in IBM Plex Mono Medium at the same size.** Bayon has no "·" and draws 1 and 0 as I and O. The cap heights match (both 0.70 em), so the runs share one baseline.
- **English only:** these faces have no CJK glyphs. Line names are written in English: GAME VISUAL, GRAPHIC VISUAL, INFORMATION DESIGN, PRINTS, ADVERTISING, LEVEL DESIGN, INTERACTION, MOTION, STORE, WEBGL.

## 4. The logo watermark

- **Version:** the white FRONTROOMSSS wordmark (`Assets/Resources/Brand/FrontRoomsLogo_White.svg`, rendered to `W/tools/shotframe/assets/logo_white.png`) on the ink band.
  - The two relay S fade from the band into the ink: S1 to 100 %, S2 to 25 % (memory `frontrooms-logo-relay-gradient-rule`).
  - `logo_black.png` is the same mark in ink #0A0A0A for a light ground. No light band exists yet.
- **Size:** the letters are exactly as tall as the title's caps (0.70 × 44 = 30.8 px at 1920 wide).
- **Place:** right-aligned at the band's side padding (24 px), its letter tops on the title's cap top.
- **Clear space** (广告's logo rule, AD_TYPE_SYSTEM §6): at least half the letter height between the logo and the title or chip. The title truncates at a 32 px gap (× u), which is more than half of 30.8.
- The tool crops the PNG's transparent padding at load (`_logo()`), so the letters, not the canvas, match the caps. Measured 10-22 on `tp2_optA_distance_strip.png` at u 1.375: logo letters 42 px, title caps 42 px (expected 42.3).
- Never recoloured, stretched or cropped. Never made semi-transparent over the evidence.

## 5. Verdict chips (the words of `VERIFICATION_LOG.md` §1.6)

| Look | Words |
|---|---|
| Paper fill, ink text | PASS, APPROVED, ACCEPTED, MERGED, DONE, PICK … |
| Safety red `#C70F2E` fill, paper text | FAIL, RISK |
| Yellow `#F4DF3B` fill, ink text (Info's decision yellow: the one colour for an open decision of Red's) | WAIT-RED |
| 1 px paper outline, paper text | FLAG, PARTIAL, FINDING, BASELINE, FAIL→FIXED |
| Dashed muted outline, muted text | PENDING, SUPERSEDED |

## 6. Layout numbers (at 1920 px)

| Element | Number |
|---|---|
| Header band padding | 20 top, 16 bottom, 24 sides |
| Title → sentence gap | 14 |
| Sentence → meta gap | 10 |
| Title max width | band width − logo − 32 − chip (it truncates with "…", it never shrinks) |
| Tile caption strip | 34 tall, 10 padding; label, a 12 gap, then data on one baseline |
| Gutter between tiles and rows | 4 |
| Tile scaling | Lanczos. Nearest-neighbour for simulated screen pixels (distance strips, sprites) |

## 7. Files

- **Formats:** JPG q92 for frames and photographic sheets; PNG for UI, masks and anything compared pixel for pixel (as `VERIFICATION_LOG.md` §1.2).
- **Where:** save under `Frontrooms3D/Documentation/research/<area>/images/` as before. The framed file takes the normal name; a raw input a script still reads keeps `_raw`.
- **Figma:** the VL slides take the framed image as it is. The slide's own title and verdict stay the log's.

## 8. How to use it

```
# one image
~/FrontRoomsVisualWork/venv/bin/python ~/FrontRoomsVisualWork/tools/shotframe/shotframe.py wrap in.png out.jpg \
  --title "Office ivory · same plate" --sentence "Needs Red's call: one material per finish today." \
  --meta VL351 --meta "2026-10-22 09:30" --meta "GAME VISUAL" --meta "research/outlets/images/50_fix1_office_ivory.jpg" --status WAIT-RED

# a grid with a caption per tile ('path|LABEL|data'); --nearest for simulated screen pixels
… shotframe.py grid out.png --cols 3 --width 1920 --tile "a.png|D|#A69F8C · 0.91x" --tile "b.png|A|#E6DDC2 · 1.33x" --tile "c.png|C|#B6AF99 · 1.03x" --title "…" --status WAIT-RED
```

From Python (workflows):

```
import sys; sys.path.insert(0, '/Users/redwang/FrontRoomsVisualWork/tools/shotframe'); import shotframe as S
ev = S.grid([(img_or_path, 'LABEL', 'data'), …], cols=3, width=1920)
S.save(S.frame(ev, 'Title', 'One sentence.', ['VL351', '2026-10-22 09:30', 'GAME VISUAL', 'path'], 'PASS'), 'out.jpg')
```

Fonts come from `~/Library/Fonts`, or from `Frontrooms3D/Assets/Resources/Fonts`. Python is `~/FrontRoomsVisualWork/venv` (Pillow).

## 9. Owner and changes

平面视觉 owns the format (type-system registry, `GRAPHIC_VISUAL_SPLIT.md` §1). A change to it is made in the tool and in this file, and is announced to every line.

Log:
- 2026-10-22 09:2x: v1, after Red's rule above. First use: `research/placard/images/tp2_optA_distance_strip.png` and `tp2_optA0_distance_strip.png`.
- 2026-10-22 09:5x: v1.1, after 信息设计's review: ink #141414 → #0A0A0A, the document ink. It changes only the band colour and the ink text on paper chips; no layout change. The two first-use images were re-rendered. 信息设计 found no other conflict with the Info system: the chip ladder is its Status-tag ladder turned for the dark band, and yellow means only an open decision.
