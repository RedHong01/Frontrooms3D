# GD3c: open calls for Red (broken glass stays, kept progress)

These are from the 游戏视觉 workflow `glass-debris-progress`, build stage, 2026-10-22 15:2x. The full report is `20_build.md`. Nothing below blocks the package from landing. Each call changes only how one small moment looks or feels.

## Call 1: a let-go just before a beat (gameplay timing, with 关卡设计 for the shot)

**Background.**
- With kept progress, a pane can now be left at any point of the 1.0 s hold.
- The cracks land at fixed beats: 0.35 s (Crack1), 0.70 s (Crack2) and 1.00 s (shatter). These are the sound chat's beats and do not move.
- The camera's swing-in (blend) takes 0.25 s at the start of every press.
- So a pane left at 0.34 s cracks 1 frame after the next press. At that moment the camera blend is at about 1 %: the crack and its sound land with no visible wind-up and no camera hit.

**Options, as the player feels them:**
- **(a) Accept it.** The rare resume a frame before a beat looks like the glass "cracks by itself" as you press. Cost: none.
- **(b) Commit the swing (recommended).** If you let go while a strike is already in flight (the last 60 ms before a beat), that strike still lands. A 0.30 s tap leaves S1 instead of a smear. It feels like a body: a swing cannot stop 1 cm from the glass. Cost: a small map-side rule (关卡设计), plus one map test. It changes gameplay timing at the margin, which is why it needs your yes.
- **(c) Replay the wind-up.** After a resume, progress holds still for up to 0.15 s while the camera swings in again; then the beat lands with full weight. Cost: the next beat comes up to 0.15 s later; the beats themselves do not move.

**Recommendation:** (b). It is also the only option that gives a sub-0.35 s knock a real crack.

**What it blocks:** nothing. Without a call, (a) is what the build does.

## Call 2: two new looks (VL364, WAIT-RED)

**Background.** In the current design no strike touches the glass before 0.35 s: 0–0.29 s is the brace and wind-up, and the first strike lands at Crack1. So below 0.35 s, the only honest trace is where the forearm braced on the pane.

**What was built:**
1. **The sleeve wipe below Crack1.**
   - A smear of dragged grime about 20 × 7 cm, just below the impact. It is half strength at 0.10 s of kept hold and full at 0.20 s.
   - It is lit like dust, so it is dim in a dead-lamp room and never glows.
   - It is honest but faint: visible at 0.6 m (3,928 px > 8/255), gone at 1.2 m face-on. VL364 shows it at 2× with the difference ×8.
   - Options: keep it as it is; make it stronger (it starts to read as a painted patch); or switch it off (`SleeveWipe = false`), in which case a knock under 0.35 s is kept but stays invisible until the next press.
2. **The S1 fin twist.**
   - Each first-crack segment turns 10–20° through the glass's thickness, as real cracks do, so part of it always catches the light.
   - At 1.2 m face-on, S1 went from 74 px to 647 px > 16/255: 2 bright lines plus about 4 faint ones.
   - Options: keep it; or go back to flat cracks (S1 then reads as plain glass from where you stand).

**Recommendation:** keep both, and choose (b) in Call 1 for real legibility below 0.35 s.

**What it blocks:** nothing. Both looks are in the package; either is one switch.

## Not a call, but you will see it: F4, pale shards under a bright ceiling

- Looking through the broken Tall Halls window at the far carpet, a few large flat shards read as pale, nearly opaque pieces (VL361 last tile, VL363).
- In the other rooms tested they read as glass.
- The most likely cause is the reflection of the lamp-lit ceiling in the flat glass. The crushed rim, the dust, the haze and the smudges were each switched off and ruled out.
- It is an open defect on the 游戏视觉 side (`20_build.md` §6, item 1); there is no decision for you here.
