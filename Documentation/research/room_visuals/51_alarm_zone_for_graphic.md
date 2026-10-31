# 51 · ALARM ZONE card: the ask to 平面视觉 · 信息设计

From 游戏视觉 (game visual), 2026-10-31. Research and the kit: `50_alarm_zone_tell.md`. Type, lettering and stamps are yours. 游戏视觉 built the card's slot and a **STAND-IN** face so the kit can land inert. Nothing here is final lettering.

## 1. What the card is

Every trigger room (RELAY §6, row 10; Run! rooms first, then every other trigger kind) carries one signature at every entrance: a ceiling detector on a red-banded base inside, and this card outside, beside the opening. The card is the read **before you commit**: "this room calls the panel". It must be readable in a dark B corridor and in a lit Office (C39), and it must never read as EXIT (C41: no EXIT sign in a Run! room, EXIT stays the floor's one way down).

The object is a 1990 internally lit warning sign: a fire-red pressed-steel box with a translucent acrylic face lit from inside. Precedents: lit legends on annunciator windows, X-ray room warning signs at the entrance (`50` §2.2).

## 2. Size and mounting (fixed by the kit; tell us if you need another size)

| Item | Value |
|---|---|
| Visible face (your art) | **254 × 152 mm (10 × 6 in)**, aspect 1.671 : 1 |
| Housing | 286 × 184 × 51 mm, fire-red enamel #B01E18; 16 mm flange around the face; four pan screws on the flange at (±135, ±83) mm from the centre |
| Art file | **2032 × 1216 px (8 px/mm)**, sRGB PNG, no alpha. Any size of the same aspect works; keep ≥ 6 px/mm (the card holds at 0.3 m) |
| Mounting | Centre **1.524 m (60 in)** above the floor, on the wall beside the opening, facing the approach. Edge ≥ 150 mm from the opening (frame face 50.8 mm + 100 mm). Approacher's right by default; the latch side when there is one (ADAAG 1991 §4.30.6) |
| Viewing | First read at 5–6 m, seen from 2 m outside the entrance, close-up possible at 0.3 m. At 1080p / 76° the stand-in's 41.3 mm caps are 16.8 px at 2 m, 6.7 px at 5 m, 3.4 px at 10 m. **If you want it read from further, ask for a bigger face** (12 × 8 in or 14 × 7 in are a one-line change to the kit) |

## 3. Light: two maps, one emission level

| Map | File (replace in place: same paths, same .meta GUIDs) | What it is |
|---|---|---|
| Albedo | `Assets/Resources/Print/AlarmTell/alarm_zone_card_albedo.png` | The face as seen unlit (in a lit room this is most of what reads) |
| Glow | `Assets/Resources/Print/AlarmTell/alarm_zone_card_glow.png` | What the internal lamp lights: white = full, black = none |

- **Emission level (target, real-world luminance):**
  - legend **40 cd/m²**;
  - ground **10 cd/m²** (red);
  - contrast (legend − ground) / legend ≥ 0.7.
  An EXIT sign is about 100 cd/m² (`exit_sign/10_spec.md`, S1/S9), so the card stays dimmer than every EXIT on the floor. In game units the material's emission gain is 1.2 on the glow map. The joint test calibrates it to "legend 0.6–1.0× a lit Office wall" (the exit-sign method). Please give the ratio between legend and ground in your glow map, not an absolute value.
- **Dark-room rule:** in a dark corridor only the glow map reads. The legend must stand alone there, without the box's outline.
- **Lit-room rule:** in a lit Office the albedo reads. The red ground and the legend must contrast unlit too (≥ 0.5, UL 924's rule for signs, `exit_sign/SOURCES.md` S4).
- No realtime light comes off the card on any platform.

## 4. Ground and ink (the brief's defaults; yours to change inside the rules)

- **Ground:** the red family, **#B01E18** (the same red as the box, B's fire gear and the detector's band). Red = ALARM ZONE and nothing else in the game (`station/30_look.md` §2). Lit, the ground glows a dim red.
- **Ink:** warm white legend (stand-in #EEE9DE unlit, #FFFBF2 in the glow map): **reverse, white on red**.
- **Never:**
  - red letters on a pale or dark ground (that is the EXIT look);
  - green or pale yellow-green anywhere (the glow ink, W6);
  - amber (TROUBLE / sanctuary, RELAY §10);
  - an arrow;
  - the word EXIT.

## 5. Era rules (lock 1990, ≤ 1993; no brands)

- A US commercial building, 1990. Legends were all caps in a grotesque. Signs and placards in our main use **TeX Gyre Heros Bold** (the stair signs, the evacuation plan), which is why the stand-in uses it. Bayon is the title face, not a building sign.
- No pictograms. ISO 7010 is 2003, and a 1990 US fire-alarm legend is words.
- No manufacturer name, logo, model number or listing mark. A small maker's line is period-true but would be a brand.
- Permitted period marks, if you want them: a zone number ("ZONE 3"), a device line in a lettering-tape style (Kroy c. 1987 or Dymo embossed tape), screws, a slight scuff. Strings come from 叙事设计.
- The face is lit acrylic, so the legend is printed or screened, not engraved. An engraved laminate plate is the alternative if you prefer an unlit card with a separate lamp; tell us and we rebuild the kit.

## 6. What the stand-in is

- "ALARM" / "ZONE", two lines, TeX Gyre Heros Bold, 1.625 in (41.3 mm) caps, centred on the ink, baseline-to-baseline 1.6 caps, 18 mm side margins on ALARM, no keyline.
- A 3.5 mm "STAND-IN" mark bottom right in a lighter red, albedo only, so it cannot ship unnoticed.
- Source: `Tools/lookdev/gen_alarm_card_standin.py` (Pillow, deterministic). Preview: `W/alarm_tell/tex/card_standin_preview.png`.
- Code flag: `FrontRoomsAlarmTell.CardFaceIsStandIn = true`. Set it false when your art lands.

## 7. What we need back

1. The two PNGs at the paths in §3, or a size change request first (§2).
2. The wording, signed by 叙事设计 (stand-in: ALARM / ZONE). One line or two, with or without a zone number.
3. The glow ratio legend : ground.
4. A yes or no on the optional device line (§5). If yes, its strings come from 叙事设计.

Deadline: inert kit due in main by 11-03; Run! playable earliest 11-04 (RELAY §6, row 10). The stand-in carries until then.
