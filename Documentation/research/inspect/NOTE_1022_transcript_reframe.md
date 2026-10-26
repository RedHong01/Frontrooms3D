# NOTE 2026-10-24 · EXAMINE: TEXT re-frames the close-up (inspect_reframe)

游戏视觉, workflow inspect-reframe, build stage (session local_5656cffd, account A). Written 12:4x, Red away (unattended).
Request: 交互设计 21:2x (phone T7, G10 "TEXT works for that close-up"), plus their 10-24 observation that the iPhone close-up looks smaller and lower on today's map.
Package: `~/FrontRoomsVisualWork/apply/inspect_reframe.sh` (v2). Clone: `~/FrontRoomsVisualWork/proj_inspreframe`, made from main 4049c70b at 12:05 after inspect_regions landed (12:04). Re-synced at 12:40 after 交互设计's C7 landing (Game 00d06f84, touch driver bec540c0).
Not landed: 游戏视觉 lands it with a dry run, then `--apply`, then Roslyn.

## 1. What changes for the player

- **TEXT during a close-up now moves the camera.** The subject eases into the area left of the panel. With TEXT off, it eases back to the open's own pose, bit for bit.
- **The ease is the open's.** It uses the same sine in-out curve and the same time rule: 0.30 s + 0.12 s per metre of travel, kept between 0.40 and 0.55 s. Reduced Camera motion multiplies it by 1.5, and Camera motion Off makes it a cut, as for the open.
- **A second tap during the ease does not jump.** The running ease keeps going underneath the new one, so the camera keeps its speed and turns round smoothly.
- **Look, zoom and pan keep working during the ease.** The zoom view keeps the open's framing (§6.2).
- **A close mid-ease returns from where the camera is.**
- **The object kind and the held key do not move.** If no pose fits the new rect, the framing stays and `ReframeWhy` gives the reason.
- **On phones, the free rect ends at the panel's own left edge.** The panel sits in the bleed safe area. On the iPhone 16 Pro the subject now stays left of x 542 of 874 pt; the panel spans x 542–812, y 64–280 pt from the top.
- **Composition A's rect is cut by the panel when they overlap.** On phones, A (the left 64 % of the screen) reached 17 pt under the panel. On desktop A already ends at 64 %, so nothing changes there.
- **Handhelds squarer than 16:9 now frame for the FOV they draw** (the iPad, both Duo screens; §3).
- **Desktop does not change unless ShowTranscript changes.** No transcript means no reframe, and the AspectFov helpers are the identity on desktop. ShowTranscript is still cleared only in `Abort` (C24), and the phone's per-close-up undo stays in 交互设计's handheld code.

## 2. API for 交互设计 (exact)

```csharp
// The one entry point. The ShowTranscript setter calls it, so today's
// FrontRoomsInspect.ShowTranscript = !FrontRoomsInspect.ShowTranscript (Mobile.cs) already gets the reframe.
public static void FrontRoomsInspect.SetTranscript(bool on);
public static bool FrontRoomsInspect.ShowTranscript { get; set; }   // set == SetTranscript(value)
public static bool FrontRoomsInspect.Reframing { get; }             // an ease is running
public static string FrontRoomsInspect.ReframeWhy { get; }          // "" = reframed (or none asked); else why the framing stayed
public const float FrontRoomsInspect.TranscriptPanelShare = .36f;   // the panel's share of the safe area, at its right
public static Rect FrontRoomsInspect.FreeScreenRect { get; }        // handheld: ends at the panel's left edge (bleed safe area)
```

What `SetTranscript` does in each state:

| When | Behaviour |
|---|---|
| Same value as now | Nothing happens. |
| Outside a close-up, or while one returns | It sets the flag only. The next CanOpen / Open solves for the new rect. |
| During a flat or panel close-up | It solves the pose again for the new `FreeScreenRect` (A cut by it, or B inside it) and eases there (§1). |
| During the object kind or a held key | It sets the flag only. |

Your leave-close-up reset (`ShowTranscript = false` once the phase leaves Inspect) arrives while the close-up is returning, so it only sets the flag. That is the behaviour you want. Switching the chip to `SetTranscript(!FrontRoomsInspect.ShowTranscript)` is optional; it does the same thing.

## 3. ALSO CHECK: the smaller, lower iPhone close-up on today's map. Not a clamp, not the map

**Measured at today's stand.** A clone-only probe sat in 交互设计's touch harness (iPhone, `-touchFocus examine`). The placard is `Kit_EvacPlacard#1`: eye (254.767, 1.620, 19.027), face 1.0 m ahead, a door frame on its left.
- Every settled arrival puts the camera **0.000 mm** from the solved pose.
- The rig's `ClampToWorld` never fires, and the solve is never refused.
- One arrival per run was 41–44 mm off with FOV +1°. That was in the middle of the harness's gesture checks, and it never repeats at the same stand.

**A controlled check (EditMode).** I added a door pier 0.30 m proud, 0.33 m left of the frame. The distance is unchanged (0.360 m), and the camera sits 0.000 mm from the solved pose.

**The cause is the harness's camera aspect combined with the 10-24 aspect-FOV hook.**
- The touch harness runs in batch mode on a 640 × 480 display, so the game camera's `aspect` is 4:3 both when we solve and when the rig sets the FOV. The capture then renders into a 2622 × 1206 texture.
- On 10-22 this only made the solve pick composition B (centred) instead of the phone's A.
- Since 交互设计's Duo landing (10-24 05:06), the rig draws `FrontRoomsHandheld.AspectFov(fov, camera.aspect)`. For 4:3 that widens the close-up from 61° to 76.3°, but our solver still framed for 61°.
- So the placard came out at tan 30.5° / tan 38.15° = **75 %** size and was pulled toward the centre. That matches the 25 % shrink in `frames_r2_iphone/24_examine_open.png`.

**On real devices:**

| Device | Before this package | After |
|---|---|---|
| iPhone 16 Pro (2.174) | AspectFov is the identity, so main was already right: A at 0.360 m, sheet x 89–532 pt, clear of the panel | Same |
| iPad 11 (1.44) | Draws 72.1° but framed for 61°: about 81 % size, and not where the solve put it | Fixed in this package: solved as drawn, A at 0.360 m, the face centre at x 0.325 as solved |
| Duo inner (1.42) | Same problem, and main chose B (centred under the panel) | Fixed: A at 0.360 m, drawn 72.8° |
| Duo outer (1.455) | Same problem | Fixed |

**The fix (ours, in this package):**
- `InputFor` solves at the FOV the rig draws.
- The zoom step's FOV and the object kind's fill go back to the rig in authored terms.
- iPad zoom check: drawn 58.64° = wanted 58.64° (authored 48.89°).
- Consequence: the iPad and the Duo now get composition A, the desktop's composition, because AspectFov keeps 16:9's horizontal field there.

**For 交互设计 (your driver, not ours).** Every touch-harness frame since 05:06 is drawn at the 4:3-widened FOV, gameplay included: 76° becomes 92°.
- Giving the game camera the profile's aspect while a profile is forced fixes it, for example `cam.aspect = FrontRoomsHandheld.ScreenPixels.x / FrontRoomsHandheld.ScreenPixels.y`.
- The clone-only probe `~/FrontRoomsVisualWork/inspreframe/probe/InspreframeProbe.cs` does exactly that under `INSPREFRAME_ASPECT=1` (run t2).
- **For 关卡设计:** nothing. The placard's placement is fine.

## 4. Files (main base → package)

| Path | Base | New | Change |
|---|---|---|---|
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspectFraming.cs` | b93122db | 939c358e | `Solve`: a free rect that cuts into A's rect cuts A too |
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs` | 2a2af005 | df6475a6 | `SetTranscript`, the reframe legs, `BasePose`, handheld `FreeScreenRect`, drawn FOV in `InputFor`, `AuthoredFov` |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectTests.cs` | 5002dc91 | f1a3114f | `ReframeTests` (18 checks) |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs` | 3870d74e | 46717d34 | `ReframeScenario` (16 checks, in the game loop) |

- Every `.meta` is kept.
- The package adds no platform branch, asset or scene.
- It touches no map, Game or Input file.

## 5. Tests (clone W/proj_inspreframe; every run UNDER LOAD, load 11–41)

| Run | Clone state | Result |
|---|---|---|
| InspectPlaytest, main's code ×2 | main 4049c70b (before C7) | 116 / 116 PASS, twice (193 s, 160 s) |
| EditMode | package, before C7 | **129 / 129 PASS** (111 + 18 new), 10 s |
| InspectPlaytest | package, before C7 | **132 / 132 PASS** (116 + 16 new), 161 s |
| Touch iPhone `-touchFocus examine` (t1) | package, harness as it is (4:3 camera) | 41 / 41, all passed, 55 s |
| Touch iPhone (t2) | package + the probe giving the camera the phone's aspect | 41 / 41, all passed, 45 s |
| EditMode | package + main 12:40 (C7 landed) | **129 / 129 PASS**, 11 s |
| InspectPlaytest | package + C7 | **132 / 132 PASS**, 151 s; note "zoom input as the game set it: wired (C7)" |
| Touch iPhone (t3) | package + C7 + driver bec540c0 | **44 / 44, all passed**: pinch zooms (0.999), double tap toggles, 4 heads + 13 lines |

**The requested checks:**
- **The subject moves into the free rect within the ease, and back.**
  - Desktop B (EditMode): x 0.143–0.857 → 0.048–0.592, in 0.400 s for 182 mm. Peak step = 1.59 × the average (sine in-out: 1.57). TEXT off returns to the open's pose bit for bit.
  - In the game (the test bed's door sign): x 0.298–0.702 → 0.118–0.522. It is inside the free rect at 0.167 s and settled at 0.400 s, then back in 0.400 s.
- **A double toggle mid-ease (0.20 s) has no pose jump.** The largest frame step equals a single toggle's (12.06 / 12.06 mm in EditMode, 7.39 / 7.39 mm in the game). The largest speed step is 1.5–1.7 × one ease's own: 3.127 vs 2.072 mm/frame, and 1.648 vs 0.969 mm/frame. A frozen start would be about 7.9 × (simulated).
  - Movement direction across the second tap: the frame before moved 12.06 mm, the frame after 11.65 mm, same direction (dot 1.000).
  - In the real harness (t1/t3) the driver's TEXT on/off taps landed mid-ease. The probe logged TOGGLE then TOGGLE 0.33 s apart, and all checks passed.
- **The desktop path with no toggle matches main.**
  - EditMode: the camera sits on the solved pose, and TEXT off returns it bit for bit.
  - InspectPlaytest: all 44 captures shared with main have identical camera position, forward, FOV and shot weight (`frames.txt`).
  - The PNGs are not byte-identical run to run, **even main against main**. Main×main gives a mean of 1.3–3.7 levels; main×package gives 1.3–1.8 at the same captures. Film grain and similar effects change between runs, so pixel identity cannot be shown by PNG bytes. The render inputs are identical.
- **The phone profile frame matches the panel geometry.**
  - The panel computes to x 542.0–812.0, y 64.0–280.0 pt from the top, and the free rect ends at x 542.00 pt.
  - Phone placard: x 89.3–531.9 → 80.5–523.1 pt, y 41.7–339.3 pt.
  - Phone B: x 196–678 → 98–506 pt.
  - In the harness at the phone's aspect (t2) the same numbers hold: 89.4–531.9 → 80.8–523.2 pt.

**Package checks:**
- The dry run on main (4049c70b + tree, 12:38 and again after C7 at 12:40) prints DRY RUN OK.
- `rebase_apply.sh --no-install` passes, with nothing to rebase.
- Scratch tests are in `W/tools/_test/inspect_reframe_1024` (log `W/inspreframe/apply_tests.log`):
  - dry 0, `--apply` MANIFEST OK, then ALREADY APPLIED;
  - the script refuses a changed .meta, a half-applied target, a tampered payload and a moved base, and writes nothing.
- Roslyn `--all` on main plus the payload: COMPILE OK for macOS and `--ios`.

## 6. Frames (Shot format)

- `images/reframe_before_after.jpg`: 交互设计's 10-24 08:25 frame `frames_r3_iphone/24d_examine_text.png` (main) against t1's `24d` (package). Both are the same harness and the same stand, still solving at the batch display's 4:3.
- `images/reframe_iphone_aspect.jpg`: t2 at the phone's own aspect, TEXT off against TEXT on. This is what an iPhone 16 Pro shows.
- Sources: `~/FrontRoomsVisualWork/inspreframe/results/{t1,t2,t3}/frames/`.

## 7. Open

1. **For 游戏视觉, already queued in LANDINGS (12:39), not in this package:**
   - the head-said-once rule for the desktop `TranscriptText` (平面视觉 12:22);
   - retiring `FrontRoomsInspectTests` "zoom not wired (v1)", which passes in batch only because Game.Awake never runs there.
   - Both touch the same two files: rebase on this package.
2. **The zoom view keeps the open's framing when TEXT is on.** A zoomed region stays centred on the full screen, so the panel can cover part of it. Whether the zoom should also respect the panel is a G9/T7 call for 平面视觉 and 交互设计.
3. **The touch harness's 4:3 camera (§3)** is 交互设计's to fix in their driver.
4. **RED_ACTIONS: none.**
