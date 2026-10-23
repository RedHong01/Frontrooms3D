# NOTE 2026-10-22 ~15:4x · The first room: what the placard (sheet A) needs (平面视觉)

The evacuation plan is the tutorial (`research/placard/50_tutorial_placard.md`).
- Sheet A is a route diagram: `TYPICAL FLOOR · NOT TO SCALE`, plan-up = the wall it hangs on.
- With the station as the first room (Red ~15:20 in the 游戏视觉 chat; 叙事 v1.2 §9, `NOTE_1022_first_room_narrative.md` §4), the Floor 1 sheet changes:
  - `YOU ARE HERE` sits at the desk, at the same spot as the legend's `SECURITY DESK (KEY)`;
  - the route becomes a short line to `STAIR A` · `EXIT`;
  - the mount moves inside the room, by the desk.
- The four lines, line 4 `KEY AT SECURITY DESK.`, `NO REENTRY`, `LEVEL 0`, the warden line and `PRINTED 11/89` all stay.
- **The LEVEL B1 stair copies are not affected.**

## What any option must guarantee, or the sheet cannot tell the truth

1. **A mount wall.**
   - One clear wall segment in the station room, at least 0.55 m wide × 0.40 m tall between 1.20 and 1.90 m (the sheet is 432 × 279 mm plus its frame; its centre sits at 1.56 m as today).
   - Within 1.5 m of the desk, facing the room's way in.
   - Not behind the desk chair, not on glass or a door leaf, not under a shelf or a set.
   - The EXAMINE aim box can be reached from the player's side of the desk, inside the READ gate (≤ 1.6 m; `30_crosshair_grammar.md`).
2. **The same mount wall on every seed.**
   - Plan-up is the mount wall (ISO 23601 §5 j / §7.3 a; Levine 1982), so the mount has to be the same wall of the room on every seed (for example the desk's back wall).
   - Otherwise the drawn plan comes out mirrored for some seeds.
3. **The way out points the same way on every seed.**
   - The stair EXIT door's side, seen from the mount (left, right or ahead), has to be fixed by the stamp. The distance is free (3–6 cells; the sheet is not to scale).
   - If the map cannot fix that side, Floor 1 needs the run-time sheet instead: sheet C's grammar, Phase 2, which needs 关卡设计's C1 floor-start API and 游戏视觉's C2 run-time drawing. **The synthesis should say which.**
4. **The start door is real.**
   - `NO REENTRY` is drawn on the station-room wall where that door really is.
   - If the title room IS the station (系统设计's point 4), the door you entered by is the one that locks behind you, and it is drawn that way.
5. **Light.**
   - The mount wall is lit by a steady lamp (系统设计's point 6: at least 2 steady lamps).
   - The print reads white on red at 5.3 : 1. The red block is the 3 m cue and must still carry in the room's darkest steady state.
   - No Relay arrival point within sight of the mount.
6. **Glow.** The placard keeps its phosphor glow for a power failure, with the glow mask unchanged. The station room's own walls never glow (Red, Q4 = B).
7. **Line 4 stays** even though the key is in the same room: a building's rule sheet holds on every floor (叙事).

## What 平面视觉 delivers after Red's pick

Within about an hour of receiving the geometry (desk, mount wall, start door and stair door, in cells):
- sheet A and Phase 0 for Floor 1 redrawn: the route, `YOU ARE HERE` at the desk, the desk legend at the same spot;
- the hand-off folders and `examine.json` regenerated;
- Figma TP01 / TP06 updated, with a VL in the Shot format;
- an in-game check against the pack (`~/FrontRoomsVisualWork/placard_tutorial/ingame_check.py`).

Until then the Floor 1 folders (`sheetA/`, `sheetA_phase0/`) hold the old route and should not be packed as final. `sheetA_B1/` and `sheetA_phase0_B1/` are unchanged.

## Update 16:0x: send the geometry in this form (平面视觉)

The redraw tool is built (`research/placard/50_tutorial_placard.md` §16). Give the room in metres, room-local: x east, z north, with the origin at the SW corner of the wall centrelines. `at` runs west to east on the N and S walls, and south to north on the E and W walls.

```json
{"room_m": [9, 6], "mount": {"wall": "N", "at": 5.4},
 "start_door": {"wall": "S", "at": 3.0, "w": 2.4},
 "openings": [{"id": "E1", "wall": "W", "at": 4.636, "w": 1.77, "exit": true},
              {"id": "E2", "wall": "S", "at": 7.29, "w": 1.29}],
 "desk": [6.16, 0.98, 6.92, 3.63]}
```

- `mount` is the placard's centre on its wall. That wall becomes plan-up.
- `exit: true` marks the opening on the EXIT sight line.
- `desk` is the counter's footprint `[x0, z0, x1, z1]`.
- The values above are a **draft**: the W′ rev 4 blockout with an assumed start door and mount. Please replace them.

Cells are fine too; I convert them. The distance to the EXIT is not needed: the walk is drawn broken.
