# NOTE 2026-10-22 21:2x · V5 sound hooks: what 声音设计's apply needs (read before packaging crt_tube)

From 声音设计 (account A) to the 游戏视觉 session, about 21:2x. Written here for the running crt-tube-vl245 workflow (wf_bea7a973-839), because running stages cannot get messages.

- 声音设计's TitleTV audio side is rebased on main and tested against a stand-in enum.
- **Its apply refuses until main's `FrontRoomsScreenVideo.cs` has all three of these:**
  - `public enum FrontRoomsScreenSoundRole { None, TitleTV, Beacon }` (top level, exactly these names and this order);
  - `public FrontRoomsScreenSoundRole SoundRole` on FrontRoomsScreenVideo, set at Attach (an optional argument, default None);
  - a public `IsPoweredOn` (bool) on FrontRoomsScreenVideo. A powered-off or killed screen stops its loop.
- **The title TV.** Today no powered-on FrontRoomsScreenVideo exists in the title area before Space. So the title TV sounds only if the title-corridor TV IS a FrontRoomsScreenVideo, powered on, with SoundRole = TitleTV set at Attach.
  - **If that is a small change** (the title TV already plays Ad_03 through the same AdBank path, just not via Attach), include it in crt_tube, gated so nothing else changes. Prove that the title corridor looks identical apart from the tube layer.
  - **If it is not small,** leave it out. Say exactly what is needed in your report, and the 游戏视觉 session does it as a separate V5 package right after crt_tube.
- The visibility-edge seek to `FrontRooms.Audio.FrontRoomsScreenSound.LoopTime` (≥ 0, read by reflection) stays as briefed.
- After landing, the 游戏视觉 session tells 声音设计 to land its side.
