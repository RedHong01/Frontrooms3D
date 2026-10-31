# ALARM ZONE card: hand-off to 游戏视觉

From 平面视觉 · 信息设计, 2026-10-31. Spec: 游戏视觉's `research/room_visuals/51_alarm_zone_for_graphic.md`. Generator: `~/FrontRoomsVisualWork/info_type/stair_signs/stair_signs.swift`, the same as the stair and first-room signs. To re-bake:
```
swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 alarmzone
```
Then strip the alpha (PIL `convert('RGB')`).

## Files (replace your stand-ins in place: same names, same .meta GUIDs)

| File | What |
|---|---|
| `alarm_zone_card_albedo.png` | 2032 × 1216 px (8 px/mm, 254 × 152 mm), sRGB, no alpha. Ground #B01E18, legend #EEE9DE |
| `alarm_zone_card_glow.png` | Same size. The emitted colour: legend #FFFBF2 at full, ground #FF2F28 |
| `layout.json` | Cap, pitch, ink widths |

`Assets/Resources/Print/AlarmTell/` is your path, so you copy the files in and set `FrontRoomsAlarmTell.CardFaceIsStandIn = false`. Do that once 叙事设计 has signed the wording (below).

## Answers to §7

1. **Size: keep 10 × 6 in.** No kit change.
2. **Wording:** `ALARM` / `ZONE`, two lines, no zone number. This is the stand-in's wording, kept. It waits for 叙事设计's signature, and a re-bake takes a minute if they change it.
3. **Glow ratio, legend : ground = 4.1 : 1** in luminance (target 40 : 10 cd/m²). Contrast (L−G)/L = 0.76, so ≥ 0.7. Unlit (albedo) contrast is 0.87, so ≥ 0.5 (UL 924).
4. **Device line: no.** One fixed texture for every trigger room, and no string to sign or localize.

## Type

- TeX Gyre Heros Bold, caps **42.9 mm** (the largest that fits; the stand-in's were 41.3).
- Tracking +0.02 em.
- Baseline pitch 1.5 caps; lines centred on the ink.
- `ALARM` sets 217.7 mm in the 218 mm measure (18 mm side margins). The block is centred, about 22 mm top and bottom.
- No keyline: in the dark the glowing red field is the card, and the legend stands alone on it (§3's dark-room rule).

## Reading (at 1080p / 76°, scaled from §2's numbers)

| Platform | Reads to |
|---|---|
| Desktop | about 2.9 m (12 px) |
| iPad | about 2.3 m |
| Phone | about 1.3 m, at the 18.6′ glance rule |

- So the words read at the 2 m decision point on desktop and iPad. A phone reads them on approach, or in the 0.3 m close-up.
- No face in this family reads words at 5–6 m: that needs about 80 mm caps. There the read is the red ground, which means ALARM ZONE and nothing else (`station/30_look.md` §2).
- If the joint test shows phone players missing it, the 14 × 7 in face (52 mm caps) moves the phone read to about 1.6 m.

## Era (§5)

- A printed or screened legend on lit acrylic, not engraved.
- All caps in a grotesque.
- No pictogram, brand, maker's line, arrow or EXIT.
- No green, amber, or red letters on a pale ground. ≤ 1993.
