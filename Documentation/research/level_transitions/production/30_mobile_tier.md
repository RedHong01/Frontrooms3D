# Level transitions · production · 30 · Mobile tier

Date: 2026-10-31. 游戏视觉 (account A, session local_5656cffd), workflow stage "transitions mobile tier". Package `W/apply/transitions_mobile.sh`.

**Red, 2026-10-30, on `20_build.md` §9:** option A, "桌面版最高，然后移动版可以思考如何保证帧率的情况下尽可能高清显示". So the desktop keeps the hero detail and the 3-in-10 share, and this package does not change it. Phones get the highest detail I judge will hold the frame rate.

**Not measured yet.** Red, 2026-10-31 01:00: "做的时候先不用测，直接做，然后等都落地了一起测即可". The only gate is Roslyn (macOS + iOS, 0 errors) plus the package's dry run and rehearsal. Every number below is an ESTIMATE from `20_build.md` §6, `12_var_renovation.md` §8–9 and the settings presets. The combined test (§4) measures them.

---

## 1. What each platform draws

| | Desktop (Mac, Windows, Editor) | iPhone / iPad: **phone tier** (new) | iPhone / iPad on LOW or in Low Power Mode | WebGL |
|---|---|---|---|---|
| Gate | `!PhoneTier`, `!Reduced` | `FrontRoomsTransitionKit.PhoneTier` (handheld player or Editor handheld preview, preset not LOW, no Low Power Mode) | `Reduced` (as every handheld before this package) | `Reduced` (compile-time, unchanged) |
| Layout, share | hero, 3 in 10 | **the desktop's, crossing for crossing** (the fray keeps the desktop's random draws) | reduced (D4: some tiles move) | reduced |
| Pieces | all | **all**: props, hanging strips, islands, scraps, tape, white lip, carpet fray | no props, strips, islands, scraps, tape, lip or fray | as LOW |
| Paper, remnants, fray sampling | 4–4.5 mm | **8 mm** (`PhoneStepMm`) | 12 mm | 12 mm |
| Props | LOD0 → LOD1 → cull (kit LODGroup) | **LOD1 from the near plane** (LOD0 dropped, as the door break and the ALARM ZONE tell on phones) | none | none |
| Frame | hero profiles | **hero profiles** (fluted saddle, cove base, 3-segment sleeve) | flat saddle, square base, 1-segment sleeve | as LOW |
| Base | own renderer, no shadow | **in the block's Cove_Base trim mesh** (one renderer fewer per chunk) | in the trim mesh | in the trim mesh |
| Near-detail rings | none | **fine ring 12 m (hide past 15)**: paper lip / curls / backs, floor scraps, tape, chalk, cable; **mid ring 24 m (hide past 28)**: hanging strips, loose carpet tiles, binder bars and saddles, wall angle, corner guards, galvanised beads. `FrontRoomsTransitionNearDetail`, every 4th frame | none | none |
| Shadows | as built | scraps, tape, chalk and cable cast none (flat or hair-thin); everything else as on the desktop | as built | as built |
| Renovation textures | authored size (2048 / 1024 / 512) | **iOS tab: the authored size (cap 2048)**, compressed as before (ASTC, normal quality) | same textures (the iOS tab is per platform, not per tier) | WebGL tab 1024, unchanged |

Android follows the phone tier in code. Its texture tab stays at 1024 because Android is not a shipping platform.

---

## 2. Why these choices

- **Phones were already on the reduced tier, and it missed its own draw target:** 24–72 kit renderers in the frustum against 40 (`20_build.md` §6, D1). It also showed none of the renovation's character up close: no props, no hanging strips, no islands, no lip, no fray. On an iPhone the limits are draws, shadow passes and the chunk-build hitch, more than triangles. Phones default to HIGH, which renders about 3.2 Mpx at 60 fps with lamp shadows on.
- **Same layout as the desktop:** the phone tier runs the desktop code path (`R` false) and only changes the step count. The one shared random stream that depended on the step count was the fray. `FrayPhone` draws exactly the desktop's 4 mm draws and reads the coarser loop from them, so the tiles, chalk and ceiling land where the desktop puts them. The kit test checks every counter against the desktop.
- **8 mm paper:** option B (9 mm on the desktop, props at LOD0) measured +111 k (shot 2) and +137 k (shot 4) triangles in view. 8 mm is slightly finer. The finest tear octave (11 mm wavelength, 1.5 mm amplitude) is about 2 px at 1 m on a phone. The fine ring then removes most of the lip's triangles, and the lip was the largest item: `Paper_Backing` +55 k on shot 1.
- **Rings:** kit renderers are one per chunk × material, and there are about 0.9 renovated crossings per chunk. Beyond 12 m lies roughly 9/10 of a 46 m view's floor area, and beyond 24 m roughly 3/4 (geometric upper bound; walls hide part of it). So the fine ring draws about 1/10 of what it would and the mid ring about 1/4. The distance is measured to the nearest point of a renderer's bounds, so a renderer shows while any of its pieces is in range. Hysteresis of 3–4 m prevents flicker at the edge.
- **Props:** the kit LODGroups already cull a 0.4 m prop below 3 % of the screen: about 8.5 m at lodBias 1, about 6 m at the iPhone's Medium quality level (lodBias 0.7). The phone draws their LOD1 (1,972 / 1,018 / 740 / 438 tris).
- **Textures:** iOS imports with ASTC at normal quality. Assuming 6×6 blocks, a 2048 map with mips is about 2.5 MB and a 1024 one about 0.62 MB. The 15 maps authored at 2048 therefore add about +28 MB (about 44 MB for all 30 maps, against about 16 MB at the old cap). They load with the first renovated crossing, as on the desktop. The Office carpet and ceiling are already at 2048 on iOS, so a stripped or primed drop no longer looks softer than the paper next to it.
- **LOW / Low Power Mode keep the reduced tier:** a player who picks LOW, or an iPhone that throttles to 30 fps in Low Power Mode, gets the cheapest kit. This is read once per chunk build.

Estimates for the phone tier near a renovated crossing (for the combined test to confirm or refute):

| | Estimate | Basis |
|---|---|---|
| kit triangles in view | about +70–110 k | between option B (9 mm, props LOD0: +111–137 k) and the reduced tier (+33–43 k), less the lip beyond 12 m |
| kit renderers in the frustum | about the reduced tier's far set + 5–15 near pieces | the rings; base in the trim |
| mesh per 25 chunks | about +12–15 MB | desktop +18.5–21.2 MB, about half of it paper and fray |
| chunk build | about +3–5 ms (Editor M3 Max scale) | reduced +1.3–2.7, desktop +4.1–6.2 |
| textures | about 44 MB (+28 MB) | §2 |

---

## 3. Desktop and WebGL unchanged (code reading)

Every new branch reads the phone flag (`phone` in `ChunkBuild`, `PhoneT` in `RenoKit`), and that flag is `!Reduced && PhoneTier`:
- `PhoneTier` is false on Mac and Windows players, because `FrontRoomsHandheld.Active` compiles to false there and `DebugForcePhoneTier` is only set by tools. It is false on WebGL players at compile time. It is false in the Editor outside the handheld preview, which `Active` already gates: batch mode, autopilot and capture runs never take it.
- `Reduced` in players: `ReducedTier || (FrontRoomsHandheld.Active && !PhoneTier)`. On Mac and Windows that is `ReducedTier` exactly as before. On WebGL `ReducedTier` is `true` at compile time, as before. The Editor branch is unchanged.
- With the flag false: `S()` returns the original expression, `Fray` runs the original loop, `BaseMesh` takes the original branch, and `Finish` builds the same renderers with the same shadows and spawns the same props. The props loop is restructured but performs the same calls and the same `StatProps` count. No ring component is added.
- Textures: only the iOS tab of the 30 renovation `.meta` files changed (1024 → 2048). The Default, Standalone, WebGL and Android tabs are byte-identical. Unity reimports the 30 maps once on the desktop, with the same settings and the same result.
- Tools (`FrontRoomsTransitionAssets`): the new mode `phone` sets `DebugForcePhoneTier`, and every existing mode clears it. In Stats, a renderer is counted at its group's first LOD, which is LOD0 on the desktop, as before.

---

## 4. The combined test (after everything lands)

1. **Device frame time.** Use iPhone (16 Pro or the oldest supported) and iPad 11", preset HIGH, 60 fps, a development build with the Profiler attached. Use seeds 2554 and 20388, and walk through 10 borders including at least 2 renovated crossings and one renovated arch. Record the median / p95 / max frame time, standing 2 m from a renovated crossing, looking down a corridor with borders 20–45 m away, and walking. Repeat the same route on LOW (the reduced tier; LOW also lowers shadows and AA, so it is a bound, not an A/B). Repeat on ProMotion at 120 if the device has it.
2. **Draws.** Read batches / SetPass calls / shadow casters (Unity Profiler Rendering, or an Xcode GPU capture) at the same three views. In the Editor, run `FrontRoomsTransitionAssets.Stats -modes now,frame,pick,reduced,phone` (shots 1–4, 8–10) and `SeedStats` (20 seeds). Report "kit in frustum" for phone vs reduced (target ≤ 40) and vs pick.
3. **Memory.** Use the Unity Memory Profiler or Xcode on device. Read the 30 renovation textures' size and format on iOS (expect about 44 MB; confirm ASTC 6×6). Read the mesh memory per 25 chunks (Stats `chunk mesh MB`, phone mode) and the app's resident footprint against the device limit.
4. **Chunk build hitch on device.** Read the map's `WorkInFrame` (chunk build ms) for renovated chunks: phone vs LOW.
5. **Look.** Shoot the VL367 fixed shots on the phone tier next to the desktop: same layout, props at LOD1, lip / strips / scraps present up close. Walk toward a renovated crossing from 35 m: note the pop at 24 m (strips, tiles, bars) and at 12 m (lip, scraps, tape) and decide if it reads. Check the Office base on every face (no gap where the base went into the trim) and the saddles at doors within 24 m.
6. **Editor suites.** Run `FrontRoomsTransitionKitTests`, which now has the iOS texture check and six phone-tier checks: plan identical to the desktop, fewer kit triangles, LOD1 props, base in the trim, rings and no-shadow pieces, nothing past 28 m showing, plus the G7 sweep on the phone tier. Run MIT's transition checks too.
7. **Desktop unchanged.** On a Mac player at the default ULTRA, the desktop G8 signature and the Stats `pick` rows must equal the pre-package numbers (or the last run before landing), and the 30 textures' Standalone import must be identical.
8. **WebGL.** No change expected (reduced at compile time). Check it whenever the WebGL line next captures.
