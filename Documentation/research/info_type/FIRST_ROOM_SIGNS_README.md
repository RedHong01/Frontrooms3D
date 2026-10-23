# First-room signs: hand-off to 游戏视觉

> Repo copy of `~/FrontRoomsVisualWork/info_type/stair_signs/out/first_room/README.md` (2026-10-22 16:4x, 平面视觉 · 信息设计). Relative paths below are relative to that W folder, where the generator `stair_signs.swift` and the art live. Edit the W file, then re-copy.

From 平面视觉 · 信息设计, 2026-10-22.
- Strings: 叙事设计, `narrative/40_floors_exit_tutorial_strings.md` v1.2 §9, after Red's "按推荐，第一个房间选方案 1" (叙事设计 chat, 15:2x).
- Faces and lettering: this line. 游戏视觉 mounts them.
- Generator: `../../stair_signs.swift`, the same system as the stair plates.

To re-bake:
```
cd ~/FrontRoomsVisualWork/info_type/stair_signs && swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 firstroom
```

## 1. `business_hours`: the door plate

| | |
|---|---|
| Text | `THIS DOOR TO REMAIN` / `UNLOCKED DURING` / `BUSINESS HOURS` |
| Rule | UBC 1991 §3304(c) exception 1 (叙事's source P01): a durable sign on or beside the main exit door, letters at least 1 in high on a contrasting background |
| Size | **18 × 6 in (457.2 × 152.4 mm)**, corner radius 3.2 mm |
| Type | TeX Gyre Heros Bold, caps 1 in (25.4 mm), tracking +0.02 em, three lines centred, 0.5 in between lines. Ink widths 410.6 / 353.6 / 322.7 mm |
| Colour | Pale, like the other door notices: white cap (236, 234, 228), letters engraved through to the black core (29, 28, 26) |
| Build | Two-ply engraved plastic, 1.6 mm thick. Letters 0.3 mm deep, 45° bevel 1.0 mm showing the black core, no screws (adhesive on a door leaf) |
| Mount | **Moved 2026-10-22 (FD-b layout, A26 B3c; 叙事 40 §9 11a6cf2e):** on the latch-side liner wall beside door D, room side, plate centre 1525 mm above the floor. Its door-side edge sits 50–100 mm from the frame's outer edge, so it reads as the door's notice (UBC: "on or adjacent to" the door). It needs at least 560 mm of clear, flat wall from the frame's edge (457 mm plate plus the gaps); nothing tall within 50 mm of its far edge. Fixed with adhesive, like the door plates. The art is unchanged. If the liner wall is narrower than 560 mm, send me its width: the code's 1 in letters can't fit a narrower plate, so we would look at another spot instead |

**Size approved by Red: "457 毫米可以，就按这个尺寸" (信息设计 chat, 2026-10-22 15:33).** Why 457 mm and not your 305 mm limit: The code's 1-inch letters make the longest line about 411 mm wide in Bold. At 305 mm the text breaks into four or five choppy lines, or the letters drop below the code minimum. On a standard 914 mm (36 in) leaf, the 457 mm plate leaves about 228 mm on each side. The size is final; no fallback is needed.

Files:
- `business_hours_albedo.png`;
- `business_hours_height.png`: grey 255 = 3.2 mm off the leaf. The plate face is 1.6 mm (128) and the letters 1.3 mm (≈104); the bevel is stepped, then blurred 0.3 mm;
- `business_hours_mask.png`: white = the engraved letters, smoothness about 0.25;
- all at 4 px/mm, 1832 × 612 px (rounded up to a multiple of 4).

## 2. `security_letters`: the desk-front letters

| | |
|---|---|
| Text | `SECURITY` (one name for one thing: the placard says SECURITY DESK; never LOBBY or STATION) |
| Choice | Individual flat-cut letters, not a plate. Pin-mounted metal letters in a Helvetica-family face were the 1980s–90s lobby and reception look, and they read better under the set's light |
| Type | TeX Gyre Heros Bold, caps 4 in (101.6 mm), tracking +0.06 em. Ink 758.4 × 104.9 mm (under the 900 × 120 mm limit). The baseline is 1.67 mm above the ink bottom, because S, C and U overshoot |
| Material | Aluminium 1/4 in (6.35 mm) thick, satin brushed finish with a horizontal grain, square cut edges |
| Mount | Pin-mounted 1/2 in (12.7 mm) off the fascia, so each letter casts its own small shadow. Centred on the counter's front fascia, facing the entry door, baseline about 750 mm above the floor (cap top about 852 mm on a 1.07 m fascia) |

Files:
- `security_letters.svg`: one path per letter (`L1_S` … `L8_Y`), in mm, y down, viewBox = the word's ink box. Extrude each path 6.35 mm;
- `security_letters_mask.png`: white = metal, 4 px/mm, 3036 × 420 px; for a decal stand-in or a check;
- `layout.json`: every size above, plus each letter's ink box in mm from the word's left ink edge and the baseline.

## Check

Once they're mounted in the first-room blockout, send me lit frames:
- the plate at about 1 m and 3 m;
- the letters from the entry door.

I check legibility and the metal-on-fascia contrast. If a mount number changes, send the delta and I'll re-bake.
