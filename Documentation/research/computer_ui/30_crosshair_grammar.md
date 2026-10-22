# Crosshair state grammar

*系统设计, 2026-10-15; amended the same day with 平面视觉 · 信息设计's HUD check (review page v17: build ≤ 1.05 s with the slow blink, the first-key eye at the crosshair, H2 on the map side only). This doc sets the rules: what each state means, when it shows, its priority, its timing envelope and how it stays honest. The looks and motion curves belong to 平面视觉 · 信息设计. The KeyHud registry and the build belong to 游戏视觉 (FrontRoomsKeyHud, contract HK1, DECISIONS #16). Map hooks are 关卡设计's; touch is the touch chat's.*

**Strings (A22, Red 2026-10-22):** the prompt verbs in this doc follow 平面视觉's approved table (`research/desktop_ui/FIX_CARD_VL217.md` §B): `E · READ PLAN / READ SCREEN / READ SIGN`, `E · EXAMINE KEY`, `E · TAKE KEY`, `LOCKED / NEEDS STAIR KEY`, `E · UNLOCK / STAIR KEY`, and BROKEN, then WON'T SHUT as line 2 on a shut try (C11).

**Sources:**
- the state inventory: workflow `crosshair-verb-grammar`, sweep 1, checked against main and the clones on 10-14;
- `research/inspect/10_design.md` (EXAMINE) and `20_design.md` (terminals);
- `RELAY_PURSUIT_REDESIGN.md` §8 (ThreatPerceivable), §9.5 (photosafety) and §10 (doors, broken doors);
- `hud_build/10_design.md` and `DECISIONS.md`;
- Red's pick, 10-15: the look-eye **E·1 "Opens from the dot"** (review page v16).

## 1. One rule above all

**The crosshair shows what E will do here, now. Nothing else.**
- A state appears only when the map, or EXAMINE, would act on E for the aimed target.
- The state and the prompt line come from **one predicate** (one `Describe` result), so they can never disagree. If they could, the gap between them would be a Relay detector.
- The crosshair never shows anything about the Relay. No state appears, changes or vanishes because of hidden Relay state. Only `ThreatPerceivable` (RELAY §8) may hide a close-up state, and only with its perceivable holds.
- Decoration gets no state: ambient monitors, dead TVs, piles, signs that are read in place, and the evacuation plan's frame when it is not an EXAMINE target.

## 2. The states

Each state is one entry in the KeyHud registry, with build / react / leave and a retrigger window (游戏视觉's framework). Times are envelopes; 平面视觉 · 信息设计 owns the curves inside them.

| State | Means | Trigger (`Describe` kind) | Build | E reaction (window) | Leave | Status |
|---|---|---|---|---|---|---|
| **Idle** | nothing to do | no kind | — | — | — | live (the dot) |
| **Use** | E opens or shuts this door | `Open`, `Shut` | none: the dot stays and the prompt shows | none | — | live (prompt only) |
| **Hold** | hold E here | `Glass` (later `Pry`) | none: dot, prompt, hold bar | the hold bar | — | live |
| **Locked** | E won't open it; you lack this zone's key | `Locked` | L3·D, 500 ms | D·3 tug and shake, **500 ms** window | 320 ms | designed (Red 10-14) |
| **Unlock** | your key opens it | `LockedHaveKey` | line 2 "KEY nn" | shackle and F·1 drop, 550 ms, latched | — | designed |
| **Broken** | it can't be shut, ever | `Broken` | B·4, 800 ms (complete at 440 ms, then the swing settles) | won't-shut swing, **550 ms** window | 300 ms | designed (Red 10-14) |
| **Read** (the eye) | E opens a close-up to read this | `Read` / `Examine` | E·1: opens from the dot; readable as an eye ≤ 0.25 s, prompt in ≤ 0.30 s; **one** slow blink ≤ 0.63 s, starting ≤ 0.45 s; total ≤ 1.05 s (a switch during the blink cuts it like any leave) | focus, then fade with the close-up's HUD fade (≤ 0.15 s); only when the close-up really opens | shuts into the dot, ≤ 0.30 s | picked (Red 10-15) |
| **Hidden** | the HUD is not yours right now | pause, caught, title, a takeover shot, inside a close-up | — | — | instant off; back in 0.9 s on play | live |

Notes:
- **Prompt timing.** The prompt line appears with the build, not after it (KeyHud `OwnsPrompt` may hold it at most until its own text alpha allows). A state that reads late is a state the player misses.
- **Unscaled time.** All motion runs on unscaled time (the KeyHud clock). A close-up pauses the world, and the HUD must still move.
- **Return from a close-up.** The eye fades in at rest, with no blink, within 0.15 s of control returning, if the aim is still on the target.

## 3. Read: the eye (Red's E·1)

**Trigger and gate.** The eye uses the exact predicate of the READ/EXAMINE prompt:
- the target's aim box is hit no more than 2 cm behind the physics hit (a nearer solid wins, so the target is not occluded);
- within 1.6 m and 60° of the face normal; it hides at 1.7 m / 65° (hysteresis);
- `CanShowInspect` holds: Playing, map play, no glass shot, climb or pull-clear, and the close-up pose solves;
- `ThreatPerceivable` is false, with its holds (RELAY §8).

**When the close-up can't open:**
- **Hard gate closed** (any condition above false): no eye and no prompt. The eye leaves with its normal 0.30 s shut, never with a special "denied" look. The holds make the moment of leaving coincide with something the player perceives.
- **Transient gate** (the E latch, the 0.30 s cooldown, a shot running): the eye stays at rest and E is swallowed. **No react**, because the focus plays only when a close-up really opens. There is no new "refused" vocabulary.
- **Warning stages 1–2:** allowed, and the close-up freezes them (EXAMINE §2.5 W). The eye shows normally.
- **Platform without the close-up** (the terminal on handheld before B-touch): `Describe` returns no `Read` there, so there is no eye. The glance still works.
- **Touch zoom not built (EXAMINE T3):** irrelevant to the eye. The close-up opens without zoom (EXAMINE D3 default).

**Targets in v1:**
- the evacuation plan;
- the first key in hand (`Examine`). During the EXAMINE §2.5 K hold, E opens the key close-up, so the dot becomes the eye **at the crosshair** as usual, with `E · EXAMINE KEY` in the normal prompt slot. No glyph is laid on the text. It leaves when the hold ends (move, look or 2.0 s). Knock-on for the HUD key (P4): on the first key, its take-off waits for the hold to end, or for HeldReturned plus the pocket beat, not a fixed 0.75 s;
- Office terminals (`Read`, phase B).

Door signs and number plates stay read in place (EXAMINE D7 default), so they get no eye.

**One glyph, one meaning.** The eye means only "there is something to look at closely". It is never used for detection, visibility or the Relay anywhere in the game or its HUD. The prompt verb carries the difference between targets (READ PLAN / READ SCREEN / EXAMINE KEY), not the glyph.

## 4. Priority

When several things want the dot, take the first applicable row:
1. **Hidden** (pause, caught, title, takeover, close-up).
2. **A latched reaction** inside its window: Unlock and Fall, the Locked tug, the Broken swing.
3. **What the aim ray hits first:** the nearest physics hit, unless an EXAMINE aim box lies within 2 cm behind it. On a door leaf the door's state wins: **Locked > Broken > Use**, because escape verbs must never be masked.
4. **Hold** targets (glass) when the pane is the nearest hit.
5. **Read** when its aim box is the hit.
6. **Idle.**

**A sign on a door** (平面视觉's case):
- The door state wins on the leaf. In v1, signs are read in place, so there is no conflict.
- If Red later wants sign close-ups, the rule is EXAMINE D7's alternative: a sign close-up only after one rattle, from 1.2 m, through the sign's own aim box. Never on the leaf's first E.

**Switching** (游戏视觉's default, made precise):
- When the target identity changes, the old state leaves, then the new one builds.
- If a new state is waiting, the old leave is cut to ≤ 0.12 s, so the new meaning lands within about 0.4 s.
- Re-aiming the same target mid-leave resumes from the current point. It doesn't rebuild and doesn't re-blink (§5).

## 5. Photosafety and reduced settings

- **The blink:** one slow blink ≤ 0.63 s, starting ≤ 0.45 s into the build, on build only. It never repeats while the target stays aimed.
  - A re-blink needs the aim to leave **and** ≥ 1.0 s since the last blink started. Otherwise the eye builds without a blink, so an aim jittering at the 1.6 m / 60° edge can't strobe.
  - At ≤ 1 blink per second the eye is far below RELAY §9.5's limit of 3 changes per second. It is also a small target, under WCAG 2.3.1's area threshold.
- **Reduce Flashing** (both platforms): no blink. The eye opens straight to rest within 0.30 s.
- **Reduce Motion** (touch OS setting or Camera motion 0 %): every state becomes an alpha cross-fade ≤ 0.15 s, with no blink, scale, swing or shake. This matches the touch rule's 108 ms cap where that is stricter. Desktop has no Reduce Motion row; Reduce Flashing covers the blink there.
- **No translucent ink** on the paper and keyline glyphs (Unity blends UI in linear light), and no meaning by hue alone. Every state is a shape.

## 6. Touch

- The USE button mirrors the state and its verb:
  - `Read` → **READ**; `Examine` → **EXAMINE**; `Locked` → LOCKED; `Broken` → BROKEN;
  - `LockedHaveKey` → UNLOCK; `Glass` → BREAK or TAP; `Open` / `Shut` → OPEN / SHUT.
- The eye and the other glyphs scale to the 12 pt dot.
- A tap in the look zone on a READ target opens the close-up, as a door tap does. Touch EXAMINE items T1 and T2 cover the exits.
- No haptic for the eye (no meaning needs it). Locked and Broken keep their designed haptics, and on iPad, with no haptics, their picture carries the meaning anyway.
- **Hook:** the touch layer must pick verbs from `Describe`'s kind, not by searching English strings (Mobile.cs ~288-294). Contract for 关卡设计 and the touch chat.

## 7. Hooks and fixes

| # | Owner | What |
|---|---|---|
| H1 | 关卡设计 | `Describe` returns a **kind** enum, with the string as data: `None, Open, Shut, Locked, LockedHaveKey, Broken, Glass, GlassTap, TakeKey, Read, Examine` (later `Pry`). EXAMINE's aim registry supplies `Read` / `Examine` through the same call |
| H2 | 关卡设计 | The HUD deny is already 0.50 s (D·3, DECISIONS). Only the map's rattle guard moves, `Rattle.Duration` 0.40 → **0.50 s**, so one press gives exactly one 3D rattle and one HUD tug |
| H3 | 游戏视觉 | KeyHud registry entry `Read` with E·1 and the §2 envelopes; `SetAim` gains `Read` / `Examine`; the react plays only on `InspectOpened` |
| H4 | touch chat | USE verbs READ / EXAMINE; verbs keyed on the kind (§6) |
| H5 | 系统设计 | RELAY §10 updated to B·4 (800 ms build, complete at 440 ms) on 10-15; 20_design.md §12 item 6 and the §10 Flashing row now allow the Read state and its single blink |

## 8. Tests

- **Agreement:** the eye shows on a frame iff the READ/EXAMINE prompt shows (100 seeds, scripted aim sweeps across the plan, terminals and the first key). 0 frames of disagreement.
- **Relay neutrality:**
  - Over a randomised 30-minute run, every eye leave falls within 0.1 s of either an aim change or a logged perceivable change: chase bed end, steps start or stop, blows, or the Relay entering or leaving the frame.
  - No eye change correlates with `SeesPlayer`.
- **Decoration:** 0 eyes on ambient monitors, TVs, piles, door signs and number plates across 100 seeds.
- **Strobe:** dither the aim across the 1.6 m / 60° edge at 10 Hz for 10 s. At most 1 blink per second, and 0 blinks with Reduce Flashing.
- **Priority:** a door leaf in front of a plan → the door state; a plan 1 cm behind a glass pane → Hold, not Read (2 cm rule).
- **Close-up:** E on the eye → the focus plays, and the HUD is gone within 0.15 s. On return the eye is at rest within 0.15 s, with no blink.
- **Playtest check (Red, 2026-10-22: E·1 kept, "全部按推荐" in the 平面视觉 chat).** On both forms (FR-PT04 questionnaire and FR-PT05 answer sheet, in the hand-read After play section; 印刷与演示 lays it out), one open item: *"If you saw the crosshair turn into an eye, what did you think it meant?"* It is conditional, so testers who never saw the eye skip it. It is open, not a tick list, so it never hints at the answer. Placed as After play item 05 on the REV. 10-22-26 drafts (FR-PT04 Figma 3512:7489 / 3512:7677, FR-PT05 3510:7479 / 3510:7787). The forms carry no coding boxes, because labels would hint. After the session the interviewer codes each answer, from the reader's `after_05` contact sheet, as **LOOK / READ** (something to look at or read: the intended meaning), **BEING SEEN** (something saw or spotted me), **OTHER** or **DIDN'T NOTICE**. Rule (accepted by 平面视觉, which owns the glyph, on 2026-10-22; `research/desktop_ui/FIX_CARD_VL217.md` §C): if 2 or more testers in one round, or 25 % or more of a round, code BEING SEEN, switch to E·3 (lowered gaze; parameters only). The form layout belongs to 平面视觉 · 印刷与演示.
