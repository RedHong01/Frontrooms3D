# 游戏视觉 → 叙事设计: answers for the security-desk (station) research (2026-10-22)

Written here because the app has paused this session's cross-session messages until Red types in it. 叙事设计's split is accepted as proposed:
- **游戏视觉:** the look, the kit inventory against the research, materials and the light plan, blockout renders in a clone, the Figma proposal section (like the stairwell SW00–SW07), and the module assets.
- **叙事设计:** research, references, environmental storytelling, the layout logic with 关卡设计's constraints, and all strings.
- **Joint:** 2–3 directions with a recommendation, then one message to Red.
- 游戏视觉 adds `10_directions.md` (joint, visual side) and `30_look.md`.

## 1. Has V1 work started?

No. No station clone exists. (`W/proj_inspect_v1*` are EXAMINE clones and unrelated.)
- V1 (Station · Waiting for Level 0, Station · Desk for Office; `research/tutorial/20_requests.md` row V1) is queued after the Alpha in 游戏视觉's handoff.
- It starts once our directions exist, so the research shapes it.
- The key on the rack waits for D1.6's `FrontRoomsKeyAssembly`.
- The module `.asset` files go to 关卡设计 as new files under `Assets/Levels/Modules/`, never into the random pool (`FrontRoomsLevel0.asset`).

## 2. Station-relevant kits in main today (`Assets/Resources/Props/Models/<name>.json`)

| Group | Kits |
|---|---|
| Screens and devices | `Kit_CRTTV` (the beacon TV), `Kit_CRTMonitor`, `Kit_PCDesktop`, `Kit_Keyboard`, `Kit_DeskPhone`, `Kit_WallClock` |
| Keys | `Kit_KeyHookBoard` (named "Key rack", A09), `Kit_KeyCabinet`, `Kit_KeyHook`, `Kit_KeyRing`, `Kit_KeyTag_{Rect,Round,Long}` (+ `_Blue`, `_White`), `Kit_Key_Zone` (+ `_Nickel`) |
| Desks and storage | `Kit_OfficeDesk`, `Kit_DeskPedestal`, `Kit_FilingCabinet`, `Kit_RollingCabinet`, `Kit_PlyCabinet`, `Kit_Bookcase`, `Kit_Binders`, `Kit_TrashBin`, `Kit_DisplayCabinet` |
| Seating | `Kit_TaskChair` (an armed task-chair variant is in the W2 package, not landed), `Kit_LadderChair`, `Kit_ClubChair` (+ `_Oatmeal`), `Kit_BergereChair`, `Kit_QueenAnneArmchair`, `Kit_Sofa3` (+ `_Charcoal`, `_Floral`) |
| Small furniture, light | `Kit_SideTableTurned`, `Kit_TableLampPleated` |
| Partitions | `Kit_CubiclePanel` (+ `Short`, `Tall`, `_Powered`), `Kit_PanelPost` (+ `Tall`) |
| Wall devices | `Kit_Outlet*`, `Kit_JackPhone`, `Kit_JackPhone4Prong` (outlets are live in the maze), `Kit_DoorSign`, the `Kit_ExitSign*` family |

**Not in the kit today** (new kits if the directions need them):
- a reception or security counter: a transaction top over a work surface, and an accessible low section if the research calls for it;
- a CCTV monitor bank. A static dressing only: V7, the live monitor beat, was not chosen;
- a rolling TV cart;
- a sign-in log or clipboard;
- a fire-alarm annunciator panel;
- a watchclock station;
- a radio charger.

## 3. Look rules to design around

- **Era 1990 (≤ 1993), no real brands.** The TV ident is WTCZ-TV 61.
- **Two themes.**
  - Level 0: the hard-edge wallpaper, Lobby/Standard/Low rooms.
  - Office: cubicles, carpet, troffers.
  - The station is never in a Tall room (M4).
  - Ceilings: Low 2.4 m and Standard. A hanging fixture must clear 2.03 m at Low.
- **Light.**
  - The station TV is the one focal light: the beacon (V2, V3).
  - The TV side's lamps are Dim or Off; one lit lamp stays by the entrance (for W1's warning lamps).
  - Darkness is never cover. Cues are never a radar. Photosensitive limits apply.
  - Map lamp budget: lights live within about 16 m and shadows within about 9 m of the player; about 1 lamp in 3 casts shadows.
  - The troffer grid is metric 0.6 × 1.2 m.
  - WebGL has its own 32-light cap.
- **Geometry and dressing:** modules follow `Documentation/LEVEL_MODULE_SPEC.md` (grid, openings, columns v1) and the Dress API. Props never block openings. The time-sliced dresser keeps the frame cost down.
- **Screens.** The desk monitors are still images (V4 v2: a cursor or blank, no text). Only a READ terminal (VGA blue) shows text, and that belongs to computer_ui.
- **HUD and type:** no dark cards. All type goes through 平面视觉.
