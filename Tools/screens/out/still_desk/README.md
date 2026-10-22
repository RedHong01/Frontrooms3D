# Still desk screens (tutorial T3 → 游戏视觉 V4)

From 平面视觉 · 广告, 2026-10-22. Generator: `Tools/screens/still_desk.py`. To re-render:

```
python3 still_desk.py
```

**Why.** Red approved T3 in the 教程设计 chat on 2026-10-22: "T3 静止屏". On a floor whose key is untaken, the maze's Office desk CRTs stop playing the ad and show a still, dim office screen. The station TV (the beacon) is then the only screen that moves or makes sound. Spec: `Documentation/research/tutorial/10_proposal.md` §4.4 and `20_requests.md` V4.

## Files

Each screen comes as PNG, 640 × 480 (the same 4:3 master as the WXRM ad), plus `@2x` at 1280 × 960 for close views.

| File | Lines (叙事设计, `research/screens/30_screen_strings.md` §6) | Use |
|---|---|---|
| `desk_dos` | `C:\>_` (an underline cursor, not blinking) | Any desk |
| `desk_print_queue` | `PRINT QUEUE  1 JOB` / `PLAN SHEET A-3 OF 4` / `PRINTING` | Any desk |
| `desk_idle` | `SYSTEM IDLE` / `OCCUPANT LOAD 1` (a screen blanker's text, held still) | Any desk |
| `desk_alarm_log` | `ALARM LOG 03/90` / `ZONE 04  TROUBLE  ACK` | Any desk; static and never live, never the player's zone |
| `desk_print_queue_T2` | `PRINTING SHEET A-1114 OF 4` | Instead of `desk_print_queue` from tier T2, if the game can read the tier |
| `desk_idle_T4` | `OCCUPANT LOAD 2` | Instead of `desk_idle` from tier T4, if the game can read the tier |

`still_desk_sheet.png` is a review sheet only.

## How they should sit in the game (a request; the wiring is 游戏视觉's)

- **Still means still.** One texture per monitor, with no blink, flicker, scroll or VideoPlayer. Don't pick a new screen while the player can see the monitor.
- **Dim is in the art.**
  - The text is paper-white phosphor at about 60 % (150, 156, 160) on a near-black field (9, 10, 11).
  - Show the art 1:1. Don't let the material boost it, so the station TV stays the brightest screen on the floor.
  - Off sets (dark glass) are still fine for some desks.
- **Colour meanings are kept** (Information Design's terminal board TM04):
  - amber = the TROUBLE lamp;
  - green = glow-ink guidance;
  - VGA blue = held for a terminal you can READ (D10, Red's call).
  - So these idle screens are neutral white.
- **Mix the four T0 screens across a floor's desks,** seeded per desk so the choice is stable across visits. Order of weight: DOS and idle most often, the print queue sometimes, the alarm log rarely.
- **No tube effects are baked in** (no scanlines, curvature or vignette); only a faint phosphor halo. The tube layer is yours whenever it comes.
- **Text stays inside RP 27.3-1989 title safe** (x 66–574, y 48–432), so overscan never cuts it.
- **Reading distance.** Body cap height is 21 px at 480 lines, so the text reads only at walk-up distance (about 0.55 m at 1080p). From across the room they read as quiet screens, as intended.
