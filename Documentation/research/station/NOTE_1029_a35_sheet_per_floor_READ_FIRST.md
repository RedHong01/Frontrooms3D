# READ FIRST: pick each floor's placard sheet from that floor's own state (A35), 2026-10-29 18:44

From 叙事设计 (account A, local_c919678b), 2026-10-29 18:5x, relayed by the 游戏视觉 session. Source of record: `research/narrative/40_floors_exit_tutorial_strings.md` v1.3 §3 (4cb71ce5).

**Why.** A35 is Red's A26-Q1 = b. It has been in main since 17:49, switched off along with the station. In a station run, the floors below LEVEL 0 have:
- no station;
- no key;
- no locked doors;
- an unlocked EXIT.

So a B floor must not hang a plan that shows a padlock and a security desk.

**Rule for packing / dressing at the A26 switch-on.** Choose the sheet per floor from *that floor's* state, never from the run's:

| Floor | Sheet folder (Tools/print/ink/art_from_graphic/placard/) | Line 4 |
|---|---|---|
| LEVEL 0 with the station | `sheetA_room/` (print md5 e11ba9c8…; the full sheet: desk, padlock) | KEY AT SECURITY DESK. |
| B1 and deeper, including the stairwell copy by the far door | `sheetA_phase0_B1/` (level = B2, … for deeper floors) | EXIT BY STAIR, ONE FLOOR DOWN. |
| A B floor that has its own station (M4, later) | the full sheet, for that floor | KEY AT SECURITY DESK. |

- The README's pairing "sheetA_B1/ when the stair lock + station are live" is superseded for B floors. 平面视觉 is updating the README.
- There are no new strings and no new art: both sheets are signed and rendered.
- A floor's "has a station" should come from the map's per-floor state. FrontRoomsStationPresence already reads FrontRoomsMapWorld.FloorHasStation by reflection; use the same kind of read.
- The EXAMINE transcript must match the sheet on the wall: each sheet folder carries its own examine.json.
