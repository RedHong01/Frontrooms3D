# Still desk screens (tutorial T3 → 游戏视觉 V4) · v2

From 平面视觉 · 广告, 2026-10-22. Generator: `Tools/screens/still_desk.py`. To re-render:

```
python3 still_desk.py
```

**Why.** Red approved T3 in the 教程设计 chat on 2026-10-22: "T3 静止屏". On a floor whose key is untaken, the maze's Office desk CRTs stop playing the ad and show a still, dim office screen. The station TV (the beacon) is then the only screen that moves or makes sound. Spec: `Documentation/research/tutorial/10_proposal.md` §4.4 and `20_requests.md` V4.

**v2 (the same day) follows 平面视觉 · 信息设计's review** against the terminal spec, `Documentation/research/computer_ui/20_design.md`:
- **§3.5 / AT-T19.** An ambient frame never uses the terminal's field colour (VGA blue) or its header/footer layout. Any text it shows must meet the glance sizes of §2.3 (a cap of 86 or 120 px of 480), or there is no text.
- **v1's text was the wrong size.** It used body-size text (cap 21), which reads only at walk-up distance. It invited a walk-up and so faked a READ terminal.
- **The alarm-log frame is dropped.** It printed a zone number before any numbering was agreed, and TROUBLE is the sanctuary lamp's word.
- **v1 is out of the repo,** in `~/FrontRoomsVisualWork/screens_dev/out_still_v1_superseded/`.

## Files

Each comes as PNG at 640 × 480 plus `@2x` at 1280 × 960. That is the terminal's logical raster (§8.2): a grid of 40 columns × 15 rows of 16 × 32 px.

| File | What it shows | Use |
|---|---|---|
| `desk_cursor` | A dark field with one still block cursor at column 5, row 2, where a prompt sits after CLS (prompt text left out) | Most desks |
| `desk_cursor_low` | The same, with the cursor at column 5, row 9: a session left part-way down | Some desks |
| `desk_blank` | A powered, blanked field: no cursor | Some desks |

`still_desk_sheet.png` is a review sheet only.

**Glance frames** (one short word at a glance size, e.g. cap 86 px reading to 1.9 m on desktop) are supported by the generator but **empty**. Their words have to come from 叙事设计, era-checked. `SYSTEM IDLE` and `OCCUPANT LOAD` still lack a pre-1991 source.

## How they should sit in the game (a request; the wiring is 游戏视觉's)

- **Still means still.** One texture per monitor, with no blink, flicker, scroll or VideoPlayer. The cursor does not blink. Don't switch screens while the player can see the monitor.
- **Dim is in the art.** The cursor is paper-white phosphor at about 60 % (150, 156, 160) on a near-black field (9, 10, 11). Show it 1:1 with no material boost, so the station TV stays the brightest screen. Off sets (dark glass) stay fine for some desks.
- **Neutral white,** because the terminal spec keeps amber for the TROUBLE lamp, green for glow-ink guidance and VGA blue for a terminal you can READ.
- **Mix per floor, seeded per desk** so the choice is stable across visits: `desk_cursor` most often, `desk_cursor_low` and `desk_blank` sometimes, some sets off.
- **No tube effects are baked in;** only a faint halo. The cursor sits inside RP 27.3-1989 title safe.
- When C20 (the terminal face) is decided, the idle and READ screens are re-rendered together.
