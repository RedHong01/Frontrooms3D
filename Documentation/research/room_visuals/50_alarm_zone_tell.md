# 50 · The ALARM ZONE tell: the detector and the card at every trigger-room entrance

游戏视觉 (game visual), 2026-10-31, Run! B production workflow, stage "Tell". Built in the clone `W/proj_alarmtell` (main 931ad7de + the Library from `W/proj_audit`), packaged as `W/apply/alarm_tell.sh`. `W` = `/Users/redwang/FrontRoomsVisualWork`.

Testing rule (Red, 2026-10-31 ~01:00, 游戏视觉 chat: "做的时候先不用测，直接做，然后等都落地了一起测即可"): nothing here was rendered, played or measured. The gates were the compile (Roslyn macOS + iOS, 0 errors) and the package's dry run and rehearsal. Every number below is a design number or arithmetic, not a measurement. §7 lists what the joint test round checks.

## 0. Short answer

1. **What the rule asks.** RELAY §6 (placement rules) and row 10: every trigger room carries one signature at every entrance, a ceiling detector with a red-banded base and an emissive ALARM ZONE card, readable in a dark room. No harmless detector props anywhere. L7 makes Run! rooms the tier-2 trigger kind, so they carry it beside B's own NOTICE plate.
2. **What was built.** Two kits, five materials, a stand-in card face, a static API with no caller, an LED clock, and an edit-mode test suite for the joint round.
   - `Kit_AlarmDetector`: a 1985–93 plug-in smoke head on a twist-lock base. Ivory head Ø129, fire-red base Ø156 on a fire-red trim ring Ø200, a red standby LED. It hangs 71.5 mm. LOD0 / 1 / 2 = **8,590 / 1,210 / 314 tris**, hand-built.
   - `Kit_AlarmZoneCard`: a 1990 internally lit warning sign. A fire-red steel box 286 × 184 × 51 mm with a 254 × 152 mm lit face. LOD0 / 1 / 2 = **734 / 46 / 14 tris**.
   - `FrontRoomsAlarmTell.Dress(entrance, …)`, `DressCeiling`, `DressCard`, `SetAlarm` (optional latch), `DetectorDepth` / `SightDepth` / `SeenFromOutside`, `Find`, `Undress`.
3. **The red band has a code precedent.** NFPA 72 marks heat detectors by a colour code, and where the body is the code colour it asks for "a ring on the surface of the detector" in a contrasting colour (§2.2). A coloured ring is how a real detector carries a class. Ours carries the game's class: red = ALARM ZONE.
4. **The LED is period-true and cannot be a radar.** System Sensor's 1400 and 2400 sheets say the LED "blinks every ten seconds" in standby and lights steady in alarm. The game keeps one dim 0.14 s blip every 10 s on game time, in three phases so neighbours never blink together. It never follows the Relay, the player, a trip or a tier. Reduce Flashing makes it a steady dim glow.
5. **The card cannot be mistaken for EXIT.** It is a flat box on the wall beside the opening at 60 in, with a white legend on a red ground (reverse). An EXIT sign hangs over the door or from the ceiling, with red letters. The card never says EXIT (C41).
6. **The detector sits where the rule can be kept.** "Visible from 2 m outside an entrance" puts it 3.7 m in for a 2.1 m door under a 2.9 m ceiling, and 2.7 m in for a Run! room's 2.2 m arch (§3.4). In tall zones the rule cannot hold (14.8 m). The detector is then clamped to 6 m and the card carries the tell outside. The API reports it (`SeenFromOutside`).
7. **Open:** the card's lettering is a STAND-IN until 平面视觉 · 信息设计 delivers (`51_alarm_zone_for_graphic.md`). Nothing calls the API until 关卡设计 builds trigger rooms (CR-2). The alarm latch is offered, not wired (§5).

## 1. The rule and the calls it touches

| Source | What it says | What it means here |
|---|---|---|
| RELAY §6 placement rules | "every trigger room carries one signature: a red-banded detector base and an emissive ALARM ZONE card at every entrance (readable in a dark room). No harmless detector props anywhere." | One vocabulary for every trigger kind. Main has no detector prop today (checked: no `Smoke`, `Detect`, `Sensor` kit). The kits are spawned only by this API, and no runtime picker selects kits by placement or tag (checked: only the furniture pile enumerates kits, and it takes pile pieces only) |
| RELAY §6 placement rules | "the device is visible from 2 m outside an entrance; the room arms 1.5 m inside" | The detector's depth follows a sight rule (§3.4). The card is outside, so it is always seen first |
| RELAY §6, trigger kinds (系统设计 10-31) | Run! rooms keep "the shared tell at each entrance … beside B's own NOTICE plate" | The tell is independent of the Run rig: it uses no Run type and works in any room |
| RELAY row 10 | 游戏视觉 builds the "ceiling detector with red-banded base and ALARM ZONE card" | This package |
| C41 (Red 10-31) | No EXIT sign anywhere in a Run! room. Fire gear stays red | The card is red fire gear by its family, not a sign of way out. No EXIT word, no arrow, no EXIT form (§3.3) |
| C39 (Red 10-31) | Run! may open off Office zones, with the same B look | The card must read in a lit Office and in a dark corridor: emission plus a red housing (§3.3) |
| `station/30_look.md` §2 | "Colours kept out on purpose: the red family (ALARM ZONE card, RELAY §6)" | Red belongs to this tell. The station annunciator is beige for this reason (`station/03_palette.md`) |
| Three-platform parity (Red 10-30) | Systems identical on desktop, iPhone and iPad | Look and behaviour are the same everywhere. Only the detector's LOD0 is dropped on handhelds and WebGL (§3.6) |

## 2. Research (real sources; nothing downloaded)

### 2.1 Commercial spot detectors, 1985–1993

- **The type.** A conventional commercial smoke detector of the period is a plug-in head on a separate base screwed to a ceiling box. System Sensor (founded 1984 as a division of Pittway) sold the 1400 ionization and 2400 photoelectric direct-wire heads and the 1451 / 2451 plug-in heads on B401-type bases [S1–S5]. The 1451's sheet sells "easy plug-in of the head to base" and two LEDs that "blink" in standby [S4]. The base is what stays on the ceiling. That makes a coloured base a sensible place for a mark that outlives a head swap.
- **Size.** The System Sensor 1400 is 5.5 in (140 mm) across and 3.12 in (80 mm) tall [S2]. A 1980s–90s Simplex head at the Ingenium museum is recorded at 15.9 × 7.5 cm (storage size) [S6]. Ours: head Ø129, base Ø156, trim ring Ø200, 71.5 mm drop.
- **Colour.** The Ingenium Simplex has a buff casing with a smooth and textured finish on a black base [S6]. Period heads were off-white or buff plastic. Ours is ivory (the kit plastic's maps, warmed).
- **The LED.** 1400: "The LED blinks every ten seconds as an indication that power is applied to the detector" and "lights continuously in alarm" [S2]. The 2400 sheet says the same 10 s blink [S3]. Newer i3-style heads blink a green LED every 5 s. They are post-era and are not used [S3]. The indicator itself is old: Cerberus's F3 (1951) was the first detector with an alarm indicator, integrated into the base [S7].
- **Remote indicators.** A single-gang plate with a red alarm LED (System Sensor RA400Z, later RA100Z) repeats a hidden detector's alarm outside a room [S8]. This is a period habit of putting the alarm state where a person approaches.
- **Mounting.** Heads and heat detectors mount to 3.5 or 4 in octagon boxes, single-gang boxes, or a 4 in square box with a plaster ring [S9]. Our trim ring Ø200 covers a 4 in octagon box.
- **Other makers of the era** (shape references only, dates unverified): Cerberus Pyrotronics DI-3 heads on DB-3S bases [S10], and the Gamewell-branded DI-3 [S11].

### 2.2 Marking practice: zones, rings and labels

- **The code of the era.** The 1993 NFPA 72 (National Fire Alarm Code) consolidated NFPA 71-1989, NFPA 72-1990, **NFPA 72E-1990 (Automatic Fire Detectors)**, 72G-1989, 72H-1988 and NFPA 74-1989 [S12]. A 1990 office building was detected under 72E-1990 and signalled under 72-1990.
- **Colour rings on detectors (the precedent for our band).** NFPA 72 classifies fixed-temperature heat detectors and marks them "with a color code": Low and Ordinary uncoloured, Intermediate white, High blue, **Extra high red**, Very extra high green, Ultra high orange. "If the overall color of a heat-sensing fire detector is the same as the color code marking", the detector carries "a ring on the surface of the detector" in a contrasting colour, or the rating in numerals at least 9.5 mm high (NFPA 72-2002 §5.6.2.1.1–5.6.2.1.2, Table 5.6.2.1.1) [S12]. The table is quoted from the 2002 edition. That the same table stood in 72E-1990 is likely (72E is where the 1993 code took detectors from) but **unverified** here. In the real world a red ring would mean an extra-high heat class. Our fiction borrows only the form: a coloured ring is how a detector shows its class, and ours shows ALARM ZONE.
- **Zones.** The code asks a system that serves two or more zones to "identify the zone of origin" by annunciation or coded signal. The location shows "by building, floor, fire zone, or other approved subdivision" (NFPA 72-2002 §4.4.6.1) [S12]. Zones were real, named and labelled things in a 1990 building, on the panel and on its annunciator.
- **Device labels.** No NFPA rule requires a label on each device. Contractors labelled devices with panel, loop or zone and device number because fire departments asked for it (practitioner forum, anecdotal) [S13]. A colour per system (normal, emergency, fire alarm) is a common specification habit for labels [S14].
- **How labels were made, 1985–93.**
  - Embossed tape: Dymo, from 1958, raised letters stamped into self-adhesive plastic tape [S15].
  - Lettering-machine tape: Kroy, trademark filings from 1980–81. The Kroy 190 (c. 1987) printed adhesive tape labels from a detachable keyboard [S16].
  - Engraved laminated plastic (phenolic, "lamacoid", traffolyte): cut through a coloured face to a contrasting core, so the legend never fades. Made to order [S14, S17].
  - Printed adhesive labels and stencils.
  The stand-in uses none of these. A device or zone label is offered to 平面视觉 as an option (51 §6).
- **Lit legends.** Industrial annunciator windows are translucent plastic or glass with engraved or printed text, lit from behind, one per alarm [S18]. Radiology rooms carry lit warning signs at the entrance. One US state's rule asks for visible warning signals "seen from each entrance" [S19, S20]. A vintage hospital sign hung above the X-ray room door, its words lit when switched on [S21]. **Tritium self-luminous signs** (over 2 million sold in the US) [S22, S23] were considered and rejected. Their glow is the narrative's pale yellow-green (W6: one colour, one meaning), and a radioactive sign brings fiction we do not want.
- **Where room signs go.** ADAAG 1991 §4.30.6: signs for rooms and spaces go "on the wall adjacent to the latch side of the door". At double-leaf doors they go on the nearest adjacent wall, "60 in (1525 mm) above the finish floor to the centerline of the sign", where a person can approach without "standing within the swing of a door". §4.30.5 asks for a "non-glare finish" with contrast [S24].

### 2.3 What the research changes

| Finding | Design consequence |
|---|---|
| The base stays, the head plugs in | The red is on the base and its trim ring, not on the head |
| A contrasting ring is a real detector mark | One red band seen from every angle: the trim ring's underside plus the base's side make a red annulus from 129 to 200 mm seen from below |
| LED: one blink per 10 s, steady in alarm | The standby blip, and the optional latch (§5) |
| Remote LED plates and lit warning signs sit at the approach | The card is outside, at the entrance, lit |
| Room signs at 60 in, latch side, out of the door's swing | Card centre 1.524 m, beside the frame, clear of the leaf |
| Tritium and the glow ink are green | The card is lit red and white, never green |

## 3. Design

### 3.1 The detector (`Kit_AlarmDetector`)

| Part | Form | Slot |
|---|---|---|
| Trim ring | Ø200 × 7 mm, rolled outer edge, over the 4 in octagon box | `Alarm_BandRed` |
| Base | Ø156, 7–22 mm under the ceiling, rounded lower edge, twist-lock tab | `Alarm_BandRed` |
| Head collar | Ø127, 22–26 mm | `Alarm_Shell` |
| Smoke-entry band | 36 radial fins 2.4 mm over a dark core Ø116, 26–44 mm | `Alarm_Shell` + `Alarm_Grille` |
| Cover | Ø129 dome with two shallow rings and a raised chamber cap Ø40, to 71.5 mm | `Alarm_Shell` |
| LED | Ø4.8 dome in an ivory bezel on the skirt, 49.5 mm down, facing the kit front (+Z) | `Alarm_LED` |

- Origin on the ceiling plane at the device centre. It hangs into −Y. `minCeiling` 2.4. No collider. Shadows off (under 0.3 m, kit rule).
- LODs (hand-built, kitlib P-1b):
  - LOD0, hero, holds at 0.3 m: 64-sided lathes, 36 fins, LED and bezel, tab and rib. **8,590 tris**, 4 draws.
  - LOD1: 24-sided, no fins (the band reads dark past 3 m), LED kept. **1,210 tris**.
  - LOD2: 12-sided, LED kept. **314 tris**.
  - Switch at 3 m and 9 m, cull at 30 m (sidecar `lodDistances`, kit importer). LOD0 is the heaviest small prop on the line. It shows only within 3 m and only at entrances.
- Red family: `Alarm_BandRed` #B01E18, the same value as B's fire gear (`RunB_FireRed`), smoothness 0.55, the kit plastic's normal at half.

### 3.2 The LED (`FrontRoomsAlarmTellLed`)

| State | Level (× lens colour 1, .055, .033, linear) | When |
|---|---|---|
| Standby | 0, with a 0.14 s raised-cosine blip to 1.6 every 10 s | Always, on game time (pauses with the world) |
| Reduce Flashing | steady 0.45 | The setting is on |
| Alarm latch | steady 2.2 (Prop_LEDRed's level) | Only if the trigger's owner calls `SetAlarm` (§5) |
| Edit mode / no clock | steady 0.45 (the material asset's own emission) | Tools, Level Designer |

- Three phases 3.33 s apart, picked from the device's world position, so two detectors in view never blink together.
- One clock object (DontDestroyOnLoad), made by the first `Dress` in Play mode. It drives three shared runtime copies of `Alarm_LED.mat`, so the SRP batcher still batches them. It writes a material only when its level changes. The asset is never written.
- Photosafety: one 5 mm source at 0.1 Hz is far under any flash threshold. Reduce Flashing still removes it.

### 3.3 The card (`Kit_AlarmZoneCard`)

- A 1990 lit warning sign. Pressed-steel box 286 × 184 × 51 mm (11.25 × 7.25 × 2 in) in fire-red enamel. A 16 mm face flange with four tamper-proof pan screws. A 254 × 152 mm (10 × 6 in) translucent face 2 mm behind the flange, lit from inside.
- Slots: `Alarm_BandRed` (box), `Alarm_CardFace` (face, decal UV 0..1), `Prop_Chrome` (screws). 3 draws at LOD0, 2 after.
- LOD0 **734**, LOD1 **46**, LOD2 **14 tris**. Switch at 5 m and 15 m, **never culled**: the card is the read down a corridor.
- Origin: the wall-face point at the housing centre, face toward +Z. No collider, no light, shadows off.
- **Face (STAND-IN):** white legend on the red ground, "ALARM" / "ZONE", TeX Gyre Heros Bold (the placard face in main), 1.625 in caps. A small "STAND-IN" mark sits bottom right. Generator: `Tools/lookdev/gen_alarm_card_standin.py`. Maps: `Resources/Print/AlarmTell/alarm_zone_card_{albedo,glow}.png`, 2032 × 1216 (8 px/mm). 平面视觉's art replaces them in place.
- **Light:** emission only (glow map × 1.2). The glow map is the legend at full white and the red ground at 0.75. In a dark corridor the legend floats on a dim red panel. In a lit Office it reads as a red box with a lit face. It is a sign, not a lamp: no realtime light, no spill, so it adds nothing to the map-lamp leak problem (`run_calls/10` §7).
- **Why it cannot read as EXIT:**
  - EXIT: red letters on a pale face, in a box over the door or hung from the ceiling.
  - The card: a white legend on red, flat on the wall at 60 in beside the opening, never over it, never with an arrow.
- **Legibility (arithmetic, 1080p, 76° vertical FOV, 14.2 px per degree):** the 41.3 mm cap is 16.8 px at 2 m, 6.7 px at 5 m and 3.4 px at 10 m. It reads from about 5–6 m. The red box itself reads much further. 平面视觉 may ask for a larger card (51 §2).

### 3.4 Placement (`Dress`)

Entrance frame: the floor point at the opening's centre, on the wall's centre plane, +Z into the trigger room.

- **Card:** on the outside face (z = −t/2), centre 1.524 m. It sits beside the opening at x = ±(w/2 + 50.8 mm frame face + 100 mm gap + 143 mm), so 0.794 m for a 1.0 m door. It faces out. `cardSide` +1 = the approacher's right. Choose the latch side when there is one (ADAAG 4.30.6). A double-acting door has no latch side, so the right is the default.
- **Detector:** on the ceiling, on the opening's centreline, LED toward the entrance. Its depth is the **sight rule**. An eye at 1.62 m (`ModuleUnits.PlayerEye`), 2 m outside the wall, must see the red ring (20 mm under the ceiling) over the door head's inner edge. Then 0.1 m is added. The result is clamped to 1–6 m past the inner face, and 0.6 m short of the far wall when the room depth is given.

| Entrance | Head | Ceiling | Sight z | Detector z | Seen from 2 m |
|---|---|---|---|---|---|
| Door (`DoorHeight`) | 2.1 | 2.9 Standard | 3.59 | **3.69** | yes |
| Arch (`ArchTop`), a Run! room's south edge (CR-1) | 2.2 | 2.9 | 2.61 | **2.71** | yes |
| Door | 2.1 | 2.4 Low | 1.34 | **1.44** | yes |
| Door | 2.1 | 5.4 Tall | 14.8 | **6.08** (clamped) | **no**: the card carries the tell; `SeenFromOutside` false |
| Door into a 2.5 m deep room | 2.1 | 2.9 | 3.59 | **1.98** (clamped) | **no** |

So the detector is past the 1.5 m arm line in every Standard case. That is the rule's own geometry: from 2 m out, through a 2.1 m head, the ceiling becomes visible only beyond about 3.6 m. Placement wants a 0.25 m clear disc of ceiling at that point (no tray, main or EMT over it).

### 3.5 Run! rooms (C38–C43)

- The south arch is the entrance. The goal door (RN-F + F5, C38) is an entrance for anyone who comes from the far side. Each takes its own `Dress`.
- B's overhead services (`run_calls/10` §8: sprinkler main 2.62 m, tray 2.70 m, EMT 2.72–2.80 m) must leave the detector's disc clear at z 2.71 on the centreline. That is the rig's layout job when B is rebased.
- B's NOTICE plate stays. The card does not replace it. They sit on opposite sides of the arch where the wall allows.
- No EXIT anywhere inside (C41): the card is outside the room, and it is not an EXIT sign.
- The F5 goal lite and the rig-held light (C42) are unaffected. The card adds no light.

### 3.6 Tiers and WebGL

- **Desktop:** everything above.
- **Handhelds (iPhone, iPad):** `ReducedTier` drops the detector's LOD0, so LOD1 (1,210 tris) shows from the near plane. Everything else is identical: the card, the LED's blink, every emission (parity). Desktop output does not change: the gate is `FrontRoomsHandheld.Active`, false on Mac and Windows.
- **WebGL:** the same reduced tier (`UNITY_WEBGL && !UNITY_EDITOR`). WebGL dresses the tell although it skips most decor props. **Decision:** the tell is gameplay information, not decor. A WebGL trigger room without it would break RELAY §6. Cost per entrance at the reduced tier: about 1,250 tris and 6 draws, no light, no shadow.

### 3.7 Cost per entrance (estimate)

| Tier | Detector (near) | Card (near) | Total |
|---|---|---|---|
| Desktop, within 3 m | 8,590 tris, 4 draws | 734 tris, 3 draws | ≈ 9.3 k tris, 7 draws |
| Desktop, 3–9 m | 1,210 tris, 4 draws | 734 / 46 tris, 3 / 2 draws | ≈ 1.3–1.9 k tris, 6–7 draws |
| Handheld / WebGL | 1,210 tris, 4 draws | as desktop | ≈ 1.3–1.9 k tris, 6–7 draws |

The per-frame CPU cost is the LED clock: three float compares per frame, plus three `SetColor` during a blip.

## 4. The API (`Assets/Scripts/Rendering/AlarmTell/FrontRoomsAlarmTell.cs`)

```csharp
GameObject FrontRoomsAlarmTell.Dress(Transform entrance,
    float openingWidth = ModuleUnits.DoorWidth, float wallThickness = ModuleUnits.WallThickness,
    float ceilingHeight = ModuleUnits.StandardCeiling, float doorHeadHeight = ModuleUnits.DoorHeight,
    int cardSide = 1, float roomDepth = 0f, float detectorDepth = -1f);   // root "ALARM ZONE tell" under the entrance
GameObject FrontRoomsAlarmTell.DressCeiling(Transform parent, Vector3 localCeilingPoint, float yaw);  // detector only
GameObject FrontRoomsAlarmTell.DressCard(Transform parent, Vector3 localWallPoint, float yaw);        // card only
float FrontRoomsAlarmTell.DetectorDepth(float wallThickness, float ceilingHeight, float doorHeadHeight, float roomDepth = 0f);
float FrontRoomsAlarmTell.SightDepth(float wallThickness, float ceilingHeight, float doorHeadHeight);
bool  FrontRoomsAlarmTell.SeenFromOutside(float z, float wallThickness, float ceilingHeight, float doorHeadHeight);
Vector3 FrontRoomsAlarmTell.CardPoint(float openingWidth, float wallThickness, int cardSide);
GameObject FrontRoomsAlarmTell.Find(Transform entrance);   void Undress(Transform entrance);
void FrontRoomsAlarmTell.SetAlarm(GameObject dress, bool on);  // OPTIONAL period latch; no caller
bool FrontRoomsAlarmTell.ReducedTier;  bool ForceReducedForTools;  const bool CardFaceIsStandIn = false; /* final since 2026-11-01: art 10-31, wording signed in narrative/90 section 2 */
```

- **Inert:** nothing in main calls it. There is no `RuntimeInitializeOnLoadMethod`, no scene object and no hook. The only thing that ever runs is what a caller starts.
- **For 关卡设计 (contract note, not an edit):** when CR-2's trigger rooms exist, call `Dress` once per entrance when the chunk is built. The dress is a child of the entrance transform, so `ChunkDropped` takes it away. Pass the arch's `ArchTop` as `doorHeadHeight` for doorless entrances. Placed by coordinate like the room (RELAY §10), so a revisit shift never moves it.
- **For the Run rig:** none. The tell does not reference the rig, and the rig need not reference the tell.

## 5. Not done, and why

- **The alarm latch is not wired.** A real head's LED goes steady in alarm. `SetAlarm(dress, true)` does exactly that (steady, never blinking). It needs the trigger owner's call on `RoomTriggered` and `RoomReset` (CR-2). It is offered as an option for 系统设计: a steady red LED after a trip is a readable "you did this" that is not a radar. Without the call, the LED keeps its standby blip whatever happens.
- **No EXAMINE close-up** on the card or detector. A close-up on the card ("ALARM ZONE · this room calls the panel") could teach the tell. That is 交互设计's and 叙事's call.
- **No dead-circuit variant** (RELAY §10, v2.1: a detector hanging by its wires, an OUT OF SERVICE tag). The kit's base and head are separate lathes in the generator, so a hanging head is a small later module.
- **No sound.** The detector chirp on a trip is 声音设计's (RELAY §13 item 11). Nothing here plays or asks for audio.
- **No render.** Per Red's rule, the first look is the joint test round.

## 6. Files (all new paths; nothing in main replaced)

| Path | What |
|---|---|
| `Tools/Blender/frontrooms_kit/assets/alarm_tell_common.py`, `alarm_detector.py`, `alarm_zone_card.py` | Kit generators (Blender 4.3, `build_asset.py … --no-preview`) |
| `Tools/lookdev/gen_alarm_card_standin.py` | Stand-in face generator (Pillow, TeX Gyre Heros Bold) |
| `Assets/Resources/Props/Models/Kit_AlarmDetector.{fbx,json}` (+ .meta) | Detector kit |
| `Assets/Resources/Props/Models/Kit_AlarmZoneCard.{fbx,json}` (+ .meta) | Card kit |
| `Assets/Resources/Surfaces/Alarm_{Shell,BandRed,Grille,LED,CardFace}.mat` (+ .meta) | Slot materials (FrontRooms/Surface) |
| `Assets/Resources/Print/AlarmTell/alarm_zone_card_{albedo,glow}.png` (+ .meta, folder .meta) | STAND-IN face |
| `Assets/Scripts/Rendering/AlarmTell/FrontRoomsAlarmTell.cs`, `FrontRoomsAlarmTellLed.cs` (+ .meta, folder .meta) | API and LED clock |
| `Assets/Editor/Rendering/FrontRoomsAlarmTellTests.cs` (+ .meta) | Edit-mode checks for the joint round (ASCII, no tuples) |

## 7. For the joint test round (after every package lands)

1. `-executeMethod FrontRoomsAlarmTellTests.RunBatch` (writes `Verification/alarm-tell-tests.json`): kits, LODs, slots, placement, sight rule, no collider, light or MonoBehaviour, reduced tier, the LED's blip.
2. Look, once, with a dressed entrance in a dark B corridor and in a lit Office:
   - the card's legend between 0.6× and 1.0× a lit Office wall's luminance (the exit-sign spec's calibration method, `exit_sign/10_spec.md`); the gain is the material's `_EmissionColor` 1.2;
   - the red annulus readable from 2 m outside;
   - the LED blip noticeable at 2–3 m but never the first thing seen;
   - LOD0 at 0.3 m (fins, LED bezel, rings);
   - the card never read as EXIT by a fresh player.
3. A phone frame on the reduced tier: LOD1 at 1 m holds.

## 8. Sources

| # | Source | Used for |
|---|---|---|
| S1 | System Sensor, Wikipedia: https://en.wikipedia.org/wiki/System_Sensor | Founded 1984, Pittway |
| S2 | System Sensor 1400 Installation and Maintenance Instructions (D400-07-00, I56-279-05R): https://www.manualsdir.com/manuals/142329/system-sensor-1400-1400-direct-wire.html | Ø140 × 80 mm; LED blinks every 10 s, steady in alarm |
| S3 | System Sensor 2400 / 2400TH Installation Manual: https://www.manualsdir.com/manuals/352788/system-sensor-2400-and-2400th-direct-wire.html | 10 s standby blink; i3 heads blink green every 5 s (post-era) |
| S4 | Notifier / System Sensor 1451 data sheet (DN-1154:A): https://www.manualsdir.com/manuals/410592/notifier-1451-4-wire-1451-2-wire.html | Plug-in head on B401B base; two LEDs blink in standby |
| S5 | System Sensor B401 base manual (I56-338-09R): https://efobasen.efo.no/mime_root/652/B401%20-%206255514.pdf ; https://www.manualslib.com/manual/1577070/System-Sensor-B401.html | Base and compatible heads |
| S6 | Ingenium, Detector, smoke (Simplex, 1980s–1990s), 1998.0297.001: https://collection.ingenium.ca/en/id/1998.0297.001 | Buff casing, black base, 15.9 × 7.5 cm |
| S7 | Siemens, Fire safety history (Cerberus): https://assets.new.siemens.com/siemens/assets/api/uuid:06c0c42a-4a3b-4e9e-aae8-e0bc29ed52d6/Siemens-fire-safety-history-EN.pdf | F3 (1951), alarm indicator in the base |
| S8 | System Sensor RA400Z manual: https://www.manualsdir.com/manuals/352713/system-sensor-ra400z-and-ra400za.html ; RA100Z: https://www.protegis.com/products/system-sensor-ra100z | Remote alarm LED on a single-gang box |
| S9 | System Sensor 5600 heat detectors manual (I56-2175-003R): https://www.manualsdir.com/manuals/352805/system-sensor-5600-series-heat-detectors.html | Box options |
| S10 | Siemens (Pyro) DB-3S base listing: https://www.protegis.com/products/siemens-pyro-db-3s | DI-3 series base |
| S11 | Gamewell 3315-088892A DI-3: https://mbglick.com/shop/gamewell-3315-088892a-di-3-ionization-smoke-fire-detector/ | Same head under another brand |
| S12 | NFPA 72 (2002), National Fire Alarm Code (copy at https://pozhproekt.ru/nsis/nfpa/NFPA72-2002.pdf): Origin and Development; §4.4.6.1; §5.6.2.1.1–5.6.2.1.2, Table 5.6.2.1.1 | The 1993 consolidation (72E-1990); zone of origin; the colour code and the ring |
| S13 | Mike Holt Forum, "Fire alarm device labeling": https://forums.mikeholt.com/threads/fire-alarm-device-labeling.120821/latest | Device labels by panel, loop or zone and device (anecdotal) |
| S14 | Engraved phenolic nameplates (Pipemarker): https://www.pipemarker.com/docs/23-EngravedPhenolic_Nameplates.pdf | Laminated engraved plates; colour per system |
| S15 | Embossing tape, Wikipedia: https://en.wikipedia.org/wiki/Embossing_tape ; Dymo Corporation: https://en.wikipedia.org/wiki/Dymo_Corporation | Dymo tape from 1958 |
| S16 | Science Museum Group, Kroy 190 printer for adhesive tape labels, c. 1987: https://collection.sciencemuseumgroup.org.uk/objects/co38597 | Lettering tape in offices |
| S17 | Silver Fox, What is traffolyte: https://silverfox.co.uk/blogs/news/what-is-traffolyte-guide-to-traffolyte-labels | Engraving through to the core; made to order |
| S18 | Annunciator panel, Wikipedia: https://en.wikipedia.org/wiki/Annunciator_panel | Back-lit engraved windows |
| S19 | Health Physics Society, Ask the Experts q12911: https://hps.org/publicinformation/ate/q12911/ | X-ray room warning lights and signs, by state |
| S20 | South Carolina Code of Regulations 61-64 RHB 8.13: https://regulations.justia.com/states/south-carolina/chapter-61/subchapter-61-64/part-viii/section-61-64-viii-rhb-8-13 | A warning signal seen from each entrance |
| S21 | 1stdibs, very early X-ray building sign: https://www.1stdibs.com/furniture/folk-art/signs/very-early-x-ray-building-sign/id-f_16822592 | A lit sign over the X-ray room door |
| S22 | ORAU Health Physics Museum, Tritium exit sign (ca. 1970s): https://orau.org/health-physics-museum/collection/radioluminescent/tritium-exit-sign.html | Self-luminous signs (rejected) |
| S23 | Federal Register 76 FR 76599 (2011-12-08): https://www.govinfo.gov/content/pkg/FR-2011-12-08/html/2011-31523.htm | Over 2 million tritium exit signs sold |
| S24 | ADA Accessibility Guidelines (1991): https://www.access-board.gov/adaag-1991-2002.html §4.30.5–4.30.6 | Latch side, 60 in to the centreline, non-glare |

Media wanted (not downloaded): `media_candidates.md`, section "50 ALARM ZONE tell".
