# The WXRM loop: content for the beacon and the title TV (tutorial S1)

From 平面视觉 · 广告, 2026-10-22, for 声音设计's S1 (`research/tutorial/20_requests.md`). Red approved the tutorial in the 教程设计 chat on 2026-10-22: "全部按推荐（T1=A，T2=E1，T3 静止屏，T5 去掉文字卡，T6 标题电视带声音）".

The picture stays **Ad_02**, unchanged: `Assets/StreamingAssets/FrontRooms_Ad_01.mp4`, sha1 058d2e28, 640 × 480, 30 fps, 16 s, hard cuts.

This note says what plays when, so the sound lands on the picture. The sound design itself is 声音设计's.

## 1. The picture, second by second

| Time (s) | Beat at 120 BPM | Card | What is on screen |
|---|---|---|---|
| 0.0–3.0 | 0–6 | 1 · Store + sale | Chevron mark, "Front Rooms" script, FURNITURE; super BACK ROOM CLEARANCE |
| 3.0–7.0 | 6–14 | 2 · Product + price | 3-PC SOFA SET, the sofa photo, NOW $499, WAS $899, 90 DAYS SAME AS CASH |
| 7.0–10.0 | 14–20 | 3 · Call | CALL 555-0147, OPEN 9 TO 9 · 7 DAYS, FREE DELIVERY |
| 10.0–14.0 | 20–28 | 4 · Sign-off + tag | The lockup, ALWAYS MORE IN THE BACK, the address tag |
| 14.0–16.0 | 28–32 | Station ID | The 61 ring, WXRM-TV, TRI-COUNTY |
| 16.0 = 0.0 | 32 = 0 | Loop | Back to card 1 |

At 120 BPM every cut falls on a beat: 3, 7, 10 and 14 s are beats 6, 14, 20 and 28. The loop is 32 beats, 8 bars of 4/4.

## 2. What to play when (proposal)

- **0–14 s: the FRONT ROOMS jingle** under the four store cards. A hit on each cut (3, 7, 10, 14 s) marks the card change, as in the promo.
- **14–16 s: the WXRM ident** under the station-ID card: one bar.
- **The loop seam (16 → 0):** the ident's tail rings into the jingle's downbeat. No gap, no fade.

**Starting material:** the promo's synthesised stems.
- `Tools/screens/promo_audio.py`: `compose()` writes the ident and jingle MIDI; `midi_render.swift` renders it through the system GS synth.
- Nothing in it was downloaded or licensed from a library.
- Mix notes: `Research/week03/instagram_promo/MIX_NOTES.md`.

## 3. Changes S1's rules require

1. **The ident uses a glockenspiel.** In `compose()`, channel 1 is set to GM program 9 (Glockenspiel) for the four-note arpeggio, and S1 forbids bell, glock and chime timbres (the lock-on chime must stay the only tonal chime). Move that line to a non-bell voice: synth brass (GM 62, which the ident already uses on channel 0), synth strings (GM 50) or a saw lead (GM 81). The other ident voices are timpani (GM 47) and a crash cymbal (drum note 49), both fine.
2. **No alarm in the loop.** The promo had a 520 Hz temporal-3 alarm under its slate. It never goes into the beacon or title loop, and in 1990 the low-frequency 520 Hz signal didn't exist yet. Any alarm sound follows `AD_TYPE_SYSTEM.md` §4: a period building bell or horn, and never emergency-alert tones.
3. **No voice.** The promo has no announcer: the macOS TTS licence is unclear. The loop is music only.
4. **Far and near.** The jingle already has the far-band parts S1 asks for, a slap bass (GM 36) and a kick on the beat, plus a mid-range hook (synth brass GM 62, electric piano GM 4). Orchestra hits (GM 55) mark the cuts.

## 4. Picture rules that touch the sound
- The beacon is a broadcast, not a warning. It never uses alarm words, alarm tones or the alarm slate (C09 is a separate question for Red).
- When the station TV switches off (V2), the picture collapses to a dot. The sound to match is the power-off zip (S4); the loop stops on that frame.
