# Station paper: BS7 art (平面视觉, 2026-10-30)

Rebuild with `/usr/bin/python3 build_atlas.py <out dir>`. Faces: TeX Gyre Heros, Courier Prime, Rock Salt (Apache 2.0, baked into the texture).

| File | md5 | Into |
|---|---|---|
| `Prop_StationPaper_A.png` | 3f3b4424… | `Prop_StationPaper`, 4096², sRGB. 4 × 4 cells, in the order of `_station_common.py`'s `PAPER_CELLS`. Paper is white, so keep the material's `_BaseColor` #DCD8CC as the tint |
| `Prop_StationKeyPlate_A.png` | 6b3845eb… | `Prop_StationKeyPlate`, 2048 × 1024, sRGB, decal UVs over the 0.30 × 0.10 plate. It carries its own colours (cap #2B2925, core #E2DDCF), so set the material's `_BaseColor` to white |

- **Each cell is stretched into its square.** The kits map the whole cell onto the quad, so the art looks squashed in the atlas and right on the prop.
- **Dates:** the first row of the register and of the sign-out sheet, and the DAR's date field, are empty, because those dates follow the live clock (`research/station/01_research.md` §6). `date_slots.json` gives the three slots for a runtime date; the register and the sign-out sheet show the same date. Left empty they read as not filled in.
- **Plate type:** `STAIR KEY` cap 26 mm, the warning line cap 11 mm. On the 688 px/m reference (`research/inspect/10_design.md`) the warning reaches 12 px at about 0.63 m, so it reads up close and in EXAMINE, not from the counter. Line 1 reaches 12 px at about 1.5 m.
- **Matches the catalog:** the plate, the DAR lines and the register and sign-out headers are the strings in `FrontRoomsInspectCatalog.cs`.
- **Not in the 13 cells:** magazine covers, annunciator labels, `NO SMOKING`. The `STAIR A` tag lettering and its colour are also still open.
- **Signed by 叙事 (BS10), 2026-10-31:** the register rows, the sign-out rows and the phone list, with its seven changes (`research/narrative/80_form_and_station_paper_signoff.md` §3).
