# Smoke-test hints for the print driver (wallpaper line → 游戏视觉, 2026-10-22)

**UPDATE 09:3x: the landing payload is now sha1 `2859c507`**, which replaces ef19ac88. The review wf_1691c1a5-666 confirmed 3 defects:
- ticks run only inside a map run; the title no longer jumps, and every run starts on K00;
- pause stops print time;
- BreakDoor no longer sets the hold.

- **Reduce Motion** is already wired to the game setting (`FrontRoomsSettings.CameraMotionPercent == 0`), not to the old PlayerPrefs key. It is not an open point.
- **In the title stream nothing jumps now:** `_FR_PrintClock.x` stays 0 with w = 1. The frame only moves inside a map run, after the 45–90 s hold. (Older note:) **Without a map run**, only ambient jumps run, and the first jump comes after a 45–90 s hold. So in a 120 s smoke `_FR_PrintClock.x` can stay at 0 for most of the run. That is expected.
  - Check `_FR_PrintClock.w == 1` and `.y == 8` instead, and that the frame moves after the hold.
  - To see a jump, run at least 150 s, or capture right after the first jump.
- **In a map run**, `WarnStageChanged(>=1)` or a Chase must fade w to 0 within about 1.2 s.
- **WebGL:** `Boot()` returns under `#if UNITY_WEBGL && !UNITY_EDITOR`, so no driver exists there and `_FR_PrintClock` stays unset.
