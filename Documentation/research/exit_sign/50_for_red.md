# EXIT sign: two calls for Red (A/B captures, 2026-10-08 19:5x)

**DECIDED 2026-10-21 (Red, "全部按推荐" in the 游戏视觉 chat): R3 = exit_sign_fix1b, the Run sign's downward spot light and the red-only diffuser (no red on the Run ceiling).** Colours were approved by 平面视觉 · 信息设计 on 10-15.

These are captures only. Nothing was packaged, and nothing in main changed except this folder and two rows in `Documentation/VERIFICATION_LOG.md`.

**How the captures were made:**
- **Clone:** `W/proj_es_ab`, made with `mkclone.sh --feature exit_sign`. It is main `bc0aa79` plus the working tree, synced at 19:45, so the EXIT sign merge is in it.
- **Harness:** `FrontRoomsExitSignABCapture`, clone-only. A copy is in `red_calls_code/`. It uses the integrate stage's Play-mode start: seed 4242, which gives run 516574485, with the Relay held dormant. It renders the title profile rooms at (4992, 0, 4992) through the game camera, at vFOV 76, with post (postExposure 0.15, ACES) and 4× MSAA, at 1920 × 1080.
- **Same build, same exposure:** A and B are toggled in place on one build. The troffer deal (lamps 0, 5, 8, 10 and 11 on, as in VL210), the camera and the exposure are the same in both, and `shots.tsv` logs each of them per frame.
- **Signs forced LIT:** both Run signs are forced to LIT. The hash had dealt LIT to both anyway.
- **Noise check:** A was rendered a second time after B. A against that second A differs on only 0.06–0.36 % of pixels, all from film grain. A against B differs on 28–42 % of pixels in the Run room.
- **Values below:** Y is the relative luminance of the graded frame, from a small window at a projected point (`images/red_calls/red_calls_measure.json`).

---

## 1. Critic issue 2: "a lamp hanging under the sign"

**Update 2026-10-14 (`exit_sign_fix1`, `60_fix1.md` §8).** fix1 keeps A's position and makes the light a spot pointing straight down. That removes both of A's symptoms (no dark copy on the ceiling, no glowing underside). B as specified below then fails the new T2-Run gate (letters 0.26–0.28× the ceiling ring against 4.2–5.7× for fix1), and B with the spot leaks light through the sign's seams. The call is still Red's; the numbers below are from 10-08, before fix1.

**Question:** should the Run sign's red light move inside the housing, with the sign casting no shadow?

**Board:** `images/red_calls/es_rc1_run_sign_light_ab.jpg`. Rows are 1.5 m, 4 m and 8 m before sign B, plus a wide frame. The eye is at 1.62 m, pitched up 20°. At 8 m, sign A is 2.2 m ahead and fills the frame. Slide: VL221.

| | **A: main today** | **B: light inside the sign** |
|---|---|---|
| Light | Kit `glow` anchor, y 1.96 m, 0.33 m under the housing | Kit `centre` anchor, y 2.394 m, inside the housing |
| Sign shadows | The sign casts | Both LOD renderers use `ShadowCastingMode.Off` |
| Light colour, intensity, range, shadows | (1, .10, .06), 3.6 × Level, range 9.5 m, soft .85 | unchanged |
| Ceiling 0.4 m beside the sign (the dark copy) | Y 0.088 at 1.5 m; 0.095 for sign A at 8 m | 0.219 (×2.5); 0.232 (×2.4). The copy is gone |
| Ceiling 1.2 m out | 0.150 | 0.132 (−12 %) |
| Ceiling just above the sign | 0.209 | 0.244 (+17 %). The red now gathers round the sign |
| Housing underside | sRGB (210, 152, 104), a salmon glow | (77, 67, 35), an unlit box bottom |
| Letters | Y 0.086 | 0.087 (no change: the letters are emissive) |
| Floor under sign B / sign A / between them (wide frame) | 0.236 / 0.175 / 0.136 | 0.209 / 0.149 / 0.136 (−11 % / −15 % / 0) |

**What B costs:**
- **Less red on the floor.** The floor pool under each sign is 11–15 % dimmer, because the light is 0.43 m higher. The walls take the pool higher too, so the room reads a little less red at foot level and a little more at the ceiling.
- **No troffer shadow from the sign.** The sign no longer shadows the troffers' light either. In a Run room only one troffer in three burns, and the sign is a 0.35 m box hanging 0.4 m from the ceiling, so the lost shadow is too small to see in these frames.
- **A cheaper fix than a shadow layer.** A custom shadow layer would keep the troffer shadow, but it needs URP Rendering Layers switched on for the whole pipeline (`FrontRooms_URP.asset` has `m_SupportsLightLayers: 0`). B instead removes 2 renderers per sign from every shadow pass, which makes it slightly cheaper than A.
- **Light count:** unchanged, on desktop and WebGL alike.

**Recommendation: B.**
- **It removes both symptoms.** B takes away the dark copy on the ceiling and the lit underside, so the sign reads as the source of the red, not as a box with a bulb under it.
- **The face does not change.** The letters are emissive, so B leaves them exactly as they are.
- **What the change is:** in `FrontRoomsRoomStream.BuildHangingExitSign`, use the `centre` anchor instead of `glow` (fallback y 2.394 − RoomHeight), and set the spawned sign's renderers to `ShadowCastingMode.Off`.
- **Where it goes:** it belongs in the `exit_sign_fix1` package, next to issue 1 (`EmissionGain`). The two are independent: issue 1 changes the letters' emission, B changes where the light sits.
- **If the floor then reads too pale:** raise the light from 3.6 to about 4.2 (1 / 0.85). This is untested. It would also brighten the ceiling halo by the same factor.

---

## 2. VL211: the Exit room's sign face reads mid-grey

**Question:** should the Exit room's wall sign get the spec §8.6 desktop spill light?

**Board:** `images/red_calls/es_rc2_exit_spill_ab.jpg`. It shows the sign from 2 m and 6 m, with the far door shut as the player arrives, aimed at the sign, plus nearest-neighbour crops of the sign. Slide: VL222.

| | **A: main today** | **B: spill light (spec §8.6, as `30_integration.md` §9 proposed)** |
|---|---|---|
| Light | none | Point at the kit `glow` anchor (0, −0.02, 0.06): world y 2.654, 6 cm off the header. Colour (1, .13, .07), intensity 0.5, range 1.5 m, no shadows |
| Face enamel at the sign's ends, 2 m | sRGB (48, 45, 26), Y 0.026 | (60, 46, 27), Y 0.030 (+17 %): redder, not whiter |
| Face enamel, 6 m | Y 0.037 | 0.041 (+10 %) |
| Letters (X), 2 m / 6 m | Y 0.156 / 0.136 | 0.165 / 0.154 (+6 % / +13 %) |
| Housing underside (just under the sign) | Y 0.012 | 0.565 (×47): a near-white hot spot, sRGB (211, 196, 161) |
| Ceiling above the sign | Y 0.029 | 0.218 (×7): a red halo |
| Wallpaper 1.7 m to the side | 0.019 | 0.018 (out of range) |

**What B does:**
- **It cannot light the face.** The anchor puts the light 2 cm under the housing and 6 cm off the wall, almost in the plane of the face, so the face only gets grazing red light.
- **It recreates issue 2.** What B adds is a bright spot right under the sign and a red halo on the ceiling: the same "lamp under the sign" read that issue 2 removes from the Run room.
- **The letters lose contrast.** At 6 m they sit in a red haze instead of on a dark plate.

**Why the face is grey:** in this room the white enamel sits above the troffers' cones. The troffers are spots at 2.85 m, so the top 0.25 m of the end wall gets only grazing light (`30_integration.md` §9). The wallpaper beside the sign is just as dark.

**Recommendation: A (leave it).**
- **The sign already works.** At 2 m and 6 m the letters read as a lit EXIT on a dim plate, which is how a real sign high on a dim wall looks.
- **Do not ship the spill as specified.** It adds a hot spot and does not whiten the face.
- **If Red wants a whiter face later,** the lever is white room light on the end wall: an Exit-room troffer row nearer the far wall, or a brighter face albedo. A red point light is not that lever. Both are untested here.

---

## Files

- **Boards:** `images/red_calls/es_rc1_run_sign_light_ab.jpg` (call 1) and `images/red_calls/es_rc2_exit_spill_ab.jpg` (call 2).
- **Slide strips** (rows of the boards): `es_rc1_pair_{d1p5,d4,d8,wide}.jpg`, `es_rc2_pair_{d2,d6}.jpg`, `es_rc2_crops.jpg`.
- **Numbers:** `images/red_calls/red_calls_measure.json`.
- **Code and logs:** `red_calls_code/`. It holds the harness, the board script, the runner, `shots.tsv` (per frame: option, eye, light state, troffers, exposure), `points.tsv` (projected measuring points) and `log.txt`.
- **Raw PNG frames** (A, B and the second A, 20 frames): `W/es_ab/frames/`.
- **Clone:** moved to `W/_partial/proj_es_ab`.
- **Verification log:** VL221 `2963:6869` (call 1) and VL222 `2963:6881` (call 2), both WAIT-RED.
