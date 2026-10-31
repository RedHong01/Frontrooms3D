# Bathroom rooms · 40 · Direction images for Red (stage 2)

Date: 2026-10-31. Line: 浴室房间调研, K session (Red's API key, Cursor, `cursor_K_bathroom_1031`).

Red, 10-31 ~14:00 (K chat, answer to D1): "全部都做，像是Claude那样出了图片之后我来负责筛选方案/variation". Read as: all three directions are built, the club on B2 leading into a pool level beyond it ((c) in full, not split), and the look is chosen from images: Red screens schemes and variations before the build commits. D2–D12: "按推荐（随 D1 调整）".

## 1. What the images let Red choose

1. The club (a): palette, how high the tile line runs, how bright.
2. The pool level (b): how much tile, how deep the room water reads, how dark, how much repetition.
3. The turn (c): where the club stops making sense and how fast.

Not chosen here: the lamp fixtures (Red picks them in `zone_lamps/30_for_red.md`; the stills use 游戏视觉's lean as placeholders) and the water rules (D3–D6, decided).

## 2. Shot list

Eye height 1.6 m, desktop at highest spec, Shot format through `W/tools/shotframe/shotframe.py`.

| # | Dir. | View | Variations |
|---|---|---|---|
| S1 | a | Entry: towel desk, FITNESS CENTER · TENANTS ONLY, corridor to the lockers | palette ×3 |
| S2 | a | Locker room and gang shower | palette ×3 |
| S3 | a | Pool hall from the shallow end: lane lines with T ends, depth marks, guard chair, wet-niche lamps | palette ×3; tile line low / high |
| S4 | c | The far door across the pool, open: the first wading room beyond, its light reaching the hall first | change starts at the door / one room later |
| S5 | c | First transition room: tile past the line, a missing sign, ankle water over the sill | slow / fast change |
| S6 | b | Room chain: ankle water through three doorways, light with no visible source | pale white / pale blue / one mint band |
| S7 | b | Basin room: ladder, gutter, depth marks, empty guard chair, a dark pocket | lit / half dark |
| S8 | b | Looking back toward the club from deep in the level | none |

About 20 stills: one contact sheet per direction, then one sheet of Red's picks.

## 3. How

- Greybox stills in a private clone under `W` (`tools/mkclone.sh`, a 6.6 Library from `proj_ref66` with `cp -Rc`, `tools/install_tools.py`). Renders Red asked for are deliverables, not tests (kickoff §3). Nothing makes a sound (rule 24).
- No shared rendering code is edited: `FrontRoomsSurface.shader`, `RenderSetup`, `RoomStream` and the lamp kit are 游戏视觉's. Water is the probe-only surface (P4 comes later).
- Palettes start from D8's rows (B: white pool, mint band, quarry-red deck, beige lockers; C: white pool, teal band, off-white slip tile, grey lockers) plus 游戏视觉's options sheet if it exists.
- Guard: rectangles on the 3 m grid; no arches, domes or skylit vaults; nothing taken from any creator's pool images, POOLS, Kane Parsons or the two 2026 films (`zone_lamps/10_pool.md` §6).
- Asked 游戏视觉 (`W/workspace_repo/remote/bathroom/OUTBOX.md`, card bathroom-101): which lookdev path and scene made its earlier options sheets, whether its lamp lean may stand in, and whether the D8 sheet folds into these stills.

## 4. Cost

6–10 PAH, inside the new estimate (`30_estimate.md` §5, row "D1 all").
