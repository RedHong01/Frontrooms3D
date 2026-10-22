# 10 · Design: FrontRoomsKeyHud (key B, crosshair lock L3·D, broken door B·4, pickup P4)

游戏视觉 build workflow, design stage, 2026-10-14 17:0x–17:3x. Nothing was built and no clone was made in this stage. This file is the design; the build stages implement it.

**Inputs read:**

| Input | Version |
|---|---|
| `hud_build/DECISIONS.md` | Read three times: <ul><li>16:50 at the start: #1–#7 decided, #8/#9 stubs;</li><li>17:04: **#10 F·1 Drop and #11 B·4 decided**;</li><li>17:17 before returning: **#12 withdrawn; #13 an open conflict over the unlock-shot fade, build parametric, default (b); #14 the unlock gesture moves to the panel key; #15 U·3 "Push, fold, click" decided.**</li></ul> This design applies all of them. The #8/#9 stubs are gone. |
| `hud_build/NOTE_deny_channels.md` | 16:41 |
| `lock_motion/README.md` | 17:04 |
| `~/FrontRoomsVisualWork/keyicon_motion/lab_template_v2.html` | Read 16:51–17:18. 平面视觉 is still editing it: K·1–K·4 and U·1–U·4 appeared at 17:18. Those are not decided, so this design does not use them. |
| `lock_motion/svg/Lock_*.svg` (18) and `design/svg/B_*_held.svg` (9) | as on disk |
| `lock_motion/contract_map_KP1.md` | as on disk |
| `research/ui_key_icon/held_pose/measure.json` | **Not present.** The P4 start factors stay parameters (§5.2). |
| Main | HEAD `0a9ee7f` plus its working tree, read 17:0x. Line numbers drift. |

## 0. Summary

- **One visual-owned component, `FrontRoomsKeyHud`** (`Assets/Scripts/Rendering/UI/`).
  - It owns everything drawn at the crosshair: the dot, the padlock (L3·D in, D·3 no key, has-key swing, F·1 drop, famOut leave), the broken door B·4, and both prompt lines while a lock or broken state shows.
  - It also owns the B key panel (rule, glyph, label, meta), the P4 flight and the U·3 unlock gesture on the panel key (#14/#15).
  - The has-key sequence's start is a parameter (#13):
    - (a) on the key-turn frame;
    - **(b) when the shot's HUD fade lifts, the build default**;
    - (c) exempt from the shot fade.
  - The panel gesture runs in (b) or (d): (d) is synced to the shot's beats, with the panel exempt from the fade.
- **The map tells it what the aim is on and when things happen.** That is 3 per-frame setters and 4 events. Gameplay stays in the map.
  - The map's `prompt` strings stay as they are, so the touch layer's parsing (`FrontRooms3DGame.Mobile.cs:288`) keeps working unchanged.
  - The new USE verbs UNLOCK and BROKEN are a separate touch contract (§6.4).
- **Drawing** (§3.2):
  - The padlock, dot and door are one UI quad drawn by a small SDF shader. Every edge is analytic, and the shader handles:
    - the iris's changing corner radii;
    - the constant-width shackle with butt ends;
    - partial-length strokes;
    - the 85 % keyline **as one group** (Unity UI has no group opacity; the shader takes the union).
  - Digits and the B glyph are sprites from our pipeline. Text is legacy `Text` with `Outline`.
  - A 9-slice + mesh fallback is specified in §7.3, with the defects it brings.
- **Time:** unscaled, with an Editor-only clock hook for the harness, the same pattern as `FrontRoomsTouchMotion.EditorClock`.
- **Pipeline** (§8): the masters are M/L/Z polylines only. We reuse the key-icon pipeline's own rasteriser, `iconlib.py`, under the venv Python:
  - 16× supersampled coverage, composited in linear light, straight alpha with RGB bleed;
  - its TrueType reader for the digits.
  - Two cross-checks:
    - the lab's own JavaScript, run headless with macOS `jsc`/JXA, for the curves;
    - QuickLook (`qlmanage`, WebKit) for the end-state masters.
  - Nothing to download.
- **Harness** (§9): one clone, `W/proj_hudkey`. It drives every state on the injected clock with `Time.timeScale = 0` and captures 60 fps sequences at 1920×1080, 1920×1200 and iPhone 16 Pro (`FrontRoomsHandheld.Force`). Backgrounds are lit L0, a dark cell and Office. It also writes numeric curve logs, diffed against the lab's JS.

---

## 1. What the game does today (survey)

### 1.1 HUD construction: `FrontRooms3DGame.BuildHud` (`FrontRooms3DGame.cs:1944`)

- **Canvas:** one `ScreenSpaceOverlay` canvas "Minimal HUD", **`pixelPerfect = true`**.
  - `CanvasScaler` ScaleWithScreenSize, match 0.5.
  - Reference 1920×1080 on desktop, 874×402 on handheld (`:1947-1949`).
  - So 1920×1200 has scaleFactor √(1200/1080) = 1.054, and iPhone 16 Pro (2622×1206) has 3.0, i.e. 1 canvas unit = 1 pt.
- **Crosshair:** `crosshairObject = Panel(... "HUD / Crosshair", centre, 32×32 desktop / 24×24 handheld)` with the `UI/HUD_Crosshair` sprite (64×64, dot 2:1, so Ø16 / Ø12 pt).
  - It has its own `crosshairHudGroup` CanvasGroup (`:1992-1996`).
  - Children:
    - `promptText`: Bayon (the `UiFont` name rule, `:1608-1613`) 20 / 17, paper, anchored at (0, −44 / −26), one line (`:1998-2000`);
    - the hold bar (−68 / −48.5);
    - 5 stamina segments (−92 / −23.5).
- **Key panel:**
  - `keyPanel = TypographyGroup("HUD / Key", bottom-left (72, 118) desktop / top-left (78, −72) handheld)` with `keyHudGroup` (`:2011-2012`).
  - Inside it: `keyImage` (40×22, `LoadHudSprite("UI/HUD_KeyGlyph")`, preserveAspect) and `keyText` (Bayon 20 / 11, "LEVEL 0 KEY") (`:2013-2020`).
  - `LoadHudSprite` (`:1630`) does `Resources.Load<Texture2D>` + `Sprite.Create(..., pivot centre, FullRect)`, so importer sprite settings are ignored.
- **The hint card** is `contextPanel` (dark `media` card, 90 % in game ≈ 77 % in Figma), bottom centre. It is not involved here.
  - The 591/614 baselines are the prompt lines under the dot (README §1), not the hint card.
  - The 10-07 locked-prompt card proposal is dead (DECISIONS #4, no card).
- **Fonts:** `InitializeFonts` (`:267-272`) loads `Resources/Fonts/IBMPlexMono-Regular`, `Bayon-Regular` and `SourceSerif4-Variable`.
  - **Courier Prime is not in `Resources`** (it lives in `Assets/Fonts/Period1990/CourierPrime/`), so the panel number needs a Resources copy (§7.1).

### 1.2 Per-frame update: `UpdateHud` (`:2300`) and `ApplyGameplayHudAlpha` (`:2394`)

- **Order:** `UpdateAim` runs inside `UpdateMapPlay` first: `prompt = map.Describe(hit.collider, out aimedHold)` (`:1376`), and E calls `UseDoor` (`:1398`). Then `UpdateHud()` runs at the end of `Update`.
- **Every frame:**
  - `crosshairImage.enabled = play` (`:2319`, also `SetPhase :2184`);
  - `promptText.text = play && prompt != null ? DisplayPrompt(prompt) : ""` (`:2323`);
  - `UpdateMobileTouchPrompt()`.
- **Key panel:**
  - `presentingKey = doorShot.Presenting(zone)` forces `keyPanelFade = 0`, then it fades in over `KeyPickup.PanelFadeIn` (`:2342-2343`).
  - `showKey = play && !inStartRooms && map.HasKeyFor(zone) && !presentingKey`, then `keyPanel.SetActive(showKey)` (`:2344-2351`).
  - **The panel GameObject is inactive for the whole presentation**, so a P4 flight cannot live under `keyPanel`.
- **Alpha:**
  - `hud = gameplayHudAlpha * (1 − rig.HudFade)`;
  - `crosshairHudGroup.alpha = hud`, `keyHudGroup.alpha = hud * keyPanelFade` (`:2397-2404`);
  - `gameplayHudAlpha` steps on `Time.unscaledDeltaTime` (`:2304`).
- **Handheld anchors** (`ApplyMobileSafeAreaLayout :2081`):
  - `keyPanel` moves to top-left `(max(78, safeMinX), −max(72, topInset))`, pivot (0, 1) (`:2127`).
  - The crosshair line (`:2131`) anchors the canvas root, so it is a no-op. The crosshair stays at the canvas centre, which is the 874×402 frame centre.

### 1.3 Touch layer and its LOCKED parse (`FrontRooms3DGame.Mobile.cs`, map-owned partial)

- `DisplayPrompt` (`:82-89`): on handheld it strips `"E  ·  "` → "OPEN DOOR", and rewrites "HOLD E" / "TAP E".
- `UpdateMobileTouchPrompt` (`:284`) parses the **raw map string `prompt`**, not `promptText`:
  - `LOCKED` → `UseKind.Locked` (`:288`);
  - else hold → Hold;
  - `SHUT` → Shut (`:292`);
  - `TAKE` → Take;
  - else Open.
- The USE button shows the verb from the kind (`FrontRoomsTouchControlsView.cs:670-675`: OPEN, SHUT, TAKE, LOCKED, BREAK, TAP). `UsePrompt.Label` is not drawn.
- `UpdateMobilePromptGlyph` (`:316`) puts a mini USE glyph left of `promptText`. It hides when locked **or when `promptText.text` is empty**.
- **Consequences for the seam:**
  - (a) Keep the map's `prompt` strings, and the parse is untouched.
  - (b) Blanking `promptText` while the KeyHud draws its own lines also hides the mini glyph, which is what the lab shows.
  - (c) A future broken prompt string containing "SHUT" would parse as **Shut**. That is why the touch contract in §6.4 exists.

### 1.4 Map facts that shape the state machine

- `Describe` (`FrontRoomsMapWorld.cs:2444`):
  - returns **null** for `door.broken`, `IsBeingBroken` and **`IsHeldForKey`**;
  - `LockedHere` gives `"LOCKED  ·  NEEDS THIS ZONE'S KEY"`;
  - a locked door whose key you hold gives `"E  ·  OPEN DOOR"`.
  - So on the frame after a key turn starts, the aim reads "none". The HUD must latch the unlock and ignore aim changes until its exit finishes.
- `Use` (`:2475`) returns:
  - `Rattled` for a locked door with no key, with a 0.40 s guard (`Rattle.Duration`);
  - `KeyTurn` for one you hold the key for (the door is held for the shot);
  - `None` for broken doors today.
  - `UseResult.Broken` is planned by 系统设计 (`RELAY_PURSUIT_REDESIGN.md` §10), together with a broken **prompt kind** ("Return a kind, not a string, so the HUD keys its state on it").
- **Key rule:** `KeyFor(door, from)` (`:2530`) is the zone the player stands in. So the padlock's identity is **the player's current zone**.
- **No zone identity exists in code yet.** There is no shape/colour/number per zone. `KeyLabel` is `ZoneName + " KEY"`, and `ZoneInfo` holds only id, height, theme and site.
  - The rule ("number from the zone hash; two zones sharing a door never share both shape and colour") is in `interactables/10_spec.md` §4.1 and the D1.6 `DressDoor(idS, idP)` / `DressKey(id)` plan.
  - **It is map-owned and must be added** (contract M-6).
- **The unlock shot fades the HUD.** `FrontRoomsDoorShot.cs:261` sets `hudUntil = Unlock.MoveLockEnd` (1.45 s), and `rig.HudFade` reaches 1 in `HudFadeSeconds` 0.15 s (`FrontRoomsCameraRig.cs:364`).
  - Left as is, the has-key swing (0.12–0.42 s) and the F·1 drop (0.56–1.11 s) would be invisible. See §4.4 and open point O-4.
- **Key pickup** (`FrontRoomsDoorShot.TickKey :541`):
  - it ticks on game dt;
  - KeyTaken fires at `Arrive` 0.25;
  - the key is held at viewport (0.65, 0.30), at depth `HeldDistance` 0.35 **clamped to the world** (`PlaceKey :515`);
  - it drops over 0.75–0.95, then `FinishKey`.
  - The P4 hand-off (KP-1) adds an event at `HoldUntil` and ends the presentation there.

---

## 2. The spec as built (Unity signs)

Units are 1080p px around the insert centre, **Unity y up**. Phone multiplies all crosshair geometry by **ls = 0.75** (16 px dot → 12 pt; lab `DEV.phone.ls`). Times are seconds of unscaled time.

### 2.1 Padlock geometry (lab `TAG`, `lockGeo`; checked against the masters)

| | rect | long | round |
|---|---|---|---|
| rim w × h, r | 39.33 × 20.01, 3.45 | 52.44 × 15.18, 3.45 | Ø 26.22 |
| body = rim − 2 (r − 1) | 37.33 × 18.01, 2.45 | 50.44 × 13.18, 2.45 | Ø 24.22 |
| insert | 27.6 × 13.11, 1.035 | 34.5 × 9.66, 1.035 | Ø 17.94 |
| keyline | rim + 2 (r + 1), #141414, one 85 % group | ← | ← |
| shackle half-width a | 8.5 | 10.5 | 6.5 |
| T = rim top above centre (where the leg meets the rim) | 10.005 | 7.590 | 11.385 (√(13.11² − a²)) |
| hide = a + 6.8 | 15.3 | 17.3 | 13.3 |
| number ink box w × h (Courier Prime, master) | 17.450 × 10.212 | 13.756 × 7.936 (sx 1.035) | 12.854 × 7.659 |

- All parts are centred on the insert centre, which is the pivot (40, 32) in the 80×48 box = Unity (0.5, 0.3333).
- **Shackle path** (closed k = 1; Unity y), stroke 2.6, keyline stroke 4.6, butt ends, round joins:
  - long leg x = −a, from y = T − 6 up to T + 4;
  - arc centre (cx, T + 4), radii (a·|k|, a), over the top;
  - free leg at x = −a + 2ak, from T + 4 down to T − 3.
  - **Clip:** nothing of the shackle or its keyline is drawn below y = T − 6 in lock coordinates. The clip is fixed to the body and does not move with the shackle offset.
  - The arc is a 48-segment polyline, like the masters.

### 2.2 Lock motion (port of `LOCK['L3·D']`, `DENY.tugShake`, `swingOpen`, `FALL.drop`, `fallOut`, `famOut`, `frameLock`)

| Phase | Clock | Channels |
|---|---|---|
| **IN** (from the frame the map first reports Locked) | u = t / 0.5 | <ul><li>body = lerpR((14, 14, 7) → body, eoc(clamp(u / 0.3)));</li><li>rim = body + (2, 2, 1);</li><li>insert = lerpR(body(u) → insert, eio(clamp((u − 0.3) / 0.35)));</li><li>shackle y = **−**hide · (1 − eob(clamp((u − 0.62) / 0.38)));</li><li>digit 1 at u ≥ 0.8, digit 2 at u ≥ 0.9;</li><li>text α = clamp(u / 0.6);</li><li>dot hidden.</li></ul> |
| **HOLD** | — | closed state, text α 1 |
| **NO KEY D·3** (`NoKey()`) | r ∈ [0, 0.5] | <ul><li>shackle y += **+**tug(r);</li><li>lock y += **−**recoil(r) (whole lock, shackle included, no scale);</li><li>lock x = text x = shake(r − 0.14, 0.36) (text x × ls on phone);</li><li>text α 1.</li></ul> `tug`, `recoil` and `shake` are verbatim (lab lines 336–338). No blinks, no dimming. |
| **LEAVE after no key** (`famOut`) | x = t / 0.32 | in(1 − x); text α = 1 − clamp(x / 0.6); the dot shows at the end |
| **HAS KEY, mode (a) / (c)** (`Unlocking()`; (a) is also the fallback when no shot fade appears within 3 frames) | r ∈ [0, 0.56] | <ul><li>shackle y = **+**5 · eoc(clamp(r / 0.15));</li><li>k = cos(π · eio(clamp((r − 0.12) / 0.3)));</li><li>text α = 1 − clamp(r / 0.12), then 0 for good.</li></ul> (c) = (a) with the crosshair root ignoring `HudFade`. |
| **FALL F·1** (DECISIONS #10), after (a)/(c) | f = r − 0.56 ∈ [0, 0.55] | <ul><li>shackle held open (lift 5, k = −1);</li><li>lock y = **−**½ · 560 · f²;</li><li>lock z-rotation = **−**10° · eio(clamp(f / 0.5)) about the **insert centre**;</li><li>scale 1;</li><li>lock α = 1 − clamp((f − 0.15) / 0.35);</li><li>**dot α** = clamp((f − 0.25) / 0.25);</li><li>text α 0.</li></ul> At f = 0.55: idle. |
| **HAS KEY, mode (b), the default** (DECISIONS #13; lab in-context `shot === 'b'`) | s = t since `Unlocking()`; t_ret = the first frame `shotFade` falls after it rose (1.45 s for a full shot, earlier on a cancel) | <ul><li>While the shot hides the HUD: lock hidden, dot 0, text α = 1 − clamp(s / 0.12).</li><li>From t_ret: padlock **already open** (lift 5, k = −1). Its alpha comes from the HUD group's own fade (1 − `HudFade`); **the lab's extra `op × clamp((s − 1.45) / .15)` is not ported**, because the CanvasGroup already does that fade.</li><li>F·1 with f = (t − t_ret) − 0.10. Dot 0 until f > 0, then F·1's dot ramp.</li></ul> **Echo option** (a 180 ms side-on turn before the drop): a parameter, default OFF, O-20. |

Easings (verbatim):
- eoc(t) = 1 − (1 − t)³
- eio(t) = t < 0.5 ? 4t³ : 1 − (2 − 2t)³ / 2
- eob(t, s = 1.7) = 1 + (s + 1)(t − 1)³ + s(t − 1)²
- springStep(t, z, f) as lab line 182

### 2.3 Broken door B·4 (DECISIONS #11; port `DOOR_BROKEN['B·4']`, `doorMake`, `doorFrame`, `doorLeaf`, `doorPhase`, `BT`)

**Geometry** (Unity y up, × ls on phone):

| Part | Drawing | Stroke / colour |
|---|---|---|
| Frame | inverted U (−7, −13) → (−7, 12) → (7, 12) → (7, −13) (total length 64), drawn to length fr · 64 from the first point, butt ends, round joins | paper 2; keyline 4 at 85 %, drawn first, same partial length |
| Leaf | lerpR(dot (16, 16, 8) → (11, 23.5, 0.8), m), centred at (0, −1.25 m) | keyline (+1 px) at 85 %, then paper |
| Knob | circle at (3.4, −2.2), r = 1.1 · max(0, kn) | ink #141414 at **100 %** |

- The leaf group (keyline, paper and knob together) is translated by (0, −dy), then rotated by z = **−θ** about the hinge (−5.5, +10.5).
- **Paint order:** frame keyline, frame, leaf keyline, leaf, knob. The leaf's keyline paints over the frame, as in the lab. These are separate opacities, not a union.

**Motion:**

| Phase | Clock | Channels |
|---|---|---|
| **IN** | x = t / 0.8 | <ul><li>m = eoc(clamp(x / .3));</li><li>fr = eio(clamp((x − .2) / .35));</li><li>kn = eob(clamp((x − .48) / .1));</li><li>act = (x − .55) · 0.8 for x ≥ .55;</li><li>θ = 12 · springStep(act, .3, 2.4), dy = 1.5 · eoc(clamp(act / .15));</li><li>text α = clamp(x / .6).</li></ul> |
| **HOLD** | — | θ 12, dy 1.5, m = fr = kn = 1, text 1 |
| **E** (`BrokenTry()`) | r ∈ [0, 0.55], **no retrigger inside the window** | θ = 12 − 9 · eoc(r / .09) for r < .09, else 3 + 9 · springStep(r − .09, .3, 2.6); dy 1.5; text 1 |
| **LEAVE** | x = t / 0.3 | <ul><li>undo = eio(clamp(x / .5)): θ = 12(1 − undo), dy = 1.5(1 − undo);</li><li>fr = 1 − eio(clamp((x − .3) / .5));</li><li>kn = 1 − clamp((x − .3) / .3);</li><li>m = 1 − eio(clamp((x − .5) / .5));</li><li>text α = 1 − clamp(x / .6).</li></ul> |

- The crosshair dot is hidden from the first IN frame to the end of LEAVE (lab `dot = 0`).
- **Two tiny steps are in the lab too, and are ported as is (O-8):**
  - IN ends at act = 0.36 with θ ≈ 11.6°, and HOLD is 12°;
  - E ends at r = 0.55 a little off 12°.

### 2.4 Text, dot and keyline

| Item | Desktop (1080p) | Phone (lab `DEV.phone`) |
|---|---|---|
| Line 1 | Bayon 20, paper, centred, baseline **591** (51 below the dot centre) | Bayon 17, baseline 239 (38 below the dot at 201 pt) |
| Line 2 | IBM Plex Mono 13, paper, tracking 0.2, baseline **614** (74 below) | Plex 11, baseline 259 (58 below) |
| Has-key line 1 | `E  ` + `·` (Plex Mono 16) + `  UNLOCK`: three runs on one baseline (Bayon has no `·`) | `UNLOCK` (the `DisplayPrompt` rule) |
| Text keyline | Unity `Outline`, #141414, distance 1 | **1 pt** (the lab does not scale the text keyline; the brief says × ls, see O-5) |
| Dot | white Ø16 + keyline circle Ø18 at 85 % | × 0.75 |
| Keyline alpha | 85 % in the lab (sRGB blend). In Unity (linear blend) the desktop-UI table says Unity 97 % ≈ Figma 85 %, so 0.97 is the starting value, **calibrated by the harness** (§9.4) | same |

### 2.5 Unlock gesture on the panel key: U·3 "Push, fold, click" (DECISIONS #14/#15; port `UNLOCK[2].pose`, lab line 802, and `swingKick`, line 792)

r = seconds since the gesture starts. Values are B-glyph px, × the panel scale (1 desktop, 0.85 phone).

| Channel | Formula | Applied to (Unity) |
|---|---|---|
| dx | 6 · eoc(r / .1) for r < .1; 6 until .42; then 6 · (1 − eio(clamp((r − .42) / .18))) | whole glyph x |
| dy | −sin(π c), c = clamp((r − .24) / .14) | whole glyph, **Unity y = +sin(π c)** (1 px up) |
| sy | 1 → .15 over .1–.28 (eio), .15 → 1 over .28–.42 (eio) | **key part only**, scaleY about glyph y 10 = Unity pivot y 0.7917 |
| swing | swingKick(r − .1, −9) + swingKick(r − .6, +6), where swingKick(t, a) = a · e^(−4t) · sin(2π · 2t) for t > 0 | ring+tag about (35, 10), Unity z = **−swing** |
| rule | 1 + .6 · sin(π c) | rule width, scaleX from its left edge (4 → 6.4 px) |

**Start of r:**
- mode (b), the default: t_ret, when the HUD returns;
- mode (d): `DoorBeat.KeyInsert` (`Unlock.InsertStart` 0.55 s), taken from the static `FrontRoomsDoorShot.Beat` event, which KeyHud subscribes to itself, with no map line.

**In (d) only:**
- the panel is exempt from the shot fade (`PanelShotExempt`) from `Unlocking()` until the shot's fade lifts;
- if the HUD returns with no `DoorBeat.BoltRetract` seen (a cancel before the commit), the channels ease back to rest over 0.15 s (eio).

### 2.6 Strings

One constant each (`FrontRoomsKeyHud.Strings`, written with `·` so the file stays ASCII):
- lock: `LOCKED` / `NEEDS KEY {0}` / `E  ·  UNLOCK` / `KEY {0}`;
- broken: `BROKEN` / `WON'T SHUT`;
- panel: `KEY` / `OPENS DOORS OUT OF THIS ZONE`.

---

## 3. Architecture

### 3.1 Components (all visual-owned)

| File | What |
|---|---|
| `Assets/Scripts/Rendering/UI/FrontRoomsKeyHud.cs` | MonoBehaviour. The API (§6.2), the crosshair state machine (§4), the panel and P4 (§5), text layout. LateUpdate only (it runs after the game's `Update`, so there is no one-frame lag). Clock: `Now`/`Delta` = `Time.unscaledTime`/`unscaledDeltaTime` (Δ clamped to 0.1 s), with `#if UNITY_EDITOR public static Func<float> EditorClock; public static float EditorStep;` |
| `…/FrontRoomsKeyHudMotion.cs` | Pure static ports of every lab curve: easings, `tug`/`recoil`/`shake`, L3·D in, swing, F·1, famOut, B·4 phases, P4 path, `simulate`, `heldSwing`; geometry tables. No scene state, so the harness can diff it 1:1 against the lab JS (§9.2). |
| `…/FrontRoomsHudVectorGraphic.cs` | `MaskableGraphic` that emits one quad and feeds a primitive list to the SDF material (§3.2). It draws the dot, the padlock and the B·4 door. |
| `…/FrontRoomsUITracking.cs` | `BaseMeshEffect` for letter-spacing (prompt line 2, panel meta: 0.2). It runs before `Outline`, so the keyline copies follow the tracked glyphs. Whitespace has no quad in legacy Text; the effect maps quads to visible characters. |
| `Assets/Resources/UI/KeyHud/FrontRoomsHudVector.shader` | UI SDF shader, `FrontRooms/UI/HudVector` (§3.2). It sits in Resources so builds include it (`Resources.Load<Shader>`, like `Brand/WhiteLogoUI`). |
| `Assets/Resources/UI/KeyHud/*.png`, `Assets/Resources/UI/Fonts/CourierPrime-Bold.ttf` (+ OFL), `Assets/Resources/UI/HUD_KeyGlyph.png` (replaced, GUID `eb266a1345aeb45f69bf39f9c6d34fb4` kept) | Parts (§7) |

### 3.2 Hierarchy and drawing

```
Minimal HUD (map's canvas, pixelPerfect)
├─ HUD / Crosshair (map)                       crosshairHudGroup
│   ├─ Key prompt, hold bar, stamina (map)
│   └─ KeyHud / Crosshair  [Canvas override: pixelPerfect OFF] [CanvasGroup ignoreParentGroups]
│       ├─ Vector  (FrontRoomsHudVectorGraphic: dot | padlock | door)
│       ├─ LockXform (follows the lock's x, y, z-rot, α) → Digit L, Digit R (Images, number atlas)
│       └─ Lines → Line1 runs (Text + Tracking + Outline) ×1–3, Line2 (Text + Tracking + Outline)
├─ HUD / Key (map; set inactive by the contract)
└─ KeyHud / Panel  [sibling right after HUD / Key; Canvas override pixelPerfect OFF; own CanvasGroup]
    ├─ PanelRoot (copies HUD / Key's anchor, pivot and position every LateUpdate: safe-area safe)
    │   ├─ Rule (Image 4 × 48, #F4DF3B; P4 scaleY pivot = its top, U·3 scaleX pivot = its left edge) ├─ Label: "KEY" Bayon 20 + number Courier Prime Bold 25 ├─ Meta (Plex 13 #BDBAB0, tracking 0.2)
    └─ FlightLayer (stretch) → Glyph (pivot = anchor (36, 24); P4 path, U·3 dx/dy)
            ├─ Swing (pivot (35, 10) = Unity (0.4861, 0.7917); P4 swing + U·3 swingKick) → RingTag Image → Number Image
            └─ Key Image (U·3 fold: scaleY, pivot y 0.7917)    [+ keyline layers under each, if O-1 says yes]
```

**Why an SDF quad and not 9-slice sprites:**

1. The iris lerps corner radii: body 7 → 2.45, rim 8 → 3.45, insert 2.45 → 1.035. 9-slice corners have a fixed size, and scaling them with `pixelsPerUnitMultiplier` drifts the AA width by up to ×2.9.
2. README requires the keyline as **one 85 % group**, and Unity UI multiplies alpha per element. The rim keyline and the shackle keyline overlap in a 1 px band at each leg, which would show at ≈ 98 % instead of 85 %.
3. A constant 2.6 px shackle seen edge-on (k → 0) folds back on itself. A feathered mesh self-overlaps there, while a distance field does not.
4. B·4's partial-length frame and butt ends are one line of math.
5. It is one draw call at any resolution and at the touch scale, with no texture memory.

**Shader** (based on the UI-Default structure):
- stencil props, `UNITY_UI_CLIP_RECT`/`UNITY_UI_ALPHACLIP`, `Blend One OneMinusSrcAlpha`, `#pragma target 3.0`.
- **Primitive list:** up to 16 primitives (rounded rect / circle / polyline with butt ends, partial length and clip half-plane), up to 64 points.
- **Per primitive:** a 2×3 inverse transform, half-width, colour (`SetColor`, so linear in this linear project) and opacity.
- **Union group:** primitives flagged K are combined by min-distance and composited **once** at the keyline alpha.
- **Coverage** = clamp(0.5 − d·pxPerUnit) (box filter). pxPerUnit = canvas scaleFactor × ls is passed in, so no derivatives are needed and WebGL2, Metal and D3D11 all work the same.
- **Compositing:** premultiplied, linear, painter's order. The result is multiplied by vertex alpha (CanvasGroup).
- **Upload:** uniforms through preallocated `SetVectorArray`/`SetColorArray` each LateUpdate. No Mask parent (masks clone materials).
- **If the shader fails to load,** KeyHud logs once and sets `OwnsDot = false`, and the map's dot shows as today.

**Quad bounds** (desktop units, × ls): x −48…+48, y −110…+40. This covers the shake, the open free leg (−3a = −31.5 for long), the 85 px F·1 drop and the B·4 swing.

**Digits** are sprites (§7.1) under `LockXform`, so they recoil, shake, fall, rotate and fade with the lock. They are revealed per glyph at u 0.8 / 0.9, as the README says.
- The lab reveals by clipping the number box at 50 %, which is the insert centre for "14". Per glyph is the README's intent and never cuts a glyph.

**Pixel snapping:**
- The map canvas is `pixelPerfect`, which would quantise the 0.6 px clack and 0.8 px recoil. KeyHud's two roots therefore override it (`Canvas.overridePixelPerfect = true, pixelPerfect = false`).
- To stay crisp at rest, KeyHud rounds its text and digit positions to whole screen pixels whenever their motion offset is 0.

**Baselines:** legacy Text has no baseline API.
- KeyHud computes each run's baseline from `TextGenerator` verts minus `CharacterInfo.minY` (`Font.RequestCharactersInTexture` / `GetCharacterInfo`), then offsets the rect so the baseline lands on 591/614 (239/259 phone).
- The harness checks ±0.5 px.

---

## 4. Crosshair state machine

### 4.1 States

`Idle(dot)`
- → `LockIn` → `LockHold` ⇄ `Deny` → `LockOut`
- → `Unlock` → `Fall` → `Idle` in modes (a)/(c)
- → `UnlockHidden` → (t_ret) `UnlockReturn` → (+0.10 s) `Fall` → `Idle` in mode (b)
- Panel, in parallel: `Rest` → `Gesture(U·3)` → `Rest`. In (d) only: `GestureEaseBack` on a cancel before the commit.
- → `BrokenIn` → `BrokenHold` ⇄ `BrokenReact` → `BrokenOut`

`OwnsPrompt` is true in every state except Idle while KeyHud's line alpha > 0.

### 4.2 Inputs per frame

`SetAim(aim, id, haveKey)`, where aim ∈ {None, Locked, Broken}:
- Idle + Locked → `LockIn`, t = 0 **on this frame**. Lab-only values are not ported: the 350 ms idle and the 550 ms open hold.
- Idle + Broken → `BrokenIn`.
- `haveKey` only picks the lines (`LOCKED / NEEDS KEY n` vs `E · UNLOCK / KEY n`).
- `id` picks the shape, colour and number.

### 4.3 Events

| Event | Accepted in | Effect |
|---|---|---|
| `NoKey()` | LockIn, LockHold | starts the D·3 window |
| `Unlocking()` | LockIn, LockHold, Deny | **latches** Unlock → Fall. SetAim is ignored until Fall ends: `Describe` turns null on the next frame (`IsHeldForKey`), and that must not start a LockOut. |
| `BrokenTry()` | BrokenIn, BrokenHold | starts the 550 ms window |
| any of them | other states | ignored |

The map only sends these for the door under the crosshair (contract M-4).

### 4.4 Shot fade (DECISIONS #13, open conflict; built parametric)

`FrontRoomsKeyHud.UnlockStart` ∈ {A_OnKeyTurn, **B_AfterShot (default)**, C_ExemptFromShotFade}, plus `PanelMode` ∈ {**B (default)**, D_ShotBeats}. Both are static and set once.

| Mode | Crosshair alpha | Lock motion |
|---|---|---|
| (a) | `gameplay × (1 − HudFade)` | swing + F·1 start on `Unlocking()`, so they play under the 150 ms fade, mostly unseen |
| **(b)** | same as (a) | §2.2 (b): the open padlock appears with the HUD's return and drops 0.10 s later |
| (c) | `gameplay` only from `Unlocking()` to the end of the fall, then as (a) | as (a), fully visible. The 2D padlock then overlays the shot's keyhole close-up at the screen centre. |

- **Panel:**
  - In (b), the panel follows `gameplay × (1 − HudFade)`, and U·3 starts at t_ret.
  - In (d), `PanelShotExempt` is true from `Unlocking()` until the fade lifts, so the panel alpha = `gameplay`, and U·3 starts at the KeyInsert beat.
- **t_ret detection:** KeyHud remembers the highest `shotFade` since `Unlocking()`; t_ret is the first frame `shotFade` drops below that peak. If `shotFade` stays 0 for 3 frames after `Unlocking()` (no shot: `BeginUnlock` returned early), mode (a) runs for both the crosshair and the panel.
- **No extra map line is needed for #14's `PanelShotExempt`:**
  - The KeyHud panel has its own CanvasGroup, and `SetHudAlpha(gameplay, shotFade)` already passes both factors.
  - The map's `keyHudGroup`/`keyPanel` is retired (M-2).
  - So the HK1 one-liner proposed in DECISIONS #14 is unnecessary under this seam. That is O-21.

### 4.5 Interrupt rules

The lab never interrupts a phase. These are 游戏视觉 defaults, chosen to keep every channel continuous; 平面视觉 may override them (O-7).

1. **Leave during LockIn at u₀:** run famOut from u₀ at the same rate, u = u₀ − t / 0.32. Text α = α₀ · (1 − clamp(t / 0.192)). For u₀ = 1 this equals the lab.
2. **Re-aim during LockOut at u₁:** LockIn resumes at u = u₁ + t / 0.5. Text α = min(clamp(u / 0.6), α₁ + t / 0.3).
3. **E during LockIn** (u < 1): the reaction starts now and the build finishes underneath. The channels add: shackle y = build + deny or lift; lock x/y offsets add.
4. **Leave during Deny:** the deny offsets run to the end of their 500 ms window while famOut starts at once.
5. **NoKey during Deny:** ignored (the window is its own guard). The map's rattle guard is 0.40 s, so a fourth press at 0.40–0.50 s rattles the door in 3D with no new HUD tug. See O-7.
6. **Identity change while not Idle** (rare: walls separate doors): treat it as a leave, then a fresh LockIn.
7. **Leave during BrokenIn or BrokenReact:**
   - m, fr and kn = min(value at leave, the LEAVE curve);
   - θ and dy = value at leave × (1 − undo);
   - text as in rule 1 over 0.18 s.
8. **The map's plain prompt and KeyHud's fading lines never overlap.** `OwnsPrompt` stays true until KeyHud's line α reaches 0 (at most 192 ms after leaving a lock, 180 ms after a broken door), so the map's next prompt waits that long.

### 4.6 Dot

- KeyHud owns the dot while attached, and the map hides its `crosshairImage` (M-2).
- Its keyline is the same exact ring as the lab's `kdot`. README suggests a Unity `Outline` on the dot, but the 4-tap diagonal Outline gives a 1.0–1.41 px ring at 85–98 %, and it would visibly change at the hand-off into the rim, which is the dot at u = 0.
- Dot alpha: 1 in Idle, 0 while a lock or door shows, F·1's ramp during Fall.

---

## 5. Key panel and P4

### 5.1 Layout

Desktop is the lab `PICK` / `DEV.desk`. Phone is the same panel × 0.85 at the panel's top-left (lab `DEV.phone.panel`).

| Panel units (from the rule's top-left) | Desktop | Phone (× 0.85, fonts on the touch ramp) |
|---|---|---|
| Rule | 4 × 48, #F4DF3B; grows from its top | 3.4 × 40.8 |
| Glyph top-left | (+18, 0), 72 × 48 | (+15.3, 0), 61.2 × 40.8 (slot (93.3, 72) pt) |
| Label baseline | (+104, +22): "KEY" Bayon 20 + number Courier Prime Bold 25 at +135.79 | Bayon 17 + Courier Bold **21** (21.25 is not a whole size, O-11) |
| Meta baseline | (+104, +41), Plex 13 #BDBAB0, tracking 0.2, **no outline** | Plex 11 |

- The panel anchors to the live `HUD / Key` corner: bottom-left (72, 118) on desktop, top-left safe-area position on phone. That corner is the rule's bottom-left on desktop and its top-left on phone.

### 5.2 P4 flight (`PickupFlight`, at `HoldUntil`; lab `DEV`, `P4`, `simulate`, `heldSwing`)

τ = time since take-off. τ₀ = shotClock − 0.75 compensates the frame the map fires on.

**Path** (canvas top-left coordinates, y down, as in the lab):
- s = eio(clamp(τ / 0.72));
- glyph top-left = bez(s, held − (36, 24) · S₀, ctrl, slot);
- scale = lerp(S₀, S₁, s), with S₁ = 1 on desktop and 0.85 on phone.
- **held** = viewport → canvas: (vx · W, (1 − vy) · H).
- **S₀** = factor × (H / 1080) × (0.35 / depth) × (tan 38° / tan(vfov / 2)):
  - factor = 3 until `held_pose/measure.json` lands;
  - that gives 1.117 on the 402 pt phone, which matches the lab;
  - when the measurement gives x/y factors, the anisotropy factor eases to 1 over the first 30 % of the flight (easing pending; default eio).
- **ctrl:** desktop (620, H − 120), with the lab's 960 kept as a distance from the bottom, because the slot is bottom-anchored; phone (300, 330) pt, top-anchored (lab `DEV`). Other aspect ratios: O-9.

**Swing** about (35, 10)·scale, Unity z = **−θ**:
- θ = heldSwing(0.5 + τ₀ + τ) + landing(τ);
- heldSwing(t) = 25 e^(−2.5t) sin(2π·2.2·t) from the hold start;
- landing = the lab's `simulate` on the pivot x = TLx + 35·scale: semi-implicit Euler at dt 1/480, acceleration by central difference h 1/960, f 1.7, ζ 0.12, normalised to an 18° peak over the 3.1 s after take-off.
- It is precomputed into a table at take-off, so it is deterministic.

**Text:**
- rule scaleY = eoc(clamp((τ − 0.68) / 0.16));
- label α = eoc(clamp((τ − 0.74) / 0.22)), x = −8 · (1 − α) panel units;
- meta α = eoc(clamp((τ − 0.84) / 0.22)).

**Rules:**
- While a flight is active, `SetHeldKey` for that identity does not pop the panel.
- After landing, `SetHeldKey(false)` hides the panel at once, as today.
- On the instant path (no presented model, which is shipped play today), there is no flight and the panel pops as today (O-10).

### 5.3 Unlock gesture

- U·3 (§2.5) plays on the resting panel glyph when the aimed door's `Unlocking()` arrives, started per `PanelMode` (§4.4).
- **A flight wins.** If a P4 flight is still running (a pickup and an unlock within about 1 s, which is rare), the gesture waits for the landing.

---

## 6. The seam

### 6.1 Principles

- The map decides everything about doors and keys. KeyHud only draws.
- `prompt` (the map's string) is unchanged, so touch parsing is unchanged.
- KeyHud never writes map-owned objects. The map hides its own `crosshairImage`, `promptText` text and `keyPanel`.

### 6.2 API (`FrontRoomsKeyHud`, visual-owned)

```csharp
public sealed class FrontRoomsKeyHud : MonoBehaviour
{
    public enum Shape { Rect, Round, Long }
    public enum Tint { Red, Blue, White }
    public enum Aim { None, Locked, Broken }
    [System.Serializable] public struct Identity { public Shape shape; public Tint tint; public int number; // 0..99
        public Identity(Shape s, Tint t, int n) { shape = s; tint = t; number = n; } }

    /// Build both roots. crosshair = "HUD / Crosshair" rect, keyPanel = "HUD / Key" rect. Returns null (and logs) if the canvas is null.
    public static FrontRoomsKeyHud Attach(Canvas hudCanvas, RectTransform crosshair, RectTransform keyPanel, bool handheld);

    // ---- every frame, from UpdateHud (after UpdateAim) ----
    public void SetAim(Aim aim, Identity id, bool haveKey);
    public void SetHeldKey(bool show, Identity id);
    // from ApplyGameplayHudAlpha
    public void SetHudAlpha(float gameplay, float shotFade);

    // ---- events (only for the door under the crosshair) ----
    public void NoKey();        // Use -> Rattled
    public void Unlocking();    // Use -> KeyTurn (the frame IsHeldForKey starts)
    public void BrokenTry();    // Use -> Broken (when 系统设计/关卡设计 add it)
    public void PickupFlight(Identity id, Vector2 viewport, float heldDepth, float verticalFov, float shotClock); // at KeyPickup.HoldUntil

    public bool OwnsPrompt { get; }   // the map writes "" to promptText while true
    public bool OwnsDot { get; }      // true once the vector shader loaded

    // ---- DECISIONS #13/#14: parametric until Red decides (visual-owned constants, not map calls) ----
    public enum UnlockStartMode { A_OnKeyTurn, B_AfterShot, C_ExemptFromShotFade }
    public enum PanelGestureMode { B_AfterShot, D_ShotBeats }
    public static UnlockStartMode UnlockStart = UnlockStartMode.B_AfterShot;
    public static PanelGestureMode PanelMode = PanelGestureMode.B_AfterShot;
    public static bool UnlockEcho = false;            // #13's optional 180 ms turn, OFF (O-20)
    public bool PanelShotExempt { get; }               // (d) only; read-only, for the log
    // KeyHud subscribes itself to the static FrontRoomsDoorShot.Beat (KeyInsert, KeyTurn, BoltRetract) for (d).

    public static class Strings { /* §2.4, the one place the proposed strings live */ }
#if UNITY_EDITOR
    public static System.Func<float> EditorClock; public static float EditorStep; // harness only
    public string DebugLine { get; }  // state, clocks, every channel: the harness logs it per frame
#endif
}
```

### 6.3 Map contract (关卡设计 wires it; `FrontRooms3DGame.cs` stays map-owned)

The clone patch implementing exactly this is the contract diff `W/hudkey/contract/FrontRooms3DGame.contract.diff`, made in the build stage.

| # | Where | Exact change |
|---|---|---|
| M-1 | `BuildHud`, after `keyText` (`:2020`) | `keyHud = FrontRoomsKeyHud.Attach(hudCanvas, crosshairObject.GetComponent<RectTransform>(), keyPanel.GetComponent<RectTransform>(), mobileHud);` Add the field `FrontRoomsKeyHud keyHud;`. |
| M-2 | `UpdateHud` `:2319` and `SetPhase` `:2184` | `crosshairImage.enabled = play && (keyHud == null \|\| !keyHud.OwnsDot);` |
| | `UpdateHud` `:2323` | `promptText.text = play && prompt != null && (keyHud == null \|\| !keyHud.OwnsPrompt) ? DisplayPrompt(prompt) : "";` |
| | `UpdateHud`, after the prompt line | `keyHud?.SetAim(AimKindOf(aimed), IdentityOf(aimZone), aimHaveKey);` where `AimKindOf` is the map query in M-6. **No string parsing.** |
| | `UpdateHud` `:2339-2351` | When `keyHud != null`: `keyPanel.SetActive(false)`, drop `keyPanelFade`, and call `keyHud.SetHeldKey(play && !inStartRooms && map.HasKeyFor(zone.id), map.ZoneKeyIdentity(zone.id))`. **Without** `!presentingKey`: KeyHud handles the flight. `SetPhase :2202` stays as is. |
| M-3 | `ApplyGameplayHudAlpha` `:2394` | `keyHud?.SetHudAlpha(gameplayHudAlpha, rig != null ? rig.HudFade : 0f);` This also covers DECISIONS #13 (b)/(c) and #14 (d). The HK1 `keyHudGroup` line is not needed (O-21). |
| M-4 | `UseDoor` `:1306`, after `map.Use(c)` | `if (keyHud != null && c == aimed) { if (result == Rattled) keyHud.NoKey(); else if (result == KeyTurn) keyHud.Unlocking(); else if (result == Broken) keyHud.BrokenTry(); }` This keeps it to the crosshair's door when a touch tap hits another door (`HandleMobileLookTap`). |
| M-5 | `FrontRoomsDoorShot.TickKey` `:549` + game (KP-1) | At `keyT ≥ KP.HoldUntil`: raise `KeyHandoff(GridCoord zone, Vector2 viewport, float depth, float keyT)`, where depth = the real held distance after `ClampToWorld`; hide/destroy the 3D key; skip the pocket drop (`FinishKey`). The game subscribes: `keyHud?.PickupFlight(map.ZoneKeyIdentity(zone), viewport, depth, rig.BaseFov, keyT)`. The pocket sound's time is the sound chat's call (KP-1 item 2). |
| M-6 | `FrontRoomsMapWorld` (new public API) | <ul><li>`AimKind Kind(Collider c, Vector3 from, out GridCoord keyZone, out bool haveKey)` → None / Plain / Locked / Broken. Locked = `NeedsKey(door)` (also when you hold the key); keyZone = `CacheZoneOf(from)`; Broken = `door.broken` (系统设计 §10).</li><li>`ZoneKeyIdentity(GridCoord zone)` → (shape, colour, number 0–99): the map's identity rule, shared with D1.6 `DressDoor(idS, idP)` / `DressKey`.</li><li>`UseResult.Broken` (append to the enum).</li></ul> The game converts to `FrontRoomsKeyHud.Identity`. |
| M-7 | `FrontRooms3DGame.Mobile.cs:288` (map-owned) | Choose `UseKind` from M-6's kind instead of the string when the touch session adds Unlock/Broken (§6.4). Until then nothing changes. |

**Not required:** no `Describe` string change. If the map later renames the has-key prompt to `E  ·  UNLOCK`, the parse still gives Open, because "UNLOCK" contains no "LOCKED". A broken string containing "SHUT" needs M-7 first.

### 6.4 Touch contract (touch session; separate)

- **T-1:** add `UseKind.Unlock` (verb `UNLOCK`) and `UseKind.Broken` (verb `BROKEN`) to `FrontRoomsTouchControls` / `…View.VerbOf`.
  - The deny shake of the USE button already exists (`lockedShake`, `View:579`).
  - Broken has no button shake in the lab.
- **T-2:** no change is needed for the mini prompt glyph. It hides by itself while KeyHud blanks `promptText`.

---

## 7. Parts

### 7.1 Plan A (recommended): sprites only where the shape is not procedural

| Asset (Resources path) | Content | @1x / 2x / 3x / 6x (px) | Pivot / anchor | Count |
|---|---|---|---|---|
| `UI/KeyHud/KeyB_<shape>_<colour>@{n}x.png` | B ring + tag (ring stroke 1.2696, tag rim, tab rim, body, tab, insert), **no number**, from `B_<shape>_<colour>_held.svg` | 72×48 / 144×96 / 216×144 / 432×288 | swing pivot (35, 10) = (0.4861, 0.7917) | 9 × 4 = 36 |
| `UI/KeyHud/KeyB_key@{n}x.png` | the key path (the same for all shapes; rect master) | same box | glyph anchor (36, 24) = (0.5, 0.5) | 4 |
| `UI/KeyHud/KeyNum_<shape>@{n}x.png` | number atlas: 10 × 20 cells, rows 0–9 = left digit of 00–99, rows 10–19 = right digit. Each number is rendered as the masters do: size so its ink height = the digit height (10.212 / 7.659 / 7.936), ink box centred on the anchor, long sx 1.035. The anchor sits on a texel corner. | cell ≈ 20×12 rect, 15×10 round, 16×10 long (@1x, exact by pipeline); 3 densities (@1x/2x/3x) | cell anchor = insert centre | 3 × 3 = 9 |
| `UI/Fonts/CourierPrime-Bold.ttf` (+ `OFL.txt`) | copy of `Assets/Fonts/Period1990/CourierPrime/CourierPrime-Bold.ttf` for the panel number (legacy Text, 25 / 21) | — | — | 1 |
| `UI/HUD_KeyGlyph.png` (replaced, GUID and `.meta` kept) | B rect red 14 composite @2x (144×96) for the map's legacy slot only. KeyHud does not use it. | 144×96 | centre | 1 |
| `UI/KeyHud/FrontRoomsHudVector.shader` | §3.2 | — | — | 1 |

**Importer (`.meta` written by us, new GUIDs):**
- Default/Sprite type, sRGB on, **mipmaps off**, clamp, bilinear;
- **uncompressed** (`textureCompression 0`) on every platform, iOS and WebGL included;
- `alphaIsTransparency 1`;
- maxSize 2048.

Folder `.meta` files: `Scripts/Rendering/UI`, `Resources/UI/KeyHud`, `Resources/UI/Fonts`.

**Density choice at runtime:** the smallest density ≥ 0.9 × (scaleFactor × ls × displayed scale).
- At 1080p: lock digits @1x, P4 take-off (×3) @3x.
- At 1200p: @1x (1.054).
- On the iPhone: the lock (2.25) uses @3x, and the take-off (×1.117 pt × 3 px) uses @6x for B.
- The numbers have no @6x (memory). The number on a flying tag upscales ≤ 2.2× while it moves.

The 6x tier is an addition to the brief's @1x/2x/3x (O-12).

**Pending from 平面视觉:**
- O-1: does B get a keyline? The lab and the 9 B masters have **none**; the brief says "baked in". If yes, two more layers per density: `KeyB_<shape>_kl` (rotating) and `KeyB_key_kl` (static), drawn first, by the same rule as `export_lock_svg.py` (ink copies, rims +1 px, ring stroke +2, group 85 %). Their crossings double the alpha in sub-pixel specks, unless the key and the swinging tag share one keyline. O-1 decides.
- O-2: are the digits Courier Prime **Regular** or **Bold**? The atlas builds either from one constant.

### 7.2 Plan A also renders (pipeline references, not shipped)

- The 18 padlock end states and the 9 B composites at @1x/2x/3x, used as the harness references (§9.4).

### 7.3 Plan B (fallback if a custom UI shader is refused): 9-slice + feathered meshes

**Sprites:** white masks, tinted by `Image.color` (exact in linear: white texel × linear vertex colour). @1x/2x/3x. Size = 2·⌈R·d + 1⌉ + 1, border ⌈R·d + 1⌉ on all sides, pivot centre.

| Role | Radius ladder (px @1x) | Notes |
|---|---|---|
| rim (rect, long) | 8, 6, 4.45, 3.45 | runtime picks the ladder step ≥ r and sets `pixelsPerUnitMultiplier` = r_step / r, so the AA width stays within ±25 % |
| body | 7, 5, 3.45, 2.45 | |
| insert | 2.45, 1.7, 1.035 | |
| keyline | 9, 6.5, 4.45 | |
| circle (round, dot, knob) | Ø 32 @1x master, scaled | Simple image |

**Meshes:**
- shackle and its keyline: a 50-point polyline strip with a 1 px alpha fringe;
- the B·4 frame: a partial-length strip;
- the leaf: a 9-slice sprite, r 0.8 → 8.

**Defects that remain in Plan B:**
- keyline double alpha (≈ 98 % instead of 85 %) in the 1 px rim band under each leg;
- the strip self-overlaps for |k| < 0.15 (about one frame per swing);
- AA width drifts while the radius lerps.

---

## 8. Rasterisation pipeline

**Tools on this Mac (checked 17:1x):**

| Missing | Available |
|---|---|
| `rsvg-convert`, `cairosvg`/`cairo`, Inkscape, ImageMagick, `resvg`, node, fontTools, skia, scipy | venv Python 3.9 + numpy 2.0.2 + Pillow 10.4 |
| Chrome headless (not available to subagents) | Blender 4.3 (own numpy; its SVG import is curve-based and not needed) |
| | `jsc` and `osascript -l JavaScript` (JavaScriptCore) |
| | `qlmanage`: renders SVG through WebKit; checked, see below |
| | `sips` |
| | Illustrator, Affinity and Figma (GUI only) |

`qlmanage -t` rendered `Lock_rect_red_closed.svg` correctly. It draws at the SVG's intrinsic size in a square canvas, so rewrite `width`/`height` to scale it.

**Choice:** extend the key-icon pipeline (`W/keyicon_design/iconlib.py`, its `design.py` approach) in `W/hudkey/pipeline/`.

**Steps:**

1. **`svgpoly.py`** reads the masters: `path d` (assert M/L/Z only; every master path is), fill, fill-rule, stroke, stroke-width, `<g opacity>`.
2. **Rasteriser additions:**
   - (a) **butt caps** for open strokes: iconlib's `stroke_coverage` gives round caps; the masters use the SVG default butt;
   - (b) **group union**: OR the supersample masks before averaging, so the keyline group is composited once at 0.85;
   - (c) layer subsets per part (ring+tag without the number, key, number only).
3. **Digits:** `iconlib.Font(CourierPrime-{Regular|Bold}.ttf).text_polys`, with the `design.py` sizing rule, then L/R split per glyph. Assert: "14", "37" and "08" reproduce the master `number` paths. Regular matches to 0.001 px in width today; Bold does not (§11, O-2).
4. **Output:** linear-light composite, straight alpha, RGB bled into transparent pixels, PNG @1x/2x/3x(/6x) + a manifest JSON (sizes, anchors, cell grid, sha1).
5. **Checks** (written to `W/hudkey/pipeline/checks.json`):
   - corner alpha 0, one RGB per single-colour part;
   - the parts re-composited equal the full master render within 1/255;
   - QuickLook (WebKit) render of each master vs ours: alpha edge difference within 2/255 after scaling;
   - digits vs master paths: bbox within 0.01 px.

Run with `W/venv/bin/python` (no `-I`; the wrapper needs its path). Outputs go under `W/hudkey/parts/`; nothing is written in main until the package.

---

## 9. Test harness

One clone: `W/tools/mkclone.sh proj_hudkey`. Disk shows **63 GiB free**, not 90.

### 9.1 Setup

1. Make the clone, then force a reimport of `Assets/Scripts` (TOOLS.md known issue), then compile with `-buildTarget OSXUniversal` and expect 0 `error CS`.
2. Install into the clone:
   - the package files;
   - the contract diff (`FrontRooms3DGame.cs`, `FrontRoomsDoorShot.cs`, `FrontRoomsMapWorld.cs`: M-1…M-6, with a stub `ZoneKeyIdentity` = hash → (shape, colour, number));
   - the harness: `Assets/Editor/HudKey/FrontRoomsKeyHudCapture.cs`, plus a runtime boot `Assets/HudKeyHarness/FrontRoomsKeyHudHarnessBoot.cs` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, gated by a SessionState key, calls `FrontRoomsHandheld.Force(IPhonePro)` for the phone run; the `FrontRoomsTouchPlaytestDriver` pattern).
   - All harness files are ASCII only, no tuples, never packaged.
3. Isolate PlayerPrefs: change `PlayerSettings.productName` in the clone first.
4. Every Play run checks for the game's READY line within 60 s and aborts if it is missing.

### 9.2 Curve logs (no rendering)

- `FrontRoomsKeyHudCurves.RunBatch -quit` evaluates `FrontRoomsKeyHudMotion` at 1 ms and at 1/60 s for every phase and the 3 shapes, and writes `curves_unity.tsv`.
- `W/hudkey/lab/extract_lab.py` copies the lab's pure functions verbatim from `lab_template_v2.html` (easings, `lockGeo`, `swingOpen`, `tug`/`recoil`/`shake`/`DENY`, `LOCK['L3·D'].in`, `FALL`, `fallOut`, `famOut`, `doorPhase`, B·4 `draw` with recording stubs for `el`/`sa`, `simulate`, `heldSwing`, `DEV` paths, `swingKick` and `UNLOCK[0]`/`UNLOCK[2].pose`, and the in-context `shot === 'b'` lock timeline) into `lab_curves.js`. `jsc` runs it into `curves_lab.tsv`.
- `compare_curves.py`: every channel |Δ| ≤ 1e-4 (px, °, α), with the Unity sign flips applied. **Gate.**

### 9.3 Play captures (the real game, real HUD path, contract wired)

- Seed `Random.InitState(4242)` → run seed 516574485, as `FrontRoomsInteractionAudit`.
- Keys on (`DoorsNeedKeysForTools`); `FrontRoomsMapWorld.ShowPlaceholderKey = true` for the P4 hand-off.
- **Stand points** (the game's own autopilot stand helper, `:3263`: 0.9 m off the lock, crosshair 1.3 m up the leaf):
  - **lit L0** door;
  - **dark cell** door;
  - **Office** door;
  - a broken door (`map.ResetDoorForTools` / break through the map's tool path);
  - a key on the floor in lit L0.
- **Clock:**
  - After standing, set `Time.timeScale = 0`.
  - KeyHud runs on `EditorClock` = frame × 1/60. `Time.captureDeltaTime` does not step unscaled time; the touch driver found the same.
  - Events are injected through the real call sites: `UseDoor` with the aimed collider (E), and the door shot's pickup for P4.
  - **Plus one real-clock smoke run:** `timeScale = 0`, no `EditorClock`, wall-clock frames. The log must show every state advancing, which proves unscaled time.
- **Capture:**
  - Use the touch driver's method: canvases converted to ScreenSpaceCamera on a UI camera, UI rendered over black and over white for exact linear coverage, composited over the scene RT.
  - Sizes: **1920×1080**, **1920×1200**, **2622×1206** (iPhone 16 Pro, forced handheld, touch HUD scale).
- **Scripts per stand point:**
  1. LockIn → hold 0.2 s → NoKey → Deny → leave → famOut.
  2. LockIn → Unlocking with the real unlock shot (the panel holds this zone's key), once per mode:
     - (b) + panel (b), the default;
     - (b) + panel (d);
     - (a), with no shot (BeginUnlock early-out);
     - (c);
     - and a **cancel before the commit** (S at 0.3 s) in (d): the gesture eases back.
     Each log records t_ret, the KeyInsert/KeyTurn/BoltRetract beat times and the U·3 channels.
  3. Leave mid-build (u 0.5) → re-aim mid-fold.
  4. BrokenIn → BrokenTry → hold → BrokenOut.
  5. P4 pickup from HoldUntil to meta + 0.3 s.
  6. Idle dot.
- **Frames:**
  - every 1/60 s as 360×200 crops around the crosshair, and the panel region for P4;
  - a full frame every 10th;
  - 1920×1200 and phone at every 4th frame plus the key frames (u 0, .15, .3, .45, .62, .8, .9, 1; deny peaks; k = 0.5, 0, −1; f .15, .25, .5; B·4 knob pop, act 0, react 0.09; P4 s .25, .5, .75, 1).
  - **Identities:** sequences for rect red. End states (closed, open, deny peak) for all 9 identities × 3 backgrounds at 1080p.
  - Estimate: about 1,500 crops + 150 full frames, under 1 GB.
- **Per-frame log:** `hudlog.tsv`: frame, clock, state, phase clocks, every channel, line α, dot α, `OwnsPrompt`, screen rects. It is diffed against `curves_lab.tsv` at the same clock to check the **wiring**:
  - the build starts on the prompt frame;
  - the swing on the KeyTurn frame;
  - the flight at HoldUntil;
  - nothing stops under `timeScale = 0`.
- **Shader compile gate** for the other targets: `ShaderData.Pass.CompileVariant` for Metal (macOS, iOS), D3D11 and GLES3x (WebGL).

### 9.4 Image checks

- **Reference renders:** `W/hudkey/pipeline/ref_render.py` draws the lab's geometry at the key frames in **sRGB blending**, like the browser, over the captured scene crops.
- **Gates:**
  - Unity vs reference: max per-channel Δ ≤ 3/255 inside shapes, edge band ≤ 12/255;
  - the keyline ring's mean matches within 2/255 (this calibrates `KeylineAlpha` and the text Outline alpha, which compounds over its 4 taps);
  - digits and lines on whole pixels at rest;
  - baselines 591/614 (239/259 pt) ±0.5 px;
  - the dot → rim hand-off frame differs only by the white → paper step (the lab does the same, O-14).
- **Contact sheets** (`W/logs/chk/contact.py`) and difference heatmaps go to the **VISUAL VERIFICATION LOG** in Figma, one slide per check, per `VERIFICATION_LOG.md` §1. The next free number was VL246 at 17:0x; re-read it before placing. The rows go in §3.

---

## 10. Package and merge

- **Package:** `W/apply/hud_key_b.sh`, convention v2 (`W/apply/README.md`).
- **Payload:** only visual-owned paths, §3.1 and §7.1:
  - every path ABSENT except `HUD_KeyGlyph.png`, whose base is recorded and kept as a base copy;
  - `HUD_KeyGlyph.png.meta` goes in `_keep.txt` (GUID).
- **No line of `FrontRooms3DGame.cs`, the map or the touch files.**
- **Inert until wired:** with M-1 absent nothing attaches. The only visible change is the B glyph in the old 40×22 key slot, which is reachable only with keys on (O-13).
- **Contract notes:** `hud_build/20_contract_map.md` (M-1…M-7 with the diff) and `21_contract_touch.md` (T-1, T-2) go to `_keep.txt` as notices.
- **Gates before the dry run:** Roslyn (`roslyn_check.py --ios` too); clone batch compile 0 errors; shader variant compile (§9.3); §9.2 curve gate; §9.4 image gates.
- Then `DRY RUN OK`, the visual chat runs `--apply`, and it merges at once.

---

## 11. Open points and conflicts (for 平面视觉 / the visual chat / Red)

| # | Point | Default in this design |
|---|---|---|
| O-1 | **B glyph keyline:** the brief says "baked in" and "both masters carry `<g id="keyline">`". **Only the 18 `Lock_*` masters do.** `B_*_held.svg` (10-07) and the lab's `keyGlyph` have none, and README's keyline list names only the padlock, the dot and the prompt. | Pipeline supports both; B without a keyline until 平面视觉 answers |
| O-2 | **Digit face:** README and the brief say Courier Prime **Bold**. The master `number` paths are **Regular**: "14" ink width 17.450 = Regular exactly, Bold = 18.031; same for 37 and 08. | One constant; default = the masters (Regular), what Red saw in Figma and the lab |
| O-3 | **The shackle shows at u = 0.** Hidden in the body at u = 0, its arc cap pokes 0.5–1.9 px (paper) and up to 2.9 px (keyline) outside the 16 px dot for 1–2 frames. Round is worst: apex −9.9 vs rim −8. The lab draws the same. | Ported as is; flagged to 平面视觉 |
| O-4 | **Unlock shot vs padlock** (DECISIONS #13, an open conflict). The shot fades the HUD in 150 ms until 1.45 s, and it frames the keyhole at the screen centre. In (c) the 2D padlock overlays the 3D key insert (0.55–0.90 s) while it drops (gone at about 1.06 s). | Parametric; default (b) per #13 |
| O-5 | **Text keyline on phone:** the brief says × touch scale (0.75 pt); the lab draws 1 pt. The lock and dot keylines are × 0.75 in both. | Lab (1 pt) |
| O-6 | **Keyline alpha in linear light:** 85 % sRGB (lab) ≈ 97 % in Unity per the desktop-UI table. The Outline's 4 taps compound. | Calibrated by the harness, one constant each |
| O-7 | **Interrupt rules** (§4.5) are not in the lab. Deny retrigger: HUD guard 0.50 s vs map rattle guard 0.40 s. | §4.5 defaults; suggest the map uses 0.50 s or accepts the 0.1 s gap |
| O-8 | **B·4 small steps:** 0.4° at IN → HOLD (act 0.36 s); a small step at the end of the E window. Lab-identical. | Ported as is |
| O-9 | **P4 on other aspect ratios:** the lab gives only 1920×1080 and 874×402 pt. | ctrl kept at a fixed distance from the anchored edge; held spot and S₀ from the viewport, depth and FOV |
| O-10 | **Instant pickup path** (no presented model; shipped play): there is no P4 hand-off. | Panel pops as today |
| O-11 | Phone panel number 25 × 0.85 = 21.25: legacy Text needs whole sizes. | 21 (TOUCH ramp) |
| O-12 | **@6x tier** for the B glyph (P4 take-off on phone and 4K); not in the brief. | Added for B only |
| O-13 | Replacing `HUD_KeyGlyph.png` before M-1 shows B shrunk in the old 40×22 slot. Keys are off in shipped L0. | Replace with B rect red 14 @2x; alternatively hold it for the map wiring |
| O-14 | The dot is #FFFFFF, the rim paper #F4F1E8: a 4 % step at the hand-off. Same in the lab. | Ported |
| O-15 | **Missing upstream:** zone identity (M-6), `UseResult.Broken` and the broken kind (系统设计 → 关卡设计), the P4 hand-off (KP-1), `doorsNeedKeys` off in L0. LOCKED, BROKEN and P4 can't be reached in shipped play until those land. | Harness uses the contract diff + stub identity |
| O-16 | `held_pose/measure.json` is absent. | Factor 3 at 1080p (1.117 at 402 pt); anisotropy easing default eio |
| O-17 | **HDR display mode** (`defaultHdr`): a custom UI shader under URP HDR output is not covered by batch captures. | Check once in a player build |
| O-18 | The lab keeps changing (17:18: K·1–K·4, U·1–U·4). | Only DECISIONS #1–#7, #10, #11, #13 (parametric), #14 and #15 are built; #12 is withdrawn, so there is no key layer in the lock |
| O-19 | **(b)'s drop start.** #13's text says the padlock "fades in with the HUD's own 0.15 s fade, holds 100 ms, then drops", which reads as a drop at +0.25 s. The lab code (in-context, `shot === 'b'`) starts F·1 at t_ret + 0.10, during the fade. | Lab code (+0.10); one constant |
| O-20 | #13's optional echo, "K·1's fold", belonged to the key layer in the lock that #12 withdrew. What it turns now is undefined. | Parameter OFF, unimplemented until 平面视觉 re-specifies it |
| O-21 | #14 proposes an HK1 line on the map's `keyHudGroup`. Under this seam the map's key panel is retired and KeyHud's own panel group already receives `gameplay` and `shotFade` (M-3). | No HK1 line; tell the visual chat |
