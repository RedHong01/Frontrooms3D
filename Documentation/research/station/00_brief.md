# 00 · The security desk (the tutorial "station"): brief

**Status: research started 2026-10-22 by 叙事设计, jointly with 游戏视觉.**

**Red's ask, in the 叙事设计 chat, 2026-10-22:** "值班台应该也有一个布局，从conceptdesign- environmental design的维度来说。我需要你开始research在咱们的这个时代和文化背景下，这个值班台的concept 和environmental design应该是什么样的，并且作为一个关卡其布局应该是什么样子的 / 这部分的工作应该是游戏视觉和你一起协作"

In short: the station needs a layout. Research what it should be, as concept and as environmental design, for our era and culture, and how it lays out as a level. 叙事 and 游戏视觉 work on it together.

## What is already decided (inputs, not re-opened)

- **Tutorial T1 = A** (Red, 2026-10-22, 教程设计 chat): each floor's EXIT is locked. Its one floor key hangs at the station, the only room whose TV still plays. Taking the key kills the TV and sends the hunter there (`research/tutorial/10_proposal.md` §3–§4, §7; requests M3–M8, V1–V5 in `20_requests.md`).
- **Player-facing name:** `SECURITY DESK` (叙事, `research/narrative/40_floors_exit_tutorial_strings.md` §3). The fiction: the guard is gone, the desk set still plays WTCZ-TV 61, and the key rack is a device on the alarm panel.
- **Module rules** (`LEVEL_MODULE_SPEC.md` §2–§9; `20_requests.md` M4, V1):
  - 3 m cells; Wall / Arch / Open edges (modules author no doors);
  - one KeySpot (on the rack), no RelayEntry;
  - never Tall, and reachable without glass or keys;
  - one per floor, placed by code, never in the random pool;
  - Floor 1: 2–6 cells past the EXIT;
  - Floor 2+: 10–20 cells from arrival, off the EXIT route, ≥ 6 cells from it;
  - TV side Dim/Off, one lit lamp by the entrance.
- **Beacon** (M5, S1–S5, V2–V3): sound through the doorways toward the TV, and a cold moving TV light about 6 m.
- **Standing rules:**
  - era 1990 (≤ 1993), no real brands;
  - dark is never cover;
  - never a radar;
  - photosafety (≤ 3 changes per second);
  - "visuals at the highest spec";
  - research leads with real sources plus `SOURCES.md`, and downloads need Red's OK.

## Questions this research answers

1. **Concept:** what a 1990 US security or attendant desk in an office or commercial building was. Who sat there, what they did, what was on and around the desk, and what the codes asked of it.
2. **Environmental design:** what the desk looks like, and what the absent guard left behind (the story told without words). How Level 0's paper-and-carpet world and the Office theme each host it.
3. **Level layout:**
   - how the room reads from the approach (the beacon's light and sound, the reveal);
   - where the TV, the key rack and the desk sit relative to the entrances;
   - two ways out (it is sent there after the take);
   - what the player sees at the take and on the way back to the EXIT.

## Split (proposed to 游戏视觉 2026-10-22; adjust on its reply)

| Piece | Owner | File |
|---|---|---|
| Brief | joint | `00_brief.md` |
| Real-world research (practice, equipment, codes, culture) | 叙事 | `01_research.md` |
| Film / TV / game references, IP distance | 叙事 | `02_refs.md` |
| Concept directions A/B/C and a recommendation | joint (游戏视觉 leads the visual side) | `10_directions.md` |
| Level layout logic and cell plans | 叙事, with 关卡设计's constraints | `20_layout.md` |
| Look: kit gap list, materials, light plan, blockout renders | 游戏视觉 | `30_look.md` |
| Sources ledger; media to download (after Red's OK) | 叙事 (+ 游戏视觉 for its renders) | `SOURCES.md`, `media_candidates.md` |
| Figma proposal section | 游戏视觉 lays it out; 叙事 supplies research frames | (node on creation) |

## Kits in main that already fit (from `Assets/Resources/Props/Models/*.json`)

- **Screens:** `Kit_CRTTV`, `Kit_CRTMonitor`, `Kit_PCDesktop`, `Kit_Keyboard`.
- **Keys:** `Kit_KeyCabinet`, `Kit_KeyHookBoard`, `Kit_KeyHook`, `Kit_KeyRing`, `Kit_KeyTag_*`.
- **Desk and seating:** `Kit_DeskPhone`, `Kit_OfficeDesk`, `Kit_DeskPedestal`, `Kit_TaskChair`, `Kit_LadderChair`, `Kit_Sofa3*`, `Kit_ClubChair*`, `Kit_SideTableTurned`.
- **Room furniture and light:** `Kit_TableLampPleated`, `Kit_Torchiere`, `Kit_WallClock`, `Kit_Credenza`, `Kit_Hutch`, `Kit_RollingCabinet`, `Kit_FilingCabinet`.
- **Clutter and fixtures:** `Kit_PaperStack`, `Kit_Binders`, `Kit_TrashBin`, `Kit_Urn`, `Kit_WaterCooler`, `Kit_VendingMachine`, `Kit_InteriorWindow`, `Kit_MiniBlind_*`, `Kit_DoorSign`, `Kit_HatStand`.
- **Not in the kit:** a reception counter or guard console, a TV cart or wall bracket, the fire-alarm annunciator, the sign-in log, a watchclock station, a radio charger. That gap list is 游戏视觉's call in `30_look.md`.
