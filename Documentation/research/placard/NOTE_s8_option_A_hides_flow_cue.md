# For the narrative chat and for Red's §8 decision: option A hides the FLOW cue (raised by 平面视觉, 2026-10-07)

> **RESOLVED 2026-10-22.** This header was added 2026-10-29 by 叙事设计, the addressee.
> - Red chose **B: the walls never glow** (叙事设计 chat, account B, 12:34: "Q4 保持 B：墙不发光").
> - Red chose it after being told that glow A no longer hid the cue (VL350, the wallpaper line's answer below).
> - EGRESS rev 4 records it (`research/wallpaper_motion/30_narrative_phosphor.md`).
> - Options (a)–(c) are moot. Only the placard's own legend glows (A.12; k stays 0.158).
> - Kept as history.

**2026-10-21 (Red, "全部按推荐" in the 游戏视觉 chat): the placard glow k stays 0.158.** The §8 A/B glow question itself belongs to narrative + 平面视觉 and is not settled by this.

**Problem.** Under the literal wording of §8 option A, only cream ink glows in a power failure. The FLOW state swallows the cream band, so the turned band is a grey one. In a power failure the wall would therefore light only the vertical stripes, and the cue itself (the turned band) would not show. 平面视觉 drew the result: `Tools/print/ink/art_from_graphic/placard/legend/<state>_cream.png`.

**Context.**
- The placard art is now v2, after Red's call today to redraw it to match the new wall hint.
- Its legend shows WP03 wallpaper swatches at 1/25: AS PRINTED, WAY ON (FLOW turned 90°), EXIT, and NO EXIT (flattened).
- The placard's own phosphor overprint follows A.12, "legend glows": the heading, the text and the turned or flattened band glow, independent of the wall's §8 A/B option.

**The narrative chat must choose one of:**
- (a) make the turned or flattened band glow under option A whatever its colour;
- (b) switch to option B;
- (c) re-assign FLOW's bands so the cue band is cream.

Send the answer to 平面视觉 and the visual chat. Red should see this before deciding §8.

**Status.** Logged 2026-10-07 by the visual chat. The narrative chat was offline, so this file is the relay.

## 2026-10-22 answer for the in-game geometry (wallpaper line, 动画和动态图形模型技术)

The problem above was true of the old single-row cue geometry (`states.py`). Re-rendered on the set the game uses, **A + M4 + V1** (`cue_states/cue_flipbook.py`), it no longer holds:

- Every arrow turns whole and is fitted to its field, so **its cream stroke turns with it**. Under option A the dark wall shows the cream stroke as a ">" where the baseline shows a "^".
- On the old geometry, FLOW lost its cream chevron (0.66 % of the legend swatch) and nothing turned in the dark. On A + M4 + V1, 0.78 % of the swatch changes (1.6 % of a whole tile), and the change is the turned stroke.
- How the files were made: they are sampled from the shipped flipbook slices C00/C03 through the v3 legend window, by `Tools/print/ink/art_from_graphic/placard/legend_cream_A.py` → `placard/legend_A/`. The baseline matches the v3 legend (mean |Δ| 0.010, 0 texels > 0.5).
- Verification: VL350 (Figma 3496:7325), with images in `research/placard/images/vl350_glowA_flow_{legend,wall}.png`.

So options (a) and (c) above are not needed for the in-game set. Option A shows the FLOW cue as one thin cream stroke per field.

Not measured: whether that stroke reads in game at the phosphor brightness (the wall-glow A/B choice is still Red's, with 叙事设计 and 平面视觉). HERE and STOP were never built on A + M4 + V1, so they are not covered.
