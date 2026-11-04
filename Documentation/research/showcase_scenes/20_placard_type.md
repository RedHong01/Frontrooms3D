# Showcase scenes · 20 · Placard type: faces, sizes and colours (平面视觉)

- 给 Red：场景 3、4 的说明牌用游戏界面那套字（Bayon、IBM Plex Mono、思源黑体 Noto Sans SC），深色牌面、纸白字、界面黄色标出“要你决定”；明显不是 1990 年的招牌，因为说明牌本身也是展品之外的说明。字号按史密森尼博物馆的规则算：小写字高约等于阅读距离的 1%。按 1080p 实际看到的像素验过：展台牌在 1.1 米处正文 x 字高 11.6 像素、中文 24 像素；场景 4 区域牌在 2.5 米处中文 18 像素，都高于我们的中文 16 像素下限。
- 这页只定字体、字号、颜色和版式；牌子放在哪里、写什么，按 `10_research.md` §3.4 和 level_space `70_structures_districts.md`。

Date 2026-11-03 (平面视觉, G `local_6bdd7780`). RULES 5b: scenes 3 and 4 need placards, and their faces and sizes are this line's (`10_research.md` §3.4: "faces and sizes are 平面视觉's"; 70 §2: the district signs are "a placeholder until 平面视觉 sets faces and sizes"). Language follows `10_research.md` Q3 as scene 3 builds it: code, kind and the Try line in English, as the game shows them; Red's lines (what it is, Red decides) in Chinese. Mocks at true size with the real faces and text: `~/FrontRoomsVisualWork/showcase_placards/placards.py`; images below.

## 1. The system

- **Faces (the game's UI set, `Assets/Resources/Fonts`):** Bayon for a kind or room name in capitals; IBM Plex Mono Medium for codes and digits (Bayon has no figures); IBM Plex Mono Regular for Try lines and meta; Noto Sans SC Regular for Chinese body and Bold for titles and Red's questions. No period faces: placards stand outside the 1990 fiction.
- **Colours:** panel albedo `#161513` (0.086, 0.082, 0.075), matte; text unlit `#F4F1E8` paper (the UI's Paper), so it reads in every light state; labels and meta unlit `#96938B`; the UI accent `#F4DF3B` for 要你决定, its bar, question numbers and the live state. No other colour, no backing card behind the panel, no shadow on the text.
- **Size rule:** Latin x-height ≥ 1 % of the reading distance (Smithsonian Fig. 12: 9 mm at 1 m, 19 mm at 2 m); CJK em = the Plex Mono cap ÷ 0.70 (WORLD_TYPE §3), and never under 16 px at 1080p from the stop point.
- **Code chip:** the station or district code in Plex Mono Medium inside a 1.2 mm paper keyline, radius 2.5 mm, padding 5 mm (9 mm on E): the same keycap grammar as the prompt chips, so a code reads as "say this".
- **Setting:** flush left, ragged right; no tracking on CJK; Chinese breaks by filling the line (body) or after a phrase mark (a question: after ，), a full-width mark never starts a line, no lone last character; a question wraps hanging after its number. Plex Mono Try steps joined by ` · `, one action a step.
- **Production:** world-space UI text (as the game's other UI `Text`): sharp at any distance and edited by changing the table. The Chinese lines need Noto Sans SC's subset grown to the tables' characters (78 of scene 3's 317 are missing today, `90_plan.md` F-D): add the showcase tables to `Tools/localization/subset_fonts.py`'s inputs and re-run it (its owner). No OS fonts: the iPad and iPhone review builds must draw the same face.

## 2. The kinds

| Kind | Panel and mount | Read from | Lines: face, size |
|---|---|---|---|
| **C · Station placard** | 600 × 340 mm, landscape, on the rail at the bay's front right corner, top 1.0 m, tilted 45° to the eye; same size at every station | stop mark, about 1.1 m | 1 code chip (Plex Mono Medium, cap 18 mm) + kind (Bayon, cap 26 mm) · 2 Try: label `KEYS` / `TOUCH` (Plex Mono Medium, x-height 5.2 mm, grey) and the steps (Plex Mono, x-height 9 mm) · 3 what it is (Noto Sans SC Regular, em 18.5 mm, pitch 24 mm, ≤ 2 lines) · 4 `要你决定` (Bold, accent) + the question (Bold, paper), a 4 mm accent bar at the margin · 5 pinned to the foot: kind, prefab, status (Plex Mono, x-height 4.6 mm, grey) and the live state right-aligned (Medium, accent) |
| **D · Group card** | 520 × 236 mm on the front at the left end of a variant row, centre 1.37 m | the lane, about 1.5 m | the property (Bayon, cap 34 mm); its Chinese (Regular, em 24 mm); the variants as letter chips + names (Plex Mono Medium / Regular, x-height 11 mm) |
| **Floor tag** | 260 × 130 mm on the floor in front of each variant | standing behind it, about 1.8 m | letter (Plex Mono Medium, cap 62 mm) + name (Medium, x-height 16 mm) |
| **B · Room sign** | 640 × 400 mm beside each entry (hub side and inside), centre 1.37 m | across the 3 m lane | number (Plex Mono Medium, cap 120 mm); name (Bayon, cap 44 mm) over its Chinese (Bold, em 36 mm); kinds (Plex Mono, x-height 11 mm, two lines at most); foot: stations and minutes (Medium, accent) and `→ n.1` (Medium, x-height 20 mm, accent) |
| **Room number over the entry** | applied to the wall above the door, no panel | across the hub, up to 12 m | Plex Mono Medium, cap 300 mm, in the panel colour `#161513` on the grey wall |
| **A · Hub board** | 1400 × 700 mm facing the spawn, top 1.5 m so rooms 2 and 3 stay in view | the spawn, about 3 m | scene name (Bayon, cap 80 mm) and its Chinese (Bold, em 64 mm); the plan in thin paper lines (2 mm) with each room's number (Medium, cap 60 mm), name (Bayon, cap 30 mm) and alcove picture; the controls on two lines, desktop and touch (Plex Mono, x-height 20 mm) |
| **E · District sign (scene 4)** | 1200 mm wide, top edge 2.05 m, height = content + 60 mm margins (0.8–1.1 m; SP1, the longest, takes 1.1 m), placed as 70 §2 says | the arrival point, about 2.5 m | code chip (Plex Mono Medium, cap 40 mm) + title (Noto Sans SC Bold, em 56 mm); body (Regular, em 32 mm, pitch 43 mm); `灰盒占位` (Bold, grey) and its list in light grey; `要你决定` (Bold, accent, 4 mm bar) and one numbered question a line (number in accent, question Bold paper, hanging) |

Checked at 1080p, FOV 76 (desktop): C at 1.1 m is 771 px wide, Try x-height 11.6 px, CJK em 23.8 px, meta x-height 5.9 px (the meta is for stepping closer); E at 2.5 m is 682 px wide, CJK em 18.2 px. On the iPhone 16 Pro (2622 px across the same FOV) C's CJK em is 10.8 pt.

## 3. Images

| | |
|---|---|
| C, 1.1A BODY DOOR at true proportions | `images/placard_C_station.jpg` |
| C as the player sees it at 1.1 m, 1080p | `images/placard_C_seen_1080p.jpg` |
| D, DOORS BY MEMBER | `images/placard_D_group.jpg` |
| Floor tag | `images/placard_floor_tag.jpg` |
| B, room 2 | `images/placard_B_room.jpg` |
| E, SP1 with 70's text | `images/placard_E_district.jpg` |
| E as seen at 2.5 m, 1080p | `images/placard_E_seen_1080p.jpg` |
