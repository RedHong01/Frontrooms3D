# C16 alarm door look (Relay v2 V7b): period research, design, as built (10)

游戏视觉 (account A, session local_5656cffd), workflow stage "V7b alarm door look", 2026-11-01.

- **Red's ask in this chat (2026-11-01 18:49):** "我需要你接手之前另外账号暂停的工作然后继续" (resume the paused visual line). Standing words that bind this stage: "在做视觉的时候，我希望你构思都以最高规格的去进行构思和落地" (visual standard 1.1) and rule 25, "做的时候先不用测，直接做，然后等都落地了一起测即可".
- **Red's decision this look follows:** 10A, "不能撞开" (interaction design's record `research/interaction/doors/20_design.md` §00, row 10; §4.6: the armed door opens and trips "only on a push you face ... held 0.4 s while the bar visibly travels").
- **The map's hooks (关卡设计, s63_v7b NOTE, 10-31; not Red's words):** `Door.alarm` set on every build before `FrontRoomsInteractableKit.DressDoor`; `map.IsAlarmArmed(door)` / `IsAlarmTripped(edge)`; the P3 default: double-acting, a crash bar on both faces, the horn over the head, the placard.
- **Signed words:** `ALARM WILL SOUND`, one line, never EXIT (`research/narrative/90_alarm_devices_and_door_strings_signoff.md` §4). The lettering is 平面视觉 · 信息设计's: this stage builds the slot and a STAND-IN face only.
- **Done here:** main read only; no Unity on main; no git; nothing played or heard. Kits built in Blender 4.3 (headless) in the clone `W/proj_bstations`; one Unity batch there imported them (kit check, not a test). No download: sources were read on the web (patents) and are listed in `SOURCES.md`.

## 0. Reused, not redone

- The alarm family's language and parts: fire red `Alarm_BandRed` (#B01E18), `Alarm_Grille`, the LED `Alarm_LED` and its clock `FrontRoomsAlarmTellLed` (standby blip, Reduce Flashing, alarm latch), the reduced tier, the STAND-IN face method (`Tools/lookdev/gen_alarm_card_standin.py`, TeX Gyre Heros Bold) from the ALARM ZONE tell (`research/room_visuals/50_alarm_zone_tell.md`).
- The door kit family: the D1.6 facade `FrontRoomsInteractableKit.DressDoor`, the DA leaves and their sidecar anchors, the F1 vision lite (`interact_door_lite_common.py`), the F4 frames, the lock trim and the unlock shot's keyhole (`FrontRoomsDoorRig`), the break visual's pose node.
- HW-DA1 (`research/door_hardware/20_build.md`): the period DA hardware finishes; this device's metal is a tiling satin grain in the same manner as its plate maps.
- The body-door solver's alarm branch (`FrontRoomsMapWorld.BodyDoors.cs`, 交互设计 / 关卡设计): the 0.4 s hold is theirs; the look only reads it (contract, section 5).

## 1. Period sources (S1 to S7 in `SOURCES.md`)

| Id | Source | What it shows for c. 1975-1992 |
|---|---|---|
| S1 | US 4,006,471, Detex Corp (Pappas), filed 1975-01-31, issued 1977-02-01, "Emergency exit lock system for doors" | Panic bar plus an armored housing on the door's inside face; "a pair of alarms or horns 32 in the upper corners of the housing", the housing perforated for the sound; battery 30; an inside key cylinder on the housing front; a mode push button 49; a red (armed) / green (disarmed) indicator FLAG behind an aperture, no lamp; the bar carries "suitable legends as to function and purpose" |
| S2 | US 4,631,528, Emhart Industries (Handel, Robida, Slusarski), filed 1984-10-02, issued 1986-12-23, "Push bar exit device with alarm" | A modular alarm at the latch-bolt end (cover and die-cast frame), "lock cylinder assembly 52 for arming and disarming", "light-emitting diode 53 which provides an indication of alarm state" (also power and the delay), a battery pack |
| S3 | US 4,785,286 (Martin), filed 1986-01-31, issued 1988-11-15, "Alarm unit for panic bar type door operator" | The alarm inside the panic bar's channel; two 9 V batteries; a pair of horns behind slots in the cover; a key cylinder on the key-side cover with armed / disarmed / maintenance positions; a red (armed) / green wheel behind a hole; "the alarm continues to be given even after the panic bar is released"; shut off "only by use of the key" |
| S4 | US 5,139,292 (Beals, Ralph's Welding), filed 1991-07-01, issued 1992-08-18, "Emergency exit alarm lock" | A push plate with a sign panel ("appropriate emergency instructions"), a louvered shroud, dual buzzers, a 9 V battery, a key switch On / Off |
| S5 | Detex ECL-230D, current listings (no era claim) | The industry's battery alarm exit control lock "for more than 30 years"; 100 dB, 9 V; the alarm resets only with the key; an English push-plate sign as standard |
| S6 | Von Duprin 22 / 98 / 99 alarm kits, current listings (no era claim) | A 9 V internal horn armed by key; "LED display notifies when armed" |
| S7 | Detex EAX-500 (via `90` §4, its search 2026-11-01) | The sign text "Emergency Exit / Alarm Will Sound" on a current product; the pre-1990 wording stays UNVERIFIED (`90` §4) |

**What the period read gives the look.**
1. The housing at the latch end, the key cylinder on its face, a small indicator, a mode button: S1, S2, S3 (1975-1988).
2. The indicator: flags in 1975 and 1986, an LED "indication of alarm state" by 1984 (S2). The LED's exact armed pattern is UNVERIFIED (no source gives it). The look uses the alarm family's period standby blip (one dim blip every 10 s, `FrontRoomsAlarmTellLed`, sourced for 1985-93 detectors) and latches it steady once tripped: S3's alarm runs until a key resets it.
3. The alarm sound comes from horns in the device in every source. The horn over the head is the map's P3 choice (kept: one place for the sound, lifted over the head, as the sound line's ping says); it is drawn as a period surface horn on a back box.
4. **No strobe or flasher.** None of S1-S4 has one, and the visual alarm notification of the period belonged to fire alarm systems (ADAAG 1991), not to battery exit alarms. Not built; nothing in this look flashes faster than the 0.1 Hz blip (photosensitive rules met by construction; Reduce Flashing gives a steady glow).
5. The legend: on the bar or its push plate in S1, S4 and S5. The look keeps the signed words on their own placard at eye level (section 3) because a legend that fits the pad (40 mm) would have 16 mm caps; noted as a choice for 信息设计.
6. Finish: exit devices of the period were clear anodised aluminium (US28 / 628) or satin stainless; this device is satin anodised aluminium, the horn and placard fire red.

## 2. The device (both faces, P3)

Part frame (Unity metres): origin on the leaf face, X from the housing's latch-side edge toward the hinge, Y from the pad's centreline, Z out of the face. Mount: door (+-0.022, datum, 0.975); face S `Euler(0, 90, 0)`, face P `Euler(0, -90, 0)` with scale (-1, 1, 1), as the lock parts, so the housing is at the latch end on both faces.

| Part | As built |
|---|---|
| Alarm housing | 130 (X) x 150 (Y) x 70 (Z) mm, its bottom flush with the rail; a cover with 9 mm rounded edges on a 4 mm base plate (the seam reads at 0.3 m); four oval-head tamper screws |
| Arming key cylinder | A 1-5/32 in mortise cylinder face in a 35.6 mm collar, satin chrome, the plug with a dark ring and a modelled warded keyway; centre X 0.065, Y 0.068 |
| Pilot LED | T-1 3/4 (5 mm) dome in a chrome bezel; X 0.040, Y 0.106; slot `Alarm_LED` |
| Mode / test button | Black cap in a chrome bezel; X 0.090, Y 0.106 (S1's push button 49) |
| Rail | A rounded C-channel 48 x 36 mm, 2 mm walls, a dark floor; X 0.126 to 0.829 |
| Touch pad | 689 x 40 x 36 mm, 8 mm front rounds, a 0.6 mm crowned face, black end caps; rests 30 to 66 mm off the leaf; **travel 12 mm** along part -Z |
| Hinge-end cap | 28 x 51 x 40 mm, rounded, one screw; ends at door Z 0.118 |

**Heights.** Free doors (the shipped rows L0-F and OF-F, `doorsNeedKeys` off): datum 1.000 m, and the lever and rose are not built on alarm doors (the bar is the handle; the latch and strike stay). Locked doors (keys mode): every lock part stays where the unlock shot needs it, and the bar rides above it at datum 1.097 m (the rail 1.073 to 1.121: 3 mm over the escutcheon's top at 1.070, 4.7 cm over the plates, 3 mm under the F1 lite's bead at 1.124). Both are inside the 34 to 48 in band for exit hardware.

**Motion (Red's 10A).** The pad shows the map's hold: the facing push's 0.4 s carries it the full 12 mm (smoothstep: the spring resists, the pad runs, it seats), the door opens and trips at the bottom, and the pad springs back in 0.15 s. A push the door refuses (slow, sprint, oblique: the hard stop) shows a strain of 0.35 of the hold (about 3.4 mm). Until the map line lands the contract (section 5) the pad plays a press at the trip instead (in 0.08 s, held 0.10 s), on the face the camera is on; the same press serves the E open when body doors are off.

**States.** Armed: the LED's standby blip (phase by place). Tripped: the LED latched steady and brighter. Read from the map each frame, never from the Relay, the player's distance or a tier: no radar.

**Break.** The Relay may break an alarm door. At the break's FINAL stage the leaf-mounted parts come off on both faces (the torn latch strip carried the housing); a door rebuilt broken gets the horn only. The steel family's bow does not carry the bar before FINAL (keys mode only; noted).

## 3. The placard slot

- A 15 x 3 in (381 x 76 mm) plate of 1/16 in screen-printed aluminium, R 4 mm corners, adhesive mounted, fire red; its face is one decal (slot `Alarm_DoorPlacard`, UV 0..1, u to the viewer's right).
- Both faces at door (+-0.022, **1.420**, **0.770**): eye level, on the latch-side panel beside the F1 lite (3.3 cm clear of its bead), never mirrored, so the words read the right way round on both faces.
- **STAND-IN face** (`Resources/Print/AlarmDoor/alarm_door_placard_albedo.png`, 2048 x 408, `Tools/lookdev/gen_alarm_door_placard_standin.py`): `ALARM WILL SOUND`, one line, TeX Gyre Heros Bold, warm white on #B01E18, 25.0 mm caps with 12 mm side margins, a small STAND-IN mark bottom right. `FrontRoomsAlarmDoorLook.PlacardFaceIsStandIn` is true until 信息设计's face replaces the PNG in place (same path and GUID, any size of the 381 : 76 aspect).

## 4. The horn

One per door, on the face of the zone that owns the door (`FrontRoomsAlarmDoors.AlarmedZone`; the S face for a tools mark), centred over the opening on the wall face, its bottom at 2.180 m (top 2.304, under the Low ceiling of 2.4): a 124 mm square back plate, a 112 mm body with 10 mm rounds, a 94 mm projector with a rolled lip, seven slats over a dark grille, four screws; fire red. It never lights. Its sound is the sound line's (`DeviceTripped`, id `alarmdoor:(x, y)-(x, y)`; the grille's centre is 2.242 m over the floor, 58 mm off the wall).

## 5. The map hook that was missing (contract)

The hold's progress is private to the solver (`BodyContact.lean`), so the bar could not show "the bar visibly travels". `W/apply/v7b_alarm_door_contract/` asks 关卡设计 for one read-only method, `FrontRoomsMapWorld.AlarmBarPress(Door)` (signed by the face, 0..1 of the hold, a strain for a refused push), and one line in the `BodyDoors` setter that hands it to `FrontRoomsInteractableKit.AlarmBarPressFromMap`. Inert by itself; the look works without it (the trip press).

## 6. As built: budgets and files

| Kit | LOD0 / LOD1 / LOD2 tris | Switch (m) | Slots |
|---|---|---|---|
| `Kit_AlarmExit_Body` | 6,102 / 300 / 36 | 2 / 8 / cull 40 | `Alarm_ExitAlu`, `Prop_ChromeSatin`, `Prop_PlasticBlack`, `Alarm_LED` |
| `Kit_AlarmExit_Pad` | 344 / 132 / 12 | 2 / 8 / cull 40 | `Alarm_ExitAlu`, `Prop_PlasticBlack` |
| `Kit_AlarmExit_Horn` | 3,524 / 284 / 44 | 3 / 10 / cull 45 | `Alarm_BandRed`, `Alarm_Grille`, `Prop_ChromeSatin` |
| `Kit_AlarmExit_Placard` | 246 / 14 / 2 | 3 / 12 / never culled | `Alarm_BandRed`, `Alarm_DoorPlacard` |

- One alarm door at hero range: 2 x (6,102 + 344 + 246) + 3,524 = **16,908 tris**, 19 draws (2 x (body 4 + pad 2 + placard 2) + horn 3); beyond 2 m (LOD1): 2 x (300 + 132 + 14) + 284 = 1,176 tris, 18 draws. Phones drop LOD0: LOD1 from the near plane (1,176 tris). WebGL never dresses map doors.
- Textures: `Alarm_ExitAlu_{A,N,S}` 1024 x 1024 (`Tools/lookdev/gen_alarm_exit_alu.py`, tiling over 0.15 m, 0.15 mm per px), the placard 2048 x 408: about 5.3 MiB on desktop with mips (BC7), loaded only when an alarm door is dressed; they ship in Resources whatever the flag.
- Frame time: not measured (rule 25); the look is one LateUpdate of about ten compares per alarm door, no light, no shadow from the horn.
- Code: `Assets/Scripts/Rendering/Doors/FrontRoomsAlarmDoorLook.cs` (new); `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs` (the hook, the lever skip, the delegate). Kits: `Tools/Blender/frontrooms_kit/assets/alarm_exit_{common,body,pad,horn,placard}.py`.
- **Inert with `alarmDoor` off:** `door.alarm` is false (the map's `IsAlarmEdge` returns false at once), so `alarmBar` is false, the lever branch runs as before (`else if (!alarmBar)`), `Attach` is never called, the delegate is never read, and the new kits are never loaded (`KitsAvailable` is evaluated only after `door.alarm`).

## 7. Open

1. 信息设计: the placard's final face (spec in the package README and this section 3); whether the legend should also ride on the pad (S1, S4).
2. 声音设计: the horn sound at the device point, and optional bar events (section 2's travel: the press, the strain, the seat at the trip, the spring back) if they want them; nothing here posts events.
3. 关卡设计: the contract (section 5).
4. The steel family's bow under the bar before FINAL (keys mode only).
5. Frames for the combined test: `W/vl_pending/v7b_alarm_door.md`. Blender QA stills of the assembly (not the game): `W/v7b_alarm_door/previews/assembly/`.
