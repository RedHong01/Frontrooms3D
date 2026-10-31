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
