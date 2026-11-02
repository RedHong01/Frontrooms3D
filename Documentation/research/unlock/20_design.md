# 20 · Unlock: the EXIT's lock structure and its micro cut scene (design + build, 2026-10-31)

**Line:** 游戏视觉, session local_5656cffd, account A. **Stage:** design + build + package. Main is not changed by this stage, except for this file.

**Red's ask (10-31, verbatim):** "目前在玩家开锁的时候，没有真的开锁的动画（micro cut scene）并且目前的门锁似乎没有一个对应的结构，因此我需要你research在那个年代的锁是什么样的，然后把这部分的锁的结构，以及钥匙是怎么打开锁的这个micro cut scene给做出来".

**Red 10-31 ~01:00 (verbatim):** "做的时候先不用测，直接做，然后等都落地了一起测即可".
- So nothing here was run in Unity. Nothing was captured or tested.
- The gate was:
  - Roslyn on main + payload, macOS and iOS: 0 errors;
  - the package's dry run: DRY RUN OK;
  - a rehearsal `--apply` on a scratch copy of main: MANIFEST OK.
- The combined test after landing covers the rest (§9, §10).

**Inputs.**
- `10_research.md` (the period lock, R1, the beat plan).
- The audit of main's seams (the workflow's audit stage, summarised in §4).

---

## 1. What changes, in one paragraph

The EXIT's floor-key lock (A13) is rebuilt as the c. 1990 after-hours chain a guard would really have put on a crash-bar stair door.
- A ⅜ in proof-coil chain is threaded through a ½ in staple welded to a steel plate in the wall beside the latch jamb.
- It runs across the latch case and makes one turn round the crossbar.
- Its two end links ride the shackle of a 2 in laminated padlock that hangs just in front of the latch case. The keyway is on the lock's bottom.

On E · 开锁, the map's unlock shot runs unchanged, and on its clock the lock now acts:
1. The real floor key rises into the keyway in five pin ticks and turns.
2. At the commit (0.86 s) the body drops 11 mm off the shackle with a hard snap.
3. The shackle swivels out about its heel, and the toe-side end link slips off.
4. The chain falls off the bar, and its weight hauls it through the staple until the open padlock jams against the staple with a knock.
5. The long side swings along the wall, its tail on the floor. The key comes out and leaves the frame.

UNLOCKED is that hanging chain, replacing main's floor coil.

## 2. The lock's structure (R1 as built)

### 2.1 The chain

| | As built | Source |
|---|---|---|
| Chain | ⅜ in grade 30 proof coil: wire 9.5 mm, link 54 × 34 mm outside, pitch 34 mm. **1.188 m, 35 links.** Finish: A13's dark oxide `Prop_ChainOxide` #34302B (it keeps the 30 m read; see D3) | 10_research §3, §5; Q2 recommended |
| Lower run | From the padlock's heel-side end link, up over the latch case's face (X ≥ 0.14), the leaf gap and the jamb casing, to the staple, at about 1.00 m | — |
| Staple | A middle link threads the staple: the staple's vertical apex wire passes through the middle of the link, whose long axis runs along the wall. A small loop beyond the staple turns the chain back | — |
| Upper run | Back over the latch case at about 1.05–1.07 m, at least 40 mm above the lower run, to the bar's front-top | — |
| Turn | **One turn** round the crossbar, 0.10 to 0.155 m in from the latch case, centre-line radius 31 mm, links crossed 45° to the bar | 10_research §5 |
| Return | Out of the turn at the front, back toward the latch under the upper run, to the padlock's toe-side end link | — |

Code: `FrontRoomsExitChainKit.LockedPathRaw` / `UnlockedPathRaw`.
- Every path is resampled to 64 points at equal arc length.
- `Place` puts one link per pitch on it, in alternating planes carried from link to link, crossed 45° round the bar and on the floor. Pinned links take their exact frames:
  - the two end links on the bow;
  - the staple's link.
- `ChainMesher` makes each link a stadium of round wire:

  | Level | Segments | Triangles per link |
  |---|---|---|
  | LOD0 | 12 × 6 | 144 |
  | LOD1 | 8 × 4 | 64 |
  | LOD2 | 6 × 3 | 36 |

- The same code builds the cached LOCKED/UNLOCKED meshes and the cut's live chain.

### 2.2 The wall anchor: `Kit_ChainStaple` (replaces A13's eye bolt)

- **Plate:** 76 × 127 × 6 mm, long side vertical, centred 0.14 m past the latch side of the opening, 1.00 m up, on N's wall face.
- **Fixings:** four ⅜ in masonry sleeve anchors (washer Ø 22, hex nut 14.3 A/F × 8.3 mm, stud end).
- **Staple:** a ½ in (12.7 mm) round-bar U standing 38 mm out, with 25 mm clear between its legs and two weld beads.
- **Finish:** the plate in the EXIT leaf's putty enamel (`Prop_SteelPutty`, D4); the fixings and staple bare laminated steel.
- **Budget:** LOD0 2,496 tris, LOD1 384, LOD2 140. Switch at 2 / 6 m, never culled.

### 2.3 The padlock: `Kit_Padlock_Body` + `Kit_Padlock_Shackle` + `Kit_Padlock_Plug` (hero spec, three moving parts)

| Part | Built | LOD0 / LOD1 / LOD2 tris | Slots |
|---|---|---|---|
| Body | 52 × 46 × 26 mm (the HUD padlock family's). Seven plates (the family's stylised count; a real 1990 lock has more, thinner ones), each with a 0.35 mm edge chamfer, so a 0.7 mm groove shows between plates. Four domed rivets on the top and four on the bottom. The two shackle bores and the cylinder bore as dark rings. No maker's mark | 2,238 / 894 / 122 | Prop_SteelLaminated, Prop_SteelBlack |
| Shackle | J shackle, 9.6 mm wire, legs 34 mm apart (24 mm inside), 25 mm clear when shut. The heel runs 30 mm into the body, the toe 9 mm. Each leg has its locking notch (a 1.2 mm waist), shown once it pops. Chamfered ends | 1,564 / 412 / 176 | Prop_ChromeSatin |
| Plug | 13 mm brass face, 0.1 mm proud, in the cylinder bore. The keyway runs front to back: a 2 mm slot with two 0.7 mm wards, the paracentric read | 178 / 106 / 46 | Prop_BrassSatin, Prop_SteelBlack |

- **Generator:** the Blender kit generator, `Tools/Blender/frontrooms_kit/assets/exit_padlock_*.py` and `exit_chain_staple.py`. Shared numbers and fit asserts are in `exit_lock_common.py`:
  - two chain wires pass the legs;
  - the bow takes two links;
  - the pop clears the toe;
  - a link threads the staple.
- **Build command:** `Blender -b --factory-startup --python Tools/Blender/frontrooms_kit/build_asset.py -- exit_padlock_body exit_padlock_shackle exit_padlock_plug exit_chain_staple --no-preview`. All four exported clean.
- **LODs:** hand-built (P-1b). Switches: body/shackle 1.5 / 5 m, plug 1 / 3 m; never culled.
- **Frames:**
  - the body's origin is its top face's centre;
  - the shackle's origin is its heel axis, and it pops +Y and swivels about +Y;
  - the plug's origin is its face centre, and it turns about +Y with the key.
- **Missing kits:** if a kit is missing, `FrontRoomsExitChainKit.StandIn` makes a plain mesh, so the lock always has its parts.

**Pose, LOCKED** (leaf rig frame):
- Top face centre at (0.152, 0.938, 0.903).
- Turned 10° toward the hinge (the dipped camera's side) and tipped 6°, bottom toward the corridor.
- It hangs on its two end links at ±35° from the bow's apex.

**Pose, UNLOCKED:**
- Shackle popped 11 mm and swivelled −110° (toe out).
- Hanging under its heel-side end link one pitch from the staple's link, its mass straight under the contact (`FrontRoomsExitChainKit.Hang`).

### 2.4 Fits and clearances

These were computed with `W/unlock_work/geom.py` (a numpy replica of the kit's poses and paths, no Unity). Every link is still to be proved on the combined test, as A13's were.

| Check | Value |
|---|---|
| Padlock body's back vs the latch case's face (X 0.120) | 0.1348: 14.8 mm clear |
| Toe-side end link's outline vs the latch case's face | 0.1237: 3.7 mm clear (it was −8 mm with the padlock at 0.141 / 15° / −12°, so the pose was moved) |
| Heel-side end link's outline | 0.1341: 14 mm clear |
| Upper run vs lower run / return | 40–60 mm apart (link width 34) |
| Turn vs bar | 31 mm centre-line radius; wire 2 mm off the 32 mm bar at 45° |
| Turn vs leaf face (X 0.025) | inner extreme about 0.048 |
| UNLOCKED padlock body vs plate face (X 0.086) | 0.110–0.150 |
| UNLOCKED: from the padlock to the floor | 1.059 m. **0.129 m pools on the floor** |

### 2.5 Where this differs from 10_research R1 (for Red, with why)

- **D1: both padlocked links are end links. The chain is threaded through the staple, not fixed to it with a quick link.**
  - R1 had a standing link in the bow. A link whose middle carries the shackle has its long axis square to the shackle's wire, which leaves the chain no way to run on both sides of the bow without crossing it.
  - With two end links, the release frees exactly one end, and gravity finishes the job.
  - It also gives a physical reason for the UNLOCKED look: the hanging chain's weight hauls it through the staple until the open padlock jams.
- **D2: the padlock hangs 15 mm in front of the latch case, tipped 6° (not resting on it at 20–30°).**
  - The end links stand 17 mm proud of the bow's plane.
  - More importantly, a keyway on the bottom of a hanging padlock is never visible from the dipped eye. The bottom face would have to tip 46° toward the camera (computed from the shot's 46.7° look).
  - So the in-shot read is the key: its bow under the body turns from edge-on to face-on, and the body lifts, twists and drops on it.
  - The reduced-motion cross-cut (§6) is the one view that shows the keyway itself, from just under it.
- **D3: the chain keeps A13's dark oxide** (research offered dull zinc). ⅜ in is 25 % thinner than main's ½ in, so 70 §4's 30 m read must be re-measured on the combined test.
- **D4: the plate is painted in the EXIT leaf's putty, not the corridor's colour.** The corridor walls are papered, so a steel plate would be enamelled to match the door's paint.
- **D5: no camera change beyond C1** (§4). The research's "eye at 1.29 m, 39°" needs DipEyeDrop changed for the EXIT. With the map's numbers, the look is 46.7° down from 1.37 m, which shows the turn, the padlock, the staple and (as the camera returns) the floor in one frame.

Q1–Q3 of 10_research §9 are built as recommended (R1 form, ⅜ in, hanging UNLOCKED); a different answer from Red means a rebuild of this package. Q4 (film a real 2 in lock at 240 fps, silently) is still open. Its numbers would replace the [estimate] timings below.

## 3. The micro cut scene: timeline on the map's shot clock

`FrontRoomsExitUnlockCut`, driven by the dresser's LateUpdate.

- **The clock:** the map's `FrontRoomsDoorShot.Clock`, read through `map.KeyTurnOwner` (Live / Clock / Committed / Door); the dresser only reads it.
- **Start:** the cut starts the first frame a live, uncommitted unlock shot is on the dressed EXIT.
- **After the release:** it runs on its own clock to its end, whatever the shot does (normal end at 1.55, a Relay cancel after the commit, an abort).
- **A cancel before the release** eases the key out (0.15 s) and the body's lift and twist back, with no pop. LOCKED shows again.

| t (s) | Beat (map's) | The lock and chain | Key | Sync point |
|---|---|---|---|---|
| 0.00–0.30 | dip | still (LOCKED, now the cut's live copy) | hidden | — |
| 0.30–0.55 | KeyApproach | still | rises from the view's lower right (0.85, 0.10 at 0.30 m) to 3 cm under the keyway, bow edge-on | — |
| 0.55–0.68 | KeyInsert, PinCatch | body pushed up 1.5 mm, a 0.3 mm bump at each tick | 25 mm in five steps (ticks 0.57 / 0.59 / 0.61 / 0.63 / 0.65), seat 0.68 | Unlock.PinTick ×5, Unlock.KeySeat 0.68 |
| 0.68–0.86 | KeyTurn | the body twists 4° with the torque and back; the plug turns with the key; lift settles to 55 % | 90°, the first 10° over 35 % of the window (stiff), easing out to 90 at 0.86; tag sways | Unlock.KeyTurn 0.68 |
| **0.86** | **BoltRetract = commit = ExitLockChanged(false)** | **body drops 11 mm off the shackle in 2 frames (0.86–0.893), no easing; one 1.5 mm hop; still by about 0.95** | rides down in the plug | **Unlock.ShackleRelease 0.86** |
| 0.87–0.925 | — | shackle swivels −110° about its heel (toe out toward the camera, a small overshoot); the toe-side end link rides the bow to the toe | — | Exit.ShackleSwivel 0.88 |
| 0.88–1.00 | KeyWithdraw (0.90) | — | turns back 0.88–0.92, then straight down 60 mm (0.92–1.00) from where the plug was | Unlock.KeyOut 0.92 |
| 0.93 | — | the toe-side end link slips off the toe; the padlock now hangs from its heel-side link and swings to hang under it, past and back | — | Exit.ChainSlip 0.93 |
| 0.93–1.00 | — | the turn loosens (pulled to 55 mm off the bar's axis by the falling end), then drops off the bar's top; **the bar is clear by 1.00**, as the map's leaf leaves the frame | out to the lower right 1.00–1.20, then gone | Exit.ChainRun 0.95 |
| 1.00–1.10 | Open (1.00) | the free part hangs from the staple, trailing toward the door | — | — |
| 1.10–1.22 | Return (1.15) | the hanging weight hauls the chain through the staple; the padlock rides up the lower run's line | — | Exit.ChainRun 1.10 |
| **1.22** | — | **the open padlock jams against the staple: one knock (6° wobble, about 0.14 s)** | — | **Exit.PadlockKnock 1.22**, Exit.ChainSway 1.22 |
| 1.22–4.22 | ShotEnded 1.55 | the long side swings along the wall about the staple (12°, period 1.4 s, decay 0.8 s); the tail on the floor stays; the foot touches about 1.38 | — | Exit.ChainFloor 1.38 |
| 4.22 | — | the sway has died (under 0.3°); the cached UNLOCKED meshes take over (they are the live chain's last frame) | — | — |

**How the chain moves.**
- Five hand-set shapes, each 64 points at equal arc length, are interpolated point by point between the beats:
  - K0 locked;
  - K0b the turn loosened;
  - K1 draped off the bar;
  - K2 hanging from the staple, trailing toward the door;
  - K3 UNLOCKED.
- The first and last 0.12 m bend so that the ends sit on the links the padlock carries.
- Pins (end links, staple's link) are weighted in and out by phase.
- The wrap rule (links 45° round the bar) fades out between the slip and 1.00.
- No physics engine; no allocation per frame (the live mesh is rewritten in place).
- The pure curves (Lift, Twist, DropShare, Rebound, Swivel, TurnDegrees, KeyOut, Sway) are public static functions of the clock, for tools.

## 4. Camera framing and the one contract (C1, 关卡设计)

- **The camera timeline stays the map's:** dip to pose P, 0.45 m out (flat), 12° toward the hinge, 0.25 m under the eye, FOV −14°, return 1.15–1.55.
- **Today** the shot's target is the spec keyhole on the latch case's face (the EXIT has no door rig).
  - The camera sits 0.583 m from it at 39.4° down.
  - The padlock is 0.13 m from the frame centre, in frame, at the DOF's blur edge (0.70 m).
  - The cut plays there with or without C1.
- **C1:** `FrontRoomsMapWorld.LockPose` returns the padlock's keyway on the EXIT through the visual dresser's static seam `FrontRoomsExitChain.Keyway(map, door, from, out Pose)`:
  - position: the plug face's centre;
  - rotation: +Z up into it, +Y its face.
  - The map receives a pose and never reads a visual object.
  - The seam answers false (LockPose exactly as today) with no dresser, on another door, or with the EXIT open or broken.
- **With C1,** the camera sits 0.656 m from the keyway, 46.7° down.
  - The padlock is in the middle, the staple at the right and the turn at the left, all inside the sharp zone (0.58 + 0.12 m).
  - The step-in point and DoorUnlocked's position move with it.
  - No timing, input lock or gameplay query changes.
- **Contract files:** `W/apply/unlock_contract/CONTRACT_map.diff` + `README.md`. It is one line plus the doc comment; land it **after** `unlock_cutscene`.
- **Clone:** the clone carries the line as glue (`W/proj_unlock`'s MapWorld = main's at sha1 1f2536b9 + the line).

## 5. Phone tier

- **Identical on desktop, iPhone and iPad** (three-platform parity): the same structure, timeline, motion, key and cues.
- **Geometry:** the kits' own LODGroups as everywhere. During the cut the padlock's parts are held at LOD0 (`ForceLOD`), and the live chain is LOD0 (35 × 144 = 5,040 tris).
- **CPU:** the live chain rewrites 3,185 vertices and normals per frame for about 3.7 s (0.55 → 4.22).
  - [estimate] under 0.15 ms on desktop and under 0.4 ms on an iPhone 12-class phone.
  - To measure on the combined test.
- **Only phone difference:** the reduced-motion cross-cut renders at 0.75 × the screen (`CrossCutMobileScale`).
- **WebGL:** gets the same (no WebGL-only change).

## 6. Reduced motion (Camera motion Off, which the iOS Reduce Motion setting also gives by default)

At Off, the map's shot plays in place: no dip, no FOV, the same beats. The cut adds a **hard cross-cut**, with no camera move:
- **0.55–1.00:** a still close-up of the lock.
  - 32° FOV, 0.28 m out from the keyway, 3.5 cm under it, 7 cm toward the hinge.
  - Aimed at the body's centre, from the shut lock's frame, so it never moves while on screen.
  - It shows the keyway, the key's insert and turn, the drop, the swivel and the slip.
- **At 1.00:** a hard cut back for the fall.
- **How it is drawn:** a camera into a RenderTexture, shown on a full-screen overlay canvas at sorting order −50, so every HUD canvas (and the captions) stays on top.
  - That camera has a target texture, so `FrontRoomsHeldItem.ViewCamera` never mistakes it for the game's.
  - Post settings copied from the game's camera; far clip 6 m.
- **Switch:** `FrontRoomsExitUnlockCut.CrossCutAtMotionOff` (default on).

## 7. Sync points for 声音设计 (names only; no sound file was touched)

`FrontRoomsExitUnlockCut.Cue(string name, Vector3 where)` is raised on the cut's clock. The map's own `DoorShot.Beat` (KeyInsert … BoltRetract …) and `ExitLockChanged` (A13's `DoorExitChainDrop`) still fire as today. For the EXIT, `Unlock.ShackleRelease` is the commit's sound.

| Name | t (s) | Where |
|---|---|---|
| Unlock.PinTick ×5 | 0.57, 0.59, 0.61, 0.63, 0.65 | padlock |
| Unlock.KeySeat | 0.68 | padlock |
| Unlock.KeyTurn | 0.68 | padlock |
| **Unlock.ShackleRelease** | **0.86** | padlock |
| Exit.ShackleSwivel | 0.88 | padlock |
| Unlock.KeyOut | 0.92 | padlock |
| Exit.ChainSlip | 0.93 | padlock |
| Exit.ChainRun | 0.95, 1.10 | padlock, then staple |
| Exit.PadlockKnock | 1.22 | staple |
| Exit.ChainSway | 1.22 (decays to about 4.2) | staple |
| Exit.ChainFloor | 1.38 | the tail on the floor |

## 8. Budgets

| | Main today (A13) | As built |
|---|---|---|
| LOCKED, LOD0 | 5,376 (chain + padlock) + eye bolt 528 | chain 5,040 + padlock 3,980 + staple 2,496 = **11,516** |
| LOCKED, LOD2 (far) | 1,452 + 528 | 1,260 + 344 + 140 = **1,744** |
| Draws per EXIT | 3 | about 8 near (chain 1, body 2, shackle 1, plug 2, staple 2), 7 far; one EXIT per floor; SRP-batched |
| Meshes cached | 7 | 6 chain meshes + the four kits; 1 live chain mesh for the session |
| Per frame | 0 B | 0 B (static buffers; the live mesh is rewritten in place) |
| Cut only | — | about 3.7 s of live chain; the cross-cut (Off only, 0.45 s): one screen-size RGBA8 + D24 texture (about 16 MB at 1080p, 13 MB on an iPhone at 0.75) |

## 9. Build, package, gate, harness

**Clone:** `W/proj_unlock`.
- Re-synced from main a0f70f33 + its working tree at 00:5x.
- The package's payload, plus the map glue (C1), plus the clone-only harness `Assets/Editor/Audit/Unlock/FrontRoomsUnlockCutProbe.cs` (copy in `W/unlock_work/harness/`, never packaged).

**Package:** `W/apply/unlock_cutscene.sh` (apply convention v2), 27 paths. Main's working tree is moving (another line's landing is in progress), so the bases were recorded at 01:29.

Replaced (main's .meta kept):

| sha1 | Path |
|---|---|
| 3f1be004 | Assets/Scripts/Rendering/Exit/FrontRoomsExitChain.cs |
| e8eecd3d | Assets/Scripts/Rendering/Exit/FrontRoomsExitChainKit.cs |

New:

| sha1 | Path |
|---|---|
| 92baa530 / ccfb82e4 (.meta) | Assets/Scripts/Rendering/Exit/FrontRoomsExitPadlock.cs |
| 297896b9 / a61ca242 (.meta) | Assets/Scripts/Rendering/Exit/FrontRoomsExitUnlockCut.cs |
| af3fea24 / 50a1b00e (.meta), json 73979872 / f8078687 (.meta) | Assets/Resources/Props/Models/Kit_Padlock_Body.fbx |
| 29fd887b / dea2786e (.meta), json df978c8c / 0c4f60f1 (.meta) | Assets/Resources/Props/Models/Kit_Padlock_Shackle.fbx |
| 112939db / f7c5481d (.meta), json c54be60e / a59296c0 (.meta) | Assets/Resources/Props/Models/Kit_Padlock_Plug.fbx |
| e112416a / 2fcad485 (.meta), json ab7c0f50 / a03845c6 (.meta) | Assets/Resources/Props/Models/Kit_ChainStaple.fbx |
| 3dc6a41c, d57d1308, ca055e93, 987f6f14, c8ef2b20 | Tools/Blender/frontrooms_kit/assets/exit_lock_common.py, exit_padlock_body.py, exit_padlock_shackle.py, exit_padlock_plug.py, exit_chain_staple.py |

**Gate** (Red 01:00: compile only):

| Check | Result |
|---|---|
| Roslyn, main + payload, macOS | COMPILE OK: Assembly-CSharp 194 files, Assembly-CSharp-Editor 81 files — 0 errors |
| Roslyn, main + payload, iOS defines | COMPILE OK: the same counts — 0 errors |
| Roslyn, main + payload + C1 + harness, macOS | COMPILE OK: Assembly-CSharp 194, Assembly-CSharp-Editor 82 — 0 errors |
| Warnings | none from the payload, the contract or the harness |
| Logs | `W/logs/unlock/roslyn_final_*` |
| `bash W/apply/unlock_cutscene.sh` | **DRY RUN OK**: 27 bases (2 at their sha1, 25 absent), 2 kept .meta, 27 payload files, 10 new GUIDs unused, main API declared |
| Rehearsal on a scratch copy of main (`W/unlock_work/rehearsal`) | **MANIFEST OK** for all 27 paths, then ALREADY APPLIED |

**Apply** (not run by this stage): `bash /Users/redwang/FrontRoomsVisualWork/apply/unlock_cutscene.sh --apply`, then C1 by 关卡设计.

**The harness for the video** (combined test; ≤ 2 visual Unity jobs, `W/tools/unity_jobs.sh` first; silent by FrontRoomsCloneOnlySilence, rule 24):

```
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/redwang/FrontRoomsVisualWork/proj_unlock -buildTarget OSXUniversal -executeMethod FrontRoomsMainScenePlaytest.RunBatch -autopilotExit -autopilotStation 4 -autopilotSeed 2554 -unlockProbe -unlockLabel desktop -logFile /Users/redwang/FrontRoomsVisualWork/logs/unlock/probe_desktop.log
ffmpeg -y -framerate 60 -i /Users/redwang/FrontRoomsVisualWork/proj_unlock/Verification/unlock/desktop/frames/f%04d.jpg -c:v libx264 -pix_fmt yuv420p -an /Users/redwang/FrontRoomsVisualWork/proj_unlock/Verification/unlock/desktop/unlock_cut.mp4
```

- The same with `-unlockMotionOff -unlockLabel motion_off` gives the cross-cut version.
- The probe:
  - steps game time at exactly 1/60 s from the shot's start;
  - renders every frame (the cross-cut's picture while it is on screen) to 1280 × 720;
  - writes `probe_report.txt`: per-frame shot / cut clock, shown state, key, cross-cut, cues, counts, kits present.

## 10. Not done here, and what the combined test must look at

- **No Unity import yet.** The four FBX kits have never been imported. The first import is in main at landing (FrontRoomsKitImporter sets LODs from the sidecars). The `.meta` files carry the slot remaps copied from the crossbar's importer settings.
- **Clearances** (§2.4) are computed at the pins and the path's control points, not for every link and frame. Look for the toe link against the latch case and the turn against the bar.
- **The in-between chain shapes** (0.93–1.22) are hand-set. Watch for a frame or two of chain through the opening leaf or the latch case as the leaf leaves at 1.00.
- **Re-measure the 30 m read** of the ⅜ in chain (70 §4). If it is lost, the fix is the finish (D3), not the size.
- **Expected counts:**
  - `CutsStarted == CutsFinished == 1` and `LockSwitches == 1` per unlock;
  - LOCKED never shown with the cut;
  - no leftover "A13 cut chain (live)" or "Key (unlock shot)" after 4.3 s;
  - with C1, the camera is centred on the padlock.
- **Timings marked [estimate]** wait on Q4.

---

## 11. Chain fall revision (options), 2026-11-01

**Line:** 游戏视觉, session local_5656cffd, account A, workflow unlock-chain-fall-options (design stage). **Options only: nothing landed in main.** Main is unchanged except this section and 10_research.md §11.

**Red's ask (11-01, verbatim):** "目前在当前游戏里的cut scene的设计上，确实把所有的结构做出来了，但是玩家在解锁之后，整个锁链和锁落下来的速度非常快，而且僵硬，我认为你需要优化这部分，给我五个优化方案的省内存的mp4视频让我告诉你哪一个更好".

**What this stage did and did not do.**
- The structure, the key, the release at 0.86 s, the shackle's swivel, the slip at 0.93 s, the map's camera and UNLOCKED are all kept. Only what happens after the slip changes.
- Five options are built in the clone `W/proj_unlock` behind a switch (`FrontRoomsExitUnlockCut.FallVariant`, harness flag `-unlockFall N`; 0 = today, unchanged).
- Each option was simulated outside Unity: the clone's real `FrontRoomsExitChainKit.cs` and the new `FrontRoomsExitChainFall.cs` compiled with a small UnityEngine stand-in (`W/unlock_fall/sim_test`, .NET 7), driven at 60 fps with the map's 10° leaf crack scripted, and checked on tuning sheets (`W/unlock_fall/preview/`, not deliverables). The times below come from that run.
- **Then all six (today + U1–U5) were run in Unity** through the probe in the clone. The runs were silent and took about 40 s each. Every cut finished and handed over to UNLOCKED (dresser `finished 1`), with no exception, and the sync points landed within a frame of the console (§11.3).
- The 1280 × 720 frames are in `W/proj_unlock/Verification/unlock/fall_uN/frames` with the comparison view (§11.6). Contact sheets for checking are in `W/unlock_fall/real/`.
- **No MP4 was encoded here.** That is the render stage's job (§11.6).

### 11.1 Audit: today's fall, frame by frame

Ported line for line from `FrontRoomsExitUnlockCut.Keyframes` / `PoseChain` (sha1 297896b9) and sampled at 60 fps (`W/unlock_fall/audit_today.tsv`). Link centres are placed on the path as `K.Place` does.

| Cut clock | What moves | Numbers |
|---|---|---|
| 0.86–0.93 | Release, shackle swivel, toe link rides to the toe | as §3 (kept by every option) |
| 0.93–0.965 | "Loosen": the turn's points pushed out to 55 mm from the bar's axis | the path grows from 1.155 to 1.305 m (+13 %) |
| 0.965–1.00 | "Off the bar": the free 0.385 m laid straight down in front of the bar | 2 frames. Height uses u^0.7, so it is fastest at the start (a fall is slowest at the start). The toe link drops 0.29 m in 3 frames: **8.7 m/s, 48 g**. A free fall reaches 8.7 m/s only after 3.9 m |
| 1.00–1.10 | The long side (0.95 m) goes from hanging at the bar to hanging from the staple at 25° | Linear blend of 64 points: links at **8 m/s, 26 g**. The chain shrinks to 1.055 m (−9 %) |
| 1.10–1.22 | The haul: the padlock rides 0.24 m up to the staple | u², so it arrives at full speed. The tail sweeps 0.33 m along the wall at up to **6.35 m/s** |
| 1.22 | The knock | A 6° wobble that is gone in 0.07 s |
| 1.22–4.22 | The sway | **One angle for every link** about the staple (12° · e^(−t/0.8) · sin 2πt/1.4); peak −7.8° at 1.56 s. Below 0.27 m the swing is faded to 0 to keep the floor tail still, so the bottom 0.25 m bends at one point. Swing at the first peak: 1.3 cm at 0.9 m height, 6.7 cm at 0.5 m, 9.4 cm at 0.3 m, 2.9 cm at 0.1 m |

**Why it reads fast.** From the slip to the knock takes 0.29 s. A 1 m drop alone takes 0.45 s, and a physical fall-and-haul takes about 0.8 s (U1). Every phase starts at full speed (u^0.7) or ends at full speed (u²) with nothing to catch it, and there is no weight in the stops.

**Why it reads stiff.**
- The chain moves as whole shapes. All 35 links start and stop on the same frame, with no lag, no whip and no link turning on the next.
- Its length breathes by ±10 %, so links crowd and spread.
- The sway is a rigid rotation: one angle, a 1.4 s period (a 1 m hanging chain swings at 1.67 s, research §11.1), and a 0.8 s decay, so it is nearly dead after one swing. No wave travels down the chain, and the foot never lags the top.

### 11.2 What the five options share

- **One class, `FrontRoomsExitChainFall`** (new, Rendering/Exit, about 1,080 lines). It takes over at exactly 0.93 from the landed pose at that instant, computed from the cut's own curves, not from whichever frame comes first. It returns the chain's path, the padlock's pose, the staple link's pin weight and the round-the-bar weight. The cut feeds these to `K.Place` and the same `ChainMesher` as today: same links, same LOD0 live mesh, nothing allocated per frame.
- **U1–U4 are simulated.**
  - Position-based dynamics: 35 link centres plus the padlock's centre of mass on a rigid rod from the heel-side end link.
  - Real g. Masses: 70 g a link (⅜ in proof coil, 2.0 kg/m) and a 0.38 kg padlock (research §11.1).
  - Links resist stretch but not compression: they can bunch to half a pitch.
  - A long-range attachment from the staple prevents stretch.
  - Contacts: the crash bar (a capsule), the leaf face and latch case (both moving with the leaf's crack), the jamb casing, the wall and staple plate, and the floor. Each has Coulomb friction. The padlock bounces off what it hits.
  - **The staple is a ring the chain runs through.** Its run is one coordinate, driven by the pull of each side. The pull is the force that side's hanging mass needs for its measured acceleration, M(a − g), so a falling side pulls nothing and a side caught at the bottom of its swing pulls more than its weight. The ring has static and kinetic friction and can catch at link joints (a clack).
- **The one cheat.** A wound turn cannot leave a bar fixed at both ends by gravity (research §11.1, capstan). Every option takes the turn off in 0.10–0.12 s by drawing its links forward and down (U2/U3: back over the top into a drape). Collision is switched off for those links until they are clear. This happens on the bar's far side and underside, which the dipped camera (46.7° down) cannot see, and it is done by 1.03–1.05, before the leaf's crack has moved the bar far.
- **The settle.** Each option eases every link onto UNLOCKED's links once the motion has died down. The last 0.3 s blends the path and the padlock onto `K.UnlockedPath` / `UnlockedPadlockAt` exactly, so the cached UNLOCKED meshes take over with no pop.
- **Determinism.**
  - Fixed 1/240 s steps, advanced from the cut's clock. The leaf's pose is read once a frame and interpolated between steps. The picture is the last step.
  - The same frame rate gives the same motion every run.
  - Measured at 30, 60 and 144 fps (console, `sim_test rates`):
    - **U1, U4 and U5 match within 0.1 mm**; the knock moves only by the frame step.
    - **U2 and U3**, whose drape rides the bar through the leaf's 0.1 s crack, match within 4 mm between 60 and 144 fps. At 30 fps they drift up to 9 cm mid-fall, because the crack is then read on only 3 frames; the knock moves by ≤ 0.08 s.
    - A production version could read the crack from the shot's clock instead of the leaf.
  - The last frame is exactly UNLOCKED on every device.
- **CPU (U1–U4).**
  - 36 bodies × 12 iterations × 4 steps per 60 Hz frame is about 1,700 constraint passes, plus contacts.
  - Measured **0.17–0.24 ms per frame** on this Mac in .NET 7, UNDER LOAD (load average 8–18 from other sessions, so not a clean figure). It covers the simulation and resample; `Place` and the mesh rewrite are as today and not included.
  - Phone [estimate, 3×]: 0.5–0.7 ms on an iPhone 12-class, for 4–5 s, once per floor.
  - U5: 0.07 ms.
- **Phone tier.** Identical motion on desktop, iPhone and iPad (three-platform parity). The live chain stays LOD0 (5,040 tris) as today. The cut lives 4.1–5.2 s instead of 4.22 s.
- **Reduced motion** (Camera motion Off). The landed cross-cut (a still close-up 0.55–1.00) is unchanged, and the fall plays in the in-place view. The chain's motion is the object's own, with no camera move. **U4 plays as U1** at Off (no time warp).
- **Sound sync points.** Every option keeps 0.57–0.93 as today (PinTick ×5, KeySeat, KeyTurn, ShackleRelease 0.86, ShackleSwivel 0.88, KeyOut 0.92, ChainSlip 0.93).
  - From 0.95 the fall raises the existing names at the times it reaches them: `Exit.ChainRun`, `Exit.PadlockKnock`, `Exit.ChainSway` and `Exit.ChainFloor`.
  - Three new names, which 声音设计 binds only for the option Red picks:
    - `Exit.ChainClack`: a link tips over the bar or catches in the staple.
    - `Exit.PadlockTap`: the open padlock hits the wall, jamb or plate before the knock.
    - `Exit.ChainClink`: a link knocks the wall during the sway (at most 6).
  - Today's director plays nothing for unknown names.

### 11.3 The five options

| | U0 today | U1 real gravity | U2 snag and slide | U3 heavy padlock | U4 slow beat | U5 keyed overlap |
|---|---|---|---|---|---|---|
| Release → knock | 0.36 s | **0.81 s** | **2.34 s** | **2.39 s** | **1.69 s** | **1.21 s** |
| Knock (cut clock) | 1.22 | 1.67 | 3.20 | 3.25 | 2.55 | 2.07 |
| Tail on the floor | 1.38 | 1.67 | 3.10 | 3.17 | 2.58 | 2.15 |
| Cut ends (UNLOCKED) | 4.22 | 4.12 | 5.12 | 5.02 | 5.15 | 4.72 |
| Second ChainRun | 1.10 | 1.27 | 2.37 | 1.63 (off bar), 2.12 (haul) | 1.57 | 1.67 |
| New sync points | — | PadlockTap 1.28 | ChainClack ×8 over the bar 1.23–1.78; staple clacks 1.35, 1.63, 2.05, 2.30, 2.57, 2.78; PadlockTap 1.30, 2.27 | PadlockTap 1.28, 2.27; ChainClack 1.30, 1.53 (drop), 2.25, 2.53, 2.75 (haul) | PadlockTap 1.60 | — |
| CPU / frame (console) | ~0 | 0.24 ms | 0.23 ms | 0.23 ms | 0.17 ms | 0.07 ms |
| Reduced motion | cross-cut | same motion | same motion | same motion | **plays as U1** | same motion |

**Measured in Unity** (probe reports, 60 fps, cut clock in s). These are the times 声音设计 would bind; ChainSlip stays 0.93 in all six.

| Option | ChainRun | ChainClack (new) | PadlockTap (new) | ChainFloor | PadlockKnock + ChainSway | Cut ends | Dresser |
|---|---|---|---|---|---|---|---|
| U0 today | 0.97, 1.12 | — | — | 1.38 | 1.23 | 4.217 | finished 1 |
| U1 real gravity | 0.97, 1.27 | — | 1.28 | 1.67 | 1.67 | 4.100 | finished 1 |
| U2 snag and slide | 0.97, 2.37 | 1.23, 1.33, 1.35, 1.43, 1.53, 1.62, 1.63, 1.68, 1.73, 1.78, 2.05, 2.30, 2.57, 2.78 | 1.30, 2.27 | 3.10 | 3.18 | 5.100 | finished 1 |
| U3 heavy padlock | 0.97, 1.63, 2.10 | 1.30, 1.53, 2.27, 2.53, 2.75 | 1.28, 2.28, 3.15 | 3.15 | 3.23 | 5.000 | finished 1 |
| U4 slow beat | 0.97, 1.57 | — | 1.60 | 2.58 | 2.55 | 5.133 | finished 1 |
| U5 keyed overlap | 0.97, 1.67 | — | — | 2.15 | 2.07 | 4.700 | finished 1 |

**Reduced motion:** U4 at Camera motion Off plays as U1 (knock 1.67, end 4.10). The cross-cut shows on 28 frames (0.55–1.00).

**U1 · Real gravity, quick and loose.**
1. The turn leaves the bar on its far side (0.93–1.03).
2. From 1.03 everything is physics with a smooth staple (μ 0.12 / 0.08). The 0.95 m long side falls from the bar and swings down along the wall from the staple. The open padlock swings on the 0.24 m lower run and taps the wall at 1.28.
3. Once the long side hangs, its weight runs the chain through the staple in 0.40 s (1.27–1.67). The padlock is hauled up and knocks the staple at **1.67**, with the tail landing at the same moment.
4. The hanging side then sways as a chain, foot lagging the top, damped 0.45 s⁻¹. The links settle onto UNLOCKED by 4.12.

It is still quick: 0.81 s from the release to the knock (2.3 × today), but every link moves on its own.

**U2 · Snag and slide, about 2.3 s.**
1. The turn opens into a **drape over the crash bar** (0.93–1.05) and hangs there. The staple's dry grip (μs 0.7 / μk 0.3) holds the padlock while the chain is on the bar. The padlock swings below the staple and taps the wall (1.30). The drape rides the leaf's 10° crack at 1.00.
2. From 1.22 the drape slides over the bar **one link at a time**: 8 steps of 0.11 → 0.05 s, a stick and a quick slip each, each a ChainClack, until the end flips over the top (1.83).
3. The long side falls and swings down (1.83–2.3).
4. The haul starts with three link catches of 0.06 s at the staple (clack, clack, clack), then runs. The knock is at **3.20**, and the cut ends at 5.12.

This is the most "mechanical" read: the chain visibly hangs up on the bar and has to work its way off.

**U3 · Heavy padlock, about 2.4 s.**
1. The turn becomes a drape that holds until 1.62. Meanwhile the open padlock's own weight runs the chain through a smooth staple **toward itself**, in two jolts. It drops about 0.18 m, to about 0.6 m above the floor, and swings as a 0.5 m pendulum. It taps the wall (1.28) and, on the way back, the jamb casing (2.27).
2. At 1.62 the drape lets go. The long side falls, and its weight hauls the padlock back up 0.4 m (three catches, then a run: 2.12–3.25).
3. The padlock knocks the staple at **3.25**, and the cut ends at 5.02.

The padlock is the hero here: it goes down first, swings and knocks, then is pulled up past where it started.

**U4 · Slow beat.**
1. U1's simulation, with its own clock at 1 × until 1.03, so the turn is off the bar before the leaf moves.
2. The clock eases to **0.4 ×** by 1.09 and holds there until 0.12 s after the knock, then eases back to 1 × over 0.25 s.
3. The map's camera (dip, return 1.15–1.55) and the key keep their own clock. So the camera comes home while the chain is still falling in slow motion: the "time stops for the lock" beat.
4. The knock is at **2.55**, and the cut ends at 5.15.

At Camera motion Off it plays as U1. Flaw: a slow object under a camera moving at full speed can read as a frame-rate hitch on a phone.

**U5 · Keyed with overlap (the cheapest, closest to today).** No physics, no contacts. Today's five key shapes, retimed to gravity:

| Phase | Cut clock | Curve |
|---|---|---|
| Loosen | 0.93–1.03 | ease |
| Off the bar | 1.03–1.25 | u²: a 0.3 m drop timed to g |
| Swing to the staple | 1.25–1.65 | ease |
| Haul | 1.65–2.05 | u², knock at **2.07** |

- The lower run is no longer held up by the toe link: it swings down about the staple to hang straight under it, as a damped pendulum (past and back, 0.93–1.65), and the haul draws it up through the staple. Today's keys left it hanging in the air.
- Each link follows its key through a damped spring: 42 rad/s at the padlock end, 13 rad/s at the free end, ζ 0.55. So the far links lag and overshoot (follow-through, research U43).
- Links are kept one pitch apart from the padlock end (follow-the-leader), so the length never breathes.
- The rigid sway becomes a **travelling wave**: period 1.6 s, decay 1.1 s, the foot lagging the top by 0.45 s and swinging wider.
- The cut ends at 4.72.

Flaw: the long side's in-between shapes are still today's straight lines (hanging straight down from the bar, a straight 25° trail), so it reads cleaner and more "animated" than U1–U4.

**Adapted from the brief, with why.**
- **U3** was briefed as "the lock's weight pulls it through". In our rig the long side is the heavier one, so the padlock can only pull the chain toward itself while the long side is held on the bar. U3 does exactly that, then reverses.
- **U5** was briefed as "settle and pile". The chain is 1.19 m and hangs from 1.00 m, so only 0.13–0.15 m (4 links) can ever lie on the floor and a pile cannot read. The per-link settle, the clinks and the floor spill are in U1–U4 anyway. U5 gives Red the other real choice: keys instead of physics.
- **Every option ends on today's UNLOCKED.** Red's Q3, the padlock against the staple and the chain hanging, is kept, so the persistent state and its cached meshes do not change.

### 11.4 Files changed in the clone (W/proj_unlock, resynced from main at 19:0x on 11-01; clone-only files saved in W/unlock_fall/cloneonly_before_resync)

| sha1 | Path | Change |
|---|---|---|
| 28132ed5 | Assets/Scripts/Rendering/Exit/FrontRoomsExitChainFall.cs | NEW: the five options |
| 7c9221c8 | Assets/Scripts/Rendering/Exit/FrontRoomsExitChainFall.cs.meta | NEW, guid 768738b32ad14128a6b974338e353777 (unused in main and clone) |
| 5a9e5bde | Assets/Scripts/Rendering/Exit/FrontRoomsExitUnlockCut.cs | base 297896b9. Adds `FallVariant` (static, default 0), `LeafDress`, `FallOption`, `Fall`, the option branch in `Pose` / `PoseChain` / `RaiseCues` / the end test, and `StepFall` / `FallCues`. With 0 every landed line runs as before |
| 3af699b1 | Assets/Scripts/Rendering/Exit/FrontRoomsExitChain.cs | base 3f1be004. One line: the cut gets `LeafDress = d.leafDress` |
| 0d4a9d6c (render stage; was 13d986fb) | Assets/Editor/Audit/Unlock/FrontRoomsUnlockCutProbe.cs | CLONE ONLY, never packaged (copy in W/unlock_work/harness). Adds `-unlockFall N` (label fall_uN), `-unlockHoldView` (the comparison camera held from 1.15, the autopilot paused from 1.40), `-unlockHoldAt X`, `-unlockQuitAfter`, and the fall's clock, knock and end in the report. Render stage: `-unlockRecordFor S` (record at least S s, so every option has the same frames) and the camera-motion fix in §11.6 |

### 11.5 Gate (rule 25: Roslyn only)

| Check | Result |
|---|---|
| Roslyn, main + the 4 clone files as overrides (zsh array), macOS | `COMPILE OK: … Assembly-CSharp 212 files, Assembly-CSharp-Editor 85 files — 0 errors; 4 overrides applied` |
| Roslyn, same, iOS defines | `COMPILE OK (iOS defines): … Assembly-CSharp 212 files, Assembly-CSharp-Editor 85 files — 0 errors; 4 overrides applied` |
| Warnings from these files | none |
| Logs | `W/logs/unlock_fall/roslyn_mac`, `roslyn_ios` |
| Console simulation, all five | end on UNLOCKED exactly; no NaN; `W/unlock_fall/cues_all.txt`, `preview/sheet_u1..5.png`, `preview/curves.png` |
| Unity probe, all six plus U4 at Camera motion Off | every cut `finished 1`, `cancelled 0`; no probe exception; logs `W/logs/unlock_fall/fall_u*.log`; sheets `W/unlock_fall/real/u*_sheet.jpg` |

### 11.6 Rendering the six videos (render stage, done 11-01)

At most one Unity process for this line; run `W/tools/unity_jobs.sh` first. Silent: FrontRoomsCloneOnlySilence, no `-audioCapture`. For N in 0 1 2 3 4 5:

```
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/redwang/FrontRoomsVisualWork/proj_unlock -buildTarget OSXUniversal -executeMethod FrontRoomsMainScenePlaytest.RunBatch -autopilotExit -autopilotStation 4 -autopilotSeed 2554 -autopilotBodyDoors -unlockProbe -unlockFall N -unlockHoldView -unlockQuitAfter -logFile /Users/redwang/FrontRoomsVisualWork/logs/unlock_fall/fall_uN.log
ffmpeg -y -framerate 60 -i /Users/redwang/FrontRoomsVisualWork/proj_unlock/Verification/unlock/fall_uN/frames/f%04d.jpg -c:v libx264 -preset slow -crf 28 -pix_fmt yuv420p -movflags +faststart -an /Users/redwang/FrontRoomsVisualWork/proj_unlock/Verification/unlock/fall_uN/fall_uN.mp4
```

- `-autopilotBodyDoors` is needed since body doors went live (Level0 `bodyDoors: 1`, 10-31 21:01). Without it the autopilot stands at the start door (try 1, 11-01 19:47, killed at 60 s of game time). The body-door bot walks into doors and uses the key only through the real unlock shot.
- The first run after the resync imports about 570 changed main files: allow time.
- `-unlockHoldView` does two things:
  - From 1.15 on the cut's clock (the start of the map's return) it renders from the shot camera's pose at that moment, held still. The whole fall then stays in frame for Red's comparison.
  - It pauses the game's autopilot from 1.40 until the recording ends. Without that, the autopilot walks through the EXIT and the next floor's build ends the cut at about 3.2 s (try 2).
- In the game the camera still returns over 1.15–1.55. From the player's eye the chain is a sliver at the right edge of the door frame, and its lower half is out of frame (U1 try 3). `-unlockHoldAt 1.55` renders that view instead.
- `-unlockQuitAfter` quits Unity once the report is written, so the run does not play on through three floors.
- `-unlockMotionOff` adds the reduced-motion versions (label fall_uN_motion_off).
- Check in the report: the fall option line, the cue times against §11.3, CutsFinished 1, and no "probe step exception".
- Red asked for small MP4s ("省内存"): H.264 CRF 28, 1280 × 720, no audio stream, about 0.5–1.5 MB for 5 s [estimate].

**Rendered (render stage, 11-01 20:11–20:15).**
- All six were run again with the final code (U1 and U4's design-stage frames predate the last edit of the fall file), one Unity at a time, adding `-unlockRecordFor 5.5`. Every run: camera motion 100, the same held view from 1.167 (256.81, 1.40, 41.40, fov 62.0), `finished 1`, no exception, cue times as the Unity table in §11.3. Script `W/unlock_fall/tools/render_all.zsh`; earlier frames moved to `W/_partial/unlock_fall_frames_designstage_20261101`.
- **Trap found.** The design stage's last run (`-unlockMotionOff`, 20:01) saved `FrontRooms.Comfort.CameraMotion = 0` in the editor PlayerPrefs (`~/Library/Preferences/unity.Red Wang.FRONTROOMSSS.plist`), which main's editor and every clone of this product share. The first render attempt (20:07) therefore played with Camera motion Off and was discarded. The probe now sets 100 for a normal run (which repaired the saved value; it reads 100 again) and puts back what it found after a motion-off run.
- Videos (`W/unlock_fall/tools/build_videos.py`): 1:1 from the shot's start to 5.40 s, then cut clock 0.80–3.60 at 0.5×; caption top-left, FRONTROOMSSS logo top-right; 1280 × 720, 30 fps, H.264 High CRF 26 (preset slow), yuv420p BT.709 limited, `+faststart`, one video stream and no audio. Each is 11.07 s and 0.80–1.01 MB.
- Deliverables in `video/` next to this file: `unlock_fall_today.mp4`, `unlock_fall_U1.mp4` … `unlock_fall_U5.mp4`, and `unlock_fall_sheet.jpg` (Shot format, 6 × 6: options × the same six moments, WAIT-RED). Red's decision note: `30_for_red.md`.

### 11.7 Limits, honestly

- **Unity check (11-01, U1, try 2).** In Unity the cut's sync points landed on the console's frames:

  | Cue | Cut clock |
  |---|---|
  | ChainSlip | 0.933 |
  | ChainRun | 0.967, 1.267 |
  | PadlockTap | 1.283 |
  | PadlockKnock, ChainSway, ChainFloor | 1.667 |

  Real frames show the turn dropping off the bar with no visible pass-through from the shot camera.
- **Most of the slow options play in the player's view, not the dip.** The map's camera comes home over 1.15–1.55, while the slow options knock at 2.6–3.3. From the player's eye (1.62 m, about 1 m back) the chain is a small object at the right of the door frame. Holding the dip until the knock would need the map's shot timing (`FrontRoomsShotTimings.Unlock.ReturnStart`, 关卡设计's), so it is a contract and not part of these options. It is worth asking Red together with the pick.

- **Never seen in Unity yet.** Contacts are tuned against boxes and a capsule standing in for the latch case, the jamb casing and the bar. The latch case's height (0.92–1.08 m) is assumed, not read from the kit. Watch for a link through the latch case or the casing, and for the hidden cheat showing from the actual shot camera (§11.2).
- **No twist dynamics.** Link orientation is today's parallel transport along the simulated centre line, alternating 90°. Links turn as the line moves, but there is no torsion.
- **The haul is one coordinate,** not a contact between wires. The catches are scripted to the first two or three joints, and a haul that stalls is carried on from a set time (U1 1.8 s, U2 2.2 s, U3 2.25 s) so that every run reaches the knock.
- **The settle pulls the last 1 s onto UNLOCKED.** The simulated chain hangs straight under the staple; UNLOCKED hangs 6 cm past it, away from the door, so the line drifts about 5 cm that way, and the floor tail takes today's curl.
- **The chosen option's production package** (dedupe the keyframes, LOD the live chain, measure the phone) waits on Red's pick, and so do 声音设计's new sync times.

### 11.8 Production: U1 without the forced straight end (landed 2026-11-01 21:27, account F)

Red, 11-01, on U1's end frame (the chain hanging dead straight from the staple): "可以选择U1，但是我不喜欢后面它被force成直的这部分". This supersedes the last two bullets of §11.7: there is no settle and no option switch any more.

**What the player sees.** Up to the knock the fall is U1 as Red saw it (same cues, frame for frame). After the knock the hanging side sways once or twice and stops where its last swing leaves it: hanging from the staple with a soft bow (25–50 mm off a straight line), the last five links lying along the foot of the wall toward the far side (the J that the U1 option video shows at 2.4–2.8 s), the open padlock hanging tilted under the staple's door-side end. Nothing pulls it into a drawn shape. Still from about 2.7 s, asleep at 3.50, the cut ends at 3.88–3.90 and the static UNLOCKED takes over with no visible change.

**How (FrontRoomsExitChainFall, one behaviour, no switch).**
- After the 0.10 s cheat off the bar, everything is the simulation to rest (fixed 1/240 s steps, 12 iterations).
- The knock: the staple's link sits on the staple's wire; the heel link and the long side come to hang from its two ends. The two hold points slide out from the apex and the reach tightens from a pitch to a hang over 0.25 s. The first production build switched this hold on at once: a 2 cm step in one 1/240 s step kicked the long side 35 cm up at about 1.95 s (the "whip"). With the slide there is no kick.
- Friction from the knock (in over 0.3 s): link friction 2/s, the padlock's shackle friction 6/s, joint stiction below 0.08 m/s (20/s), carpet static 1.4 / sliding 1.0 (a link that lands stays). The padlock was the last mover before its own damping (air damping only, still swinging at 5.8 s).
- Sleep: every link and the padlock under 4 mm/s for 0.25 s (at the latest 5.5 s after the slip).

**UNLOCKED = the fall's own rest, baked.** `FrontRoomsExitChainKit.RestNodeData` (the 35 link centres) and `RestPadlockData` (top-face centre + rotation), from the console harness ("bake": 60 fps, the map's 10° crack). `PlaceNodes` lays the links on them with the same pins as the cut's last frame (heel link on the open bow, the staple's link on the staple, wrap 0, full rest twist), so the cached UNLOCKED mesh is the cut's last frame: after the cut, on every rebuild and restore, on every floor, when the cut is skipped or cancelled after the release. No straight hang is drawn anywhere (`UnlockedPathRaw` and `UnlockedHeelOffset` are gone). Live, the cut closes from where its own run fell asleep onto the bake over 0.40 s.

| Determinism (console, rest vs the 60 fps bake) | Max link gap |
|---|---|
| 30, 50, 72, 90, 120, 144 fps | 0.00 mm |
| the crack 1 or 2 frames late, or no crack | 1.41 mm |
| 1e-6 m noise on the slip (3 seeds) | ≤ 0.05 mm |
| 1e-4 m noise (2 seeds) | ≤ 0.96 mm |
| the close onto the bake at 30 / 60 / 144 fps | 0.000 mm |

**Cost.** Console (.NET 7, this Mac): 0.21 ms a frame at 60 fps from the slip to 3.9 s (0.49 ms at 30 fps, more steps a frame; worst frame 0.84 ms); nothing after the cut. The same simulation on desktop, iPhone and iPad (the rest must match the bake). iPhone not measured (rule 25: combined test).

**Reduced motion** (Camera motion Off): the fall is the same; the cross-cut only changes the camera, so it ends on the same rest.

**Sync points for 声音设计** (Unity, final build, 60 fps, cut clock; names only, no Audio file touched). Up to 0.93 nothing changed (PinTick ×5 0.57–0.65, KeySeat / KeyTurn 0.68, ShackleRelease 0.86, ShackleSwivel 0.88, KeyOut 0.92, ChainSlip 0.93).

| Name | t (s) | Where (world, this run) | Note |
|---|---|---|---|
| Exit.ChainRun | 0.967 | the bar's far side (256.80, 1.03, 41.88) | the turn leaves the bar |
| Exit.ChainRun | 1.267 | staple (257.14, 1.00, 41.88) | the long side starts running the chain through the staple |
| Exit.PadlockTap (new) | 1.283 | padlock (257.23, 0.70, 41.90) | the open padlock taps the wall once on the lower run |
| Exit.PadlockKnock | 1.667 | staple | the padlock jams against the staple |
| Exit.ChainSway | 1.667 | staple | visible sway about 1 s (to about 2.7), still by 3.5: shorter than today's 3 s rigid swing |
| Exit.ChainFloor | 1.667 | the tail (257.48, 0.02, 41.91) | first link on the carpet; the tail lays along the wall foot by about 2.5 (carpet, no new event) |
| Exit.ChainClink | — | — | in the code; not raised in this fall |
| cut end | 3.88–3.90 | — | the static UNLOCKED takes over (nothing to play) |

**Files** (W/apply/unlock_fall_u1, base = main 11-01): `FrontRoomsExitChain.cs` 3f1be004 → d3d71ea9, `FrontRoomsExitChainKit.cs` e8eecd3d → 91535d7e, `FrontRoomsExitUnlockCut.cs` 297896b9 → f6c53862, NEW `FrontRoomsExitChainFall.cs` 6732604c (+ .meta 7c9221c8, guid 768738b3…). Gate: Roslyn macOS + iOS COMPILE OK (4 overrides), DRY RUN OK, rehearsal MANIFEST OK; landed MANIFEST OK. Unity in the clone (W/proj_unlock, two silent probe runs): cues within a frame of the console, cut over at 3.883.

**Video for Red:** `video/unlock_fall_U1_final.mp4` (13.07 s, 0.99 MB, no audio): 1:1 to 5.40 s and the fall again at 0.5x (the option videos' camera), then 2 s of the settled UNLOCKED from the player's eye after the cut.

**Combined test (rule 25).** Unlock the EXIT on any floor (desktop, iPhone, iPad; Camera motion 100 and Off): the chain falls as U1 and rests as a J with the tail along the wall foot, no straight hang, no pop at about 3.9 s; leave and come back (rebuild) and take the next floor: the same rest; iPhone frame time over 0.9–3.9 s.
