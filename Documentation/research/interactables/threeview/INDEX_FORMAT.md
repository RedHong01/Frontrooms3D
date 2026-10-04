# index.json format for 平面视觉's K-sheets (agreed 2026-10-03 18:3x)

One entry per kit. Every field is required:

| Field | Meaning |
|---|---|
| `kit` | kit name, e.g. `Kit_DoorLeaf_Veneer` |
| `title` | the sheet title (Bayon) |
| `lede` | one or two lines that fit 576 px wide (Source Serif) |
| `dims_mm` | `{ "w": …, "d": …, "h": … }` from the sidecar bounds, in mm |
| `era` | `{ "from": year, "to": year, "label": "Timeless" \| "Period" }`; the sheet draws a 1985–93 band and a 1990 tick |
| `materials` | `[{ "name": "Prop_…", "hex": "#rrggbb" }]`: albedo average × `_BaseColor`, with the tint converted from sRGB to linear first (`Tools/three_view/README.md`) |
| `variants` | variant kit names shown as small images under the hero panel, e.g. tag colours, brass vs chrome |
| `images` | `{ "top": path, "front": path, "side": path, "persp": path }`: the three-view PNGs plus the hero |

Layout on the sheet (平面视觉 builds it like K45):
- third-angle top, front and side with a 1 m scale bar;
- a 576 × 432 hero panel with variant thumbnails;
- an era panel and material swatches.

Layer names are `img:<Kit>_<top|front|side|persp>`, so later re-renders replace images through `upload_assets` with the node ids.

Placement: K46 and K47 go in the two empty cells of PROP KIT 2324:852 at (4200, 13800) and (6240, 13800); K48 onward go in a new section "PROP KIT · THREE-VIEW + ERA (CONT.)", which 平面视觉 places.
