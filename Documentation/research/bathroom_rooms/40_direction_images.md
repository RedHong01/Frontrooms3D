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
| S5 | c | First transition room: tile past the line, a missing sign, ankle water over the sill; the club's wear and colour drain away (15 §5) | slow / fast change |
| S6 | b | Room chain: ankle water through three doorways, light with no visible source, white only | pale white / pale blue / one mint band; rectangular doors / round-headed doors with bullnosed corners (Red's call, 15 §5) |
| S7 | b | Basin room: ladder, gutter, depth marks, empty guard chair, a dark pocket | lit / half dark |
| S8 | b | Looking back toward the club from deep in the level | none |
| S9 | b | Flooded pillar hall, too large for any pool: tiled columns on the 3 m grid standing in the water, light from odd angles, one side dark (15 §2, §4) | none |
| S10 | b | A stair running down into dark water to a gate under the water line, an oversized intake grate beside it (15 §4, D6 kept) | none |

About 25 stills: one contact sheet per direction, then one sheet of Red's picks. Red, 10-31 ~14:15, widened the research ("还包括 后室概念里的泳池房间应该是什么样子的"): S9, S10 and the S5/S6 additions come from `15_poolrooms.md`.

## 3. How

- Greybox stills in a private clone under `W` (`tools/mkclone.sh`, a 6.6 Library from `proj_ref66` with `cp -Rc`, `tools/install_tools.py`). Renders Red asked for are deliverables, not tests (kickoff §3). Nothing makes a sound (rule 24).
- No shared rendering code is edited: `FrontRoomsSurface.shader`, `RenderSetup`, `RoomStream` and the lamp kit are 游戏视觉's. Water is the probe-only surface (P4 comes later).
- Palettes start from D8's rows (B: white pool, mint band, quarry-red deck, beige lockers; C: white pool, teal band, off-white slip tile, grey lockers) plus 游戏视觉's options sheet if it exists.
- Guard: rectangles on the 3 m grid; no arches, domes or skylit vaults; nothing taken from any creator's pool images, POOLS, Kane Parsons or the two 2026 films (`zone_lamps/10_pool.md` §6).
- Asked 游戏视觉 (`W/workspace_repo/remote/bathroom/OUTBOX.md`, card bathroom-101): which lookdev path and scene made its earlier options sheets, whether its lamp lean may stand in, and whether the D8 sheet folds into these stills.

## 4. Cost

6–10 PAH, inside the new estimate (`30_estimate.md` §5, row "D1 all").

## 5. Delivered (K, 2026-10-31 15:1x)

- **24 stills** (1920×1080, desktop High, the game's FOV 76°): `W/bathroom_dir/renders/pooldir/all/S*.jpg`. Log `W/bathroom_dir/logs/pooldir_all.log` ("FINISHED fail=0").
- **Four contact sheets in Shot format, for Red:** `W/bathroom_dir/sheets/A1_club_palettes.jpg` (S1–S3 × palettes W, B, C), `A2_hall_tile_line.jpg` (tile to 2.4 m / 1.2 m / ceiling), `C_turn.jpg` (S4 at the door / one room later; S5 slow / fast), `B_pool_level.jpg` (S6 white / pale blue / mint band / arched; S7 lit / half dark; S8; S9; S10).
- **How it was made:** clone `W/proj_pooldir` with game visual's pool STAND-IN tools (`FrontRoomsZoneLampsShots.cs` + `Props/ZoneLamps` + `Props/LampOptions` copied from `W/proj_zonelamps`), four clone-only edits to that file (`partial`, output root, the `pooldir` route, `FloorY` reading -1.07/-1.37 for D6) and the new `FrontRoomsZoneLampsShots.PoolDir.cs` (source `W/bathroom_dir/src/`). Re-run: the ledger job `pooldir_stills` has the command.
- **Honest limits:** greybox walls have no thickness (doorways show no jambs); signs and depth marks are blank panels; bullnosed corners were not modelled (only the round-headed doors); the stand-in uses the map's warm ambient, so white tile reads cream (the level's lights are 1.7× and cooler to push back); room water is the probe-only surface, so ankle water reads mainly as sparkle; the stair's darkness is faked by darker tile tints, not by water absorption (P4 comes later); lamps are game visual's lean (P1-A, P2-A, P3-B, P4-A, P6-A/B) as placeholders until Red picks in `zone_lamps/30_for_red.md`.
- **Fixed before delivery:** the first pass rendered the P1-A and P2-A lamps magenta (their prefabs use `LampOptions/Materials/LampOpt_Trim.mat`, not yet copied) and S7's camera stood behind the guard chair.
