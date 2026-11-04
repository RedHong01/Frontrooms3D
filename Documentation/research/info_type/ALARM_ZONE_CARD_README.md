# Alarm family faces: hand-off to 游戏视觉

Two faces in the fire red of the alarm family: the ALARM ZONE card (below) and the C16 alarm door placard (last section, 2026-11-03).

## ALARM ZONE card

From 平面视觉 · 信息设计, 2026-10-31. Spec: 游戏视觉's `research/room_visuals/51_alarm_zone_for_graphic.md`. Generator: `~/FrontRoomsVisualWork/info_type/stair_signs/stair_signs.swift`, the same as the stair and first-room signs. To re-bake:
```
swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 alarmzone
```
Then strip the alpha (PIL `convert('RGB')`).

### Files (replace your stand-ins in place: same names, same .meta GUIDs)

| File | What |
|---|---|
| `alarm_zone_card_albedo.png` | 2032 × 1216 px (8 px/mm, 254 × 152 mm), sRGB, no alpha. Ground #B01E18, legend #EEE9DE |
| `alarm_zone_card_glow.png` | Same size. The emitted colour: legend #FFFBF2 at full, ground #FF2F28 |
| `layout.json` | Cap, pitch, ink widths |

`Assets/Resources/Print/AlarmTell/` is your path, so you copy the files in and set `FrontRoomsAlarmTell.CardFaceIsStandIn = false`. Do that once 叙事设计 has signed the wording (below).

### Answers to §7

1. **Size: keep 10 × 6 in.** No kit change.
2. **Wording:** `ALARM` / `ZONE`, two lines, no zone number. This is the stand-in's wording, kept. It waits for 叙事设计's signature, and a re-bake takes a minute if they change it.
3. **Glow ratio, legend : ground = 4.1 : 1** in luminance (target 40 : 10 cd/m²). Contrast (L−G)/L = 0.76, so ≥ 0.7. Unlit (albedo) contrast is 0.87, so ≥ 0.5 (UL 924).
4. **Device line: no.** One fixed texture for every trigger room, and no string to sign or localize.

### Type

- TeX Gyre Heros Bold, caps **42.9 mm** (the largest that fits; the stand-in's were 41.3).
- Tracking +0.02 em.
- Baseline pitch 1.5 caps; lines centred on the ink.
- `ALARM` sets 217.7 mm in the 218 mm measure (18 mm side margins). The block is centred, about 22 mm top and bottom.
- No keyline: in the dark the glowing red field is the card, and the legend stands alone on it (§3's dark-room rule).

### Reading (1080p, vFOV 76; corrected 2026-11-03)

| Platform | Reads to (18.6′) | With effort (14′) |
|---|---|---|
| Desktop | 2.5 m (12 px) | 3.3 m |
| iPad 11-inch | 2.0 m | 2.6 m |
| Phone (iPhone 16 Pro) | 1.1 m | 1.5 m |

- **Corrected 2026-11-03.** The first numbers here (2.9 / 2.3 / 1.3 m) were scaled from §2's, which take 1080 px over 76° as 814 px per radian. At the screen's centre a 76° camera gives 691 px per metre at 1 m, so they were 18 % long. The same model now serves every sign of this line (`W/info_type/stair_signs/placard_read.py`, `reach`).
- So the words read at the 2 m decision point on desktop, and just about on an iPad. A phone reads them on approach, or in the 0.3 m close-up.
- No face in this family reads words at 5–6 m: that needs about 80 mm caps. There the read is the red ground, which means ALARM ZONE and nothing else (`station/30_look.md` §2).
- If the joint test shows phone players missing it, the 14 × 7 in face (52 mm caps) moves the phone read to about 1.4 m.

### Era (§5)

- A printed or screened legend on lit acrylic, not engraved.
- All caps in a grotesque.
- No pictogram, brand, maker's line, arrow or EXIT.
- No green, amber, or red letters on a pale ground. ≤ 1993.

## Alarm door placard (C16, Relay v2 V7b)

From 平面视觉 · 信息设计, 2026-11-03 (account G, local_4aa27f23).
- **Ask:** 游戏视觉, `briefs/graphic_info.md`, 2026-11-01 20:55. They want the face for slot `Alarm_DoorPlacard` on `Kit_AlarmExit_Placard`, the cap size, and whether the 40 mm pad gets a second legend.
- **Words:** `ALARM WILL SOUND`, one line, never EXIT. Signed by 叙事设计 (`research/narrative/90_alarm_devices_and_door_strings_signoff.md` §4).
- **Slot spec:** `research/door_hardware/v7b_alarm_door/10_research.md` §3. A 15 × 3 in plate of 1/16 in screen-printed aluminium, R 4 mm corners, on both door faces at 1.42 m, never mirrored.

To re-bake (RGB, no alpha; `alarmdoor_options` re-bakes the compared faces):
```
swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 alarmdoor
```

### File and landing

| File | What |
|---|---|
| `out/alarm_door/alarm_door_placard_albedo.png` | 2048 × 408 px, sRGB, no alpha, sha1 `516991d3`. Ground #B01E18 (as `Alarm_BandRed`), legend and keyline #EEE9DE. Not lit, so no glow map |
| `out/alarm_door/layout.json` | Cap, tracking, ink width, margins, keyline |
| `out/alarm_door/reach.json` | Reading distances per platform |

`Assets/Resources/Print/AlarmDoor/` is your path. The package `W/apply/alarm_door_placard_face.sh` replaces the stand-in in place: same path, its .meta (`6137c8bf`, GUID `ebd5d89e…`) kept. Dry run OK against main on 2026-11-03 16:59, and a scratch rehearsal gave MANIFEST OK. Then set `FrontRoomsAlarmDoorLook.PlacardFaceIsStandIn = false`: the words are signed.

### Answers

1. **Face and size: TeX Gyre Heros Bold Condensed, caps 27.45 mm (1.08 in), tracking +0.04 em.** One line, centred on its ink, with the cap band on the plate's centre line.
   - Heros Bold at the door plates' 1 in tier does not fit the 15 in plate with margins. The stair-sign rule (`W/info_type/stair_signs/SPEC.md` §3) then sets the line in Bold Condensed. The alarm family takes the largest cap that fits, as the ALARM ZONE card does; here 0.5 cap stays clear of the keyline.
   - Against Bold in the same plate (24.0 mm), the caps are 14 % taller and the words read 14 % further (VL486). The stand-in had Bold at 25.0 mm, 12 mm margins and no keyline.
2. **A printed keyline: yes.** 1/16 in (1.6 mm) wide, its outer edge 1/8 in (3.2 mm) in from the plate's edge, outer radius 1/8 in, in the legend's ink.
   - The red plate (luminance 0.10) and the walnut door (`DoorVeneer_A`, mean #7B573E, 0.11) are equally light. Without the line, the plate's edge reads by hue alone, and less for a player with red-weak colour vision.
   - A white border on red fire and safety signs is the usual US form [memory, not source-read].
3. **A second legend on the pad: no.**
   - The pad is at 1.00 m (1.10 m on locked doors) and the eye at 1.62 m. Looking ahead, the pad leaves the frame inside 0.8 m.
   - A legend that fits its 40 mm face has about 16 mm caps, which read only from 0.9 m on desktop. The words would be legible only between about 0.92 and 0.79 m of the approach, at the bottom edge of the screen.
   - The placard at eye level carries the words, and the HUD shows `ALARMED` on aim. Period sources do print legends on bars (S1, S4 in `door_hardware/v7b_alarm_door/SOURCES.md`); this choice is for the read, not the era.

### Drawn on the kit's face, not the plate

- The decal's UVs run 0..1 across the face's own bounds (`Tools/Blender/frontrooms_kit/kitlib.py`, `_uv_decal`).
- The LOD0 face is the plate less 0.6 mm all round: 379.8 × 74.8 mm. The LOD1/2 quad is 380 × 75.
- The art is laid out in plate millimetres and drawn at 5.392 px/mm across and 5.455 px/mm up. The face's aspect differs from 2048 : 408 by about 1 %, and this keeps the letters unsquashed.
- Re-bake if the kit's face changes size.

### Reading (VL486; VL485 shows the faces)

Simulated as the GPU samples it: mips built in linear light, BC1 per level, trilinear; flat albedo, no light.

| Platform | Reads to (18.6′) | With effort (14′) |
|---|---|---|
| Desktop 1080p | 1.58 m | 2.11 m |
| iPad 11-inch | 1.27 m | 1.69 m |
| Phone (iPhone 16 Pro) | 0.72 m | 0.96 m |

- The player meets the door at 0.3 m (`PlayerRadius`), so the words read before contact on every platform.
- At the 2 m approach, a desktop reads them with effort. A phone reads the HUD's `ALARMED`, shown on aim within 2.4 m.
- Not checked: the lit in-game frame (游戏视觉, with the combined test), BC1 as Unity encodes it (the simulation approximates it), and WebGL (it never dresses map doors).

### Era

- A screen-printed legend on painted aluminium, all caps in a grotesque, a printed border.
- No pictogram, brand, maker's line, arrow or EXIT.
- Detex sells a separate 3-1/2 × 11 in "Emergency Exit Only / Alarm Will Sound" sign for its devices (EA-561; current listings, no era claim) [search, 2026-11-03: https://www.rhsecurityhardware.com/sign-emergency-exit-only-alarm-will-sound-det-ea-561/].
- The pre-1990 wording and layout stay UNVERIFIED (`90` §4).
