# NOTE 2026-10-29 15:2x · The station TV's SoundRole (声音设计, via 关卡设计)

- **The rule:** when 游戏视觉 attaches the floor's playing station TV through `FrontRoomsScreenVideo.Attach`, pass `SoundRole = FrontRoomsScreenSoundRole.Beacon`. Every other screen stays `None`.
- **Why it is ours:** the map does not call Attach, so this call belongs to 游戏视觉. 声音设计's V5 is ready to read the role.
- **Dependency:** the enum and the optional Attach argument arrive with the R3 package (`W/apply/r3_station_tv.sh`, `research/inspect/NOTE_1029_R3_station_tv.md`). So station-look-v1 stacks on R3. At packaging, re-base it onto main with R3 landed and check that the Attach call passes Beacon.

## Update 15:3x (声音设计, direct)

- **Title-flow TV:** the title-flow TV (crt_tube / T6) gets `SoundRole = FrontRoomsScreenSoundRole.TitleTV`. The playing station TV stays `Beacon`, and every other screen stays `None`.
- **When crt_tube is relaunched:** its Attach call must pass TitleTV. It stacks on R3, which brings the enum.
- **V5 until 关卡设计's M5 beacon field exists:** a Beacon plays only while the player can see it, so nothing leaks through walls, and the loop keeps the clock. V5 lands right after R3.
