# Showcase scenes · 20 · Placard type: faces, sizes and colours (平面视觉)

- 给 Red：场景 3、4 的说明牌用游戏界面那套字（Bayon、IBM Plex Mono、思源黑体 Noto Sans SC），深色牌面、纸白字、界面黄色标出“要你决定”；明显不是 1990 年的招牌，因为说明牌本身也是展品之外的说明。11-04 修正：游戏相机的 76° 是垂直视场，第一版按对角算，屏幕上的像素数高估了一倍（交互设计 场景 3 查出）。现在每种牌按两个距离定字号：远处认得出（房号、房名、标题），走近读得清（其余内容）；1080p 下中文字身不小于 16 像素、英文 x 高不小于 8 像素。站台牌放大到 800 × 480 毫米、顶边 1.15 米；房间牌的中文放大到 70 毫米，从 3 米外的通道就能读；总览板的房名移到平面图旁边的图例。
- 这页只定字体、字号、颜色和版式；牌子放在哪里、写什么，按 `10_research.md` §3.4 和 level_space `70_structures_districts.md`。

v1 2026-11-03 (平面视觉, G `local_6bdd7780`); v2 2026-11-04 08:4x after 交互设计's review of the scene 3 builder (场景 3, G `local_8bb66aad`, F1: the camera). RULES 5b: scenes 3 and 4 need placards, and their faces and sizes are this line's (`10_research.md` §3.4; 70 §2). Language follows `10_research.md` Q3: code, kind and the Try line in English, as the game shows them; Red's lines (what it is, Red decides) in Chinese. Mocks at true size with the real faces and 场景 3's real table (`placards.json`): `~/FrontRoomsVisualWork/showcase_placards/placards_v2.py`; images below.

## 1. The system

- **Faces (the game's UI set, `Assets/Resources/Fonts`):** Bayon for a kind or room name in capitals; IBM Plex Mono Medium for codes and digits (Bayon has no figures); IBM Plex Mono Regular for Try lines and the foot; Noto Sans SC Regular for Chinese body and Bold for titles and Red's questions. No period faces: placards stand outside the 1990 fiction.
- **Colours:** panel albedo `#161513` (0.086, 0.082, 0.075), matte; text unlit `#F4F1E8` paper (the UI's Paper), so it reads in every light state; labels and the foot unlit `#96938B`; the UI accent `#F4DF3B` for 要你决定, its bar, question numbers and the live state. No other colour, no backing card behind the panel, no shadow on the text.
- **The camera:** the game camera's `fieldOfView` is 76° vertical (`FrontRooms3DGame.cs:515`), so f = 540 / tan 38° = 691.2 px at 1080p; the eye stands 1.62 m. v1 took 76° as the diagonal (f ≈ 1410 px): its pixel figures were 2.04 × too large. VL504 corrects VL500.
- **Size rule:** each kind has a FAR distance, where its identity must read (number, name, title), and a NEAR distance, where the player steps up to read the rest, as at a museum label; distances run from the eye to the panel's centre. At 1080p: Chinese em at least 16 px; Latin running text x-height at least 8 px (Plex Mono about 15.5 px); words in capitals, cap height at least 11 px. These are stricter than the Smithsonian guide's x-height of 1 % of the distance at every distance here.
- **Phones:** the same placards. At the same distance an iPhone draws them smaller (场景 3: C's Chinese is 4.3 pt in v1, about 7.5 pt in v2), so a phone player steps closer; parity is in the content, not the pixel size.
- **Code chip:** the station or district code in Plex Mono Medium inside a 1.4 mm paper keyline, radius 3 mm, padding 6 mm (9 mm on E): the keycap grammar of the prompt chips, so a code reads as "say this".
- **Setting:** flush left, ragged right; no tracking on CJK; Chinese breaks by filling the line (body) or after a phrase mark (a question: after ，), a full-width mark never starts a line, no lone last character; a question wraps hanging after its number. Plex Mono Try steps joined by ` · `, one action a step; Try rows wrap at a word space, at the Try pitch.
- **Production:** world-space UI text (as the game's other UI `Text`), the raster density per kind from its NEAR distance (场景 3's F2). The Chinese lines need Noto Sans SC's subset grown to the tables' characters (add the showcase tables to `Tools/localization/subset_fonts.py`'s inputs and re-run it). No OS fonts: the iPad and iPhone builds must draw the same face.

## 2. The kinds

| Kind | Panel and mount | Read from (eye to centre) | Lines: face, size (mm) |
|---|---|---|---|
| **C · Station placard** | 800 × 480, landscape, top edge 1.15 m, tilted 45° to the eye; on its own post at the bay's front right corner (F8: a rail at the same height if the bays get one); same size at every station | NEAR 0.99 m, standing 0.75 m from it; the foot from 0.8 m | code chip (Plex Mono Medium, cap 20) + kind (Bayon, cap 30) · Try: `KEYS` / `TOUCH` (Plex Mono Medium, cap 16, grey) and the steps (Plex Mono, x-height 12, pitch 27, from x 140, measure 620) · what it is (Noto Sans SC Regular, em 24, pitch 32, at most 2 lines) · `要你决定` (Bold, accent) + the question (Bold, paper), a 5 mm accent bar at the margin · the foot pinned to the bottom: staging and status (Plex Mono, x-height 9.5, pitch 17, grey), the live state right-aligned (Medium, accent); no "rule set" (F4: the museum's internal word) |
| **D · Group card** | 640 × 290 on the front at the left end of a variant row, centre 1.37 m | NEAR 1.18 m, 1.15 m from it | the property (Bayon, cap 36); its Chinese (Regular, em 28); the variants as letter chips + names (Plex Mono Medium / Regular, x-height 14), rows by the table's `rowStarts` |
| **Floor tag** | 380 × 150 on the floor in front of each variant | 1.76 m, standing 0.7 m behind it (the floor's slope counted) | letter (Plex Mono Medium, cap 62) + name (Medium, x-height 23, at most two lines, pitch = cap + 12) |
| **B · Room sign** | 720 × 470 beside each entry (hub side and inside), centre 1.37 m | FAR 3.0 m across the lane: number, name, Chinese; NEAR 1.2 m at the entry: the rest | number (Plex Mono Medium, cap 120); name (Bayon, cap 50, one or two lines) over its Chinese (Bold, em 70); kinds (Plex Mono, x-height 14.5, pitch 30, measure 640); foot: stations and minutes (Medium, x-height 14.5, accent) and `→ n.1` (Medium, x-height 20, accent) |
| **Room number over the entry** | applied to the wall above the door, no panel | across the hub, up to 12 m | Plex Mono Medium, cap 300 mm, `#161513` on the grey wall. Unlit text is fine for now (F9): in the Reading state's even light it reads as paint; a lit path is later polish |
| **A · Hub board** | 1400 × 700 facing the spawn, top 1.5 m so rooms 2 and 3 stay in view | FAR 3.04 m (the spawn): title, Chinese, the plan's numbers; NEAR 1.57 m: legend and controls | scene name (Bayon, cap 80) over its Chinese (Bold, em 72); the plan in 2 mm paper lines on 35 mm cells (rooms 140 mm) with each room's number only (Medium, cap 60) and its alcove picture; the legend right of the plan, one row a room: number (Medium, cap 30), name (Bayon, cap 30), Chinese (Regular, em 37, grey) (F6: names do not fit inside the rooms); the controls on two lines (Plex Mono, x-height 20, pitch 48) |
| **E · District sign (scene 4)** | 1200 wide, top edge 2.05 m, height = content + 60 mm margins (SP1, the longest, 1.14 m), placed as 70 §2 says | FAR 2.5 m (the arrival point): code and title; NEAR 1.41 m: body and questions | code chip (Plex Mono Medium, cap 40) + title (Noto Sans SC Bold, em 58); body (Regular, em 33, pitch 44); `灰盒占位` (Bold, grey) and its list in light grey; `要你决定` (Bold, accent, 6 mm bar) and one numbered question a line (number in accent, question Bold paper, hanging) |

Checked at 1080p with f = 691.2 px (`placards_v2.py` prints the table): C code 14.0 px cap, kind 21.0, labels 11.2, steps 8.4 px x-height, Chinese 16.8 px em, foot 8.2 at 0.8 m · D title 21.1, Chinese 16.4, names 8.2 · tag letter 22.3, name 8.3 · B number 27.6, name 11.5, Chinese 16.1 across the lane, kinds and foot 8.2 at the entry · A title 18.2, Chinese 16.4 and numbers 13.7 from the spawn, names 13.2 and controls 8.8 up close · E code 11.0 and title 16.0 at arrival, body 16.2 stepping up. Fit with the real table: all 31 stations (the worst, 1.6 and 3.1, with three Try rows and a two-line foot), both group cards, every tag (VENDING / MACHINE the longest), the four room signs and the board.

## 3. Images

| | |
|---|---|
| C, 1.6 RUN! DOOR (a worst case) at true proportions | `images/placard_C_station.jpg` |
| C as the player sees it at 0.99 m, 1080p (x2 nearest) | `images/placard_C_seen_1080p.jpg` |
| D, DEV CLOSE-UPS (two rows) | `images/placard_D_group.jpg` |
| Floor tag, C VENDING MACHINE | `images/placard_floor_tag.jpg` |
| B, room 2 | `images/placard_B_room.jpg` |
| A, the hub board with its legend | `images/placard_A_board.jpg` |
| E, SP1 with 70's text | `images/placard_E_district.jpg` |
| E as seen stepping up, 1.41 m, 1080p (x2 nearest) | `images/placard_E_seen_1080p.jpg` |
| VL504, all kinds | `images/vl504_placards_v2.jpg` |
