# R3 · The playing station TV: inspect definition (游戏视觉, 2026-10-29)

**Decision.** Red, 总控 B chat, 10-29 10:55 PDT: "全部按推荐" on the audit questions. The source is `Documentation/Takeover_2026-10-18/reviews/DECIDED_VS_LANDED_20261029.md` §6, row R3:

> Only the playing station TV shows the eye. E opens a close-up of the ad screen and the game pauses. Dark maze screens show no eye. The grammar rule "never on TVs" becomes "only the playing station TV".

**Owners.**
- 系统设计 owns the grammar text (`research/computer_ui/30_crosshair_grammar.md`).
- 游戏视觉 owns the inspect definition: this note, `Rendering/Inspect/*` and `Media/FrontRoomsScreenVideo.cs`.
- 关卡设计 owns the A26 hook that builds the station.
- 声音设计 owns the close-up mix.

## What the player gets

- **Aiming.** The TV playing the ad in the floor's station shows the E·1 look-eye opening from the dot, with READ. A screen is a readable (O-28: plan, sign, screen = READ; objects = EXAMINE).
- **Other screens show nothing.** A dark maze desk screen, a powered-off screen or a still desk screen shows no eye and keeps its own E (TURN ON / TURN OFF).
- **E on the playing TV** opens the close-up: the camera pushes in to the screen and the world holds (timeScale 0).
- **The picture keeps running through the close-up**, from where it is, with no restart. This answers 声音设计's ask (a): the voice keeps playing, so a frozen frame would contradict it.
  - Only the inspected screen's bank plays. Every other screen stays held (C26), and its sound pauses with it (声音设计's default).
- **Sound (声音设计, 10-29):**
  - the ad keeps its level as heard standing at the set, since EXAMINE attenuates from the body;
  - everything else takes the usual EXAMINE mix: Foley, mechanisms, music and the subjective layer paused, the Relay −6 dB, room tone −10 dB.
- **Esc** closes the close-up as for every EXAMINE, and the world resumes.

## Contract details

- **(b) Placement.** The TV's `FrontRoomsInspectable` sits on the object that carries its `FrontRoomsScreenVideo`, or on a child of it. Then `FrontRoomsInspect.Current.GetComponentInParent<FrontRoomsScreenVideo>()` finds the screen, and 声音设计 needs no new API.
- **The world hold (C26) gets one exemption.** `WorldHeld` stays true for every screen except the one being inspected. Its bank's `VideoPlayer` plays on a clock that runs at timeScale 0, and it is set back to the decided clock when the close-up ends. The E ray stays off during the close-up, so the E that leaves the close-up never toggles the TV.
- **Only while it plays.** The eye and READ exist only while the screen is the floor's playing station TV: powered on and not a still. Turning the TV off removes the inspect target, so the eye leaves.
- **And only before the floor key is taken** (关卡设计 10-29 15:3x, MapWorld main 527ddb39): gate on the bool `map.StationKeyIsTaken` (true after the take, false with no station), read through FrontRoomsStationPresence like `FloorHasStation`. The event `StationKeyTaken(Vector3)` may be used to react at once, but never alone, because it misses a take that happened before the reader subscribed. M3 makes the take raise both; until the group lands nothing in play raises them (station off). 关卡设计 lands no R3 hook and will strip any read-only "TV playing" property from its M3/M6/A22 package.
- **V5, appended in the same file** (agreed with 声音设计 10-22 15:2x and recorded in handoff/visual.md):
  - a top-level `public enum FrontRoomsScreenSoundRole { None, TitleTV, Beacon }`;
  - `public FrontRoomsScreenSoundRole SoundRole`, set at Attach through an optional argument.

  This lets V5 drop its stand-in enum.

## Not in this package

- **关卡设计's A26 hook:** marking the station's TV as the playing station TV, or calling the attach on it. That is a contract diff to them.
- **The grammar text change** belongs to 系统设计.
- **声音设计's V5** mix change, which leaves the Screen bus running for an EXAMINE of a sounding screen.
