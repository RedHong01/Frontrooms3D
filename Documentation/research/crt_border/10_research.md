# CRT picture vs bezel: how much black should show (1985–1993)

Wallpaper / motion line (动画和动态图形模型技术), 2026-10-22. Status: **research done; numbers handed to 游戏视觉 for the tube layer.** Verification image: VL429 (`images/vl429_crt_border_mock.png`). Every source and its verification state: `SOURCES.md`.

**Red, 2026-10-22 ~20:20 (account-B chat of this line), after seeing VL392:** "目前大部分的CRT TV的显示区域应该不会有边框里面的黑边才对，或者说就算有也不会那么厚，我需要你调研这部分然后优化这个黑边应该是多大，在那个时期的CRT电视".

## 1. Answer

**Decision (Red, 2026-10-22 21:23, in this line's chat, pointing at VL429's "1990 OFFICE MONITOR" tile): "选择这个".** The desk CRT (`Kit_CRTMonitor`) shows the **whole 4:3 picture, about 276 × 207 mm, with about 12 mm of unlit glass at the sides and 10 mm at top and bottom**, whatever it plays (the ad included): no pillarbox, no crop. The TV row below (overscan, 0 mm) still stands for `Kit_CRTTV`, which Red did not pick for here.

| What the screen shows | On which set | Visible black inside the bezel | Picture size for our openings |
|---|---|---|---|
| Broadcast, VHS, the station TV, a CCTV camera (any video signal) | consumer TV, video or CCTV monitor | **0 mm.** The picture runs under the bezel on all four sides (overscan). | Scale the full 4:3 picture so **about 5 % is hidden at each edge** (plausible 3.5–7.5 %). Desk monitor opening 300 × 228 mm → picture ≈ 333 × 253 mm. TV opening 445 × 336 mm → ≈ 494 × 373 mm. **Never pillarbox a 4:3 picture on a 4:3 tube.** |
| A computer screen (terminal, office software) | office computer monitor (12–15 in class) | **About 12 mm left/right and 10 mm top/bottom** (plausible 8–15 mm), unlit glass at factory settings. Sides a little wider than top/bottom (4:3 image on a slightly squarer opening). | Lit image ≈ 92 % of the opening: **276 × 208 mm** on the desk monitor. |

Today the game shows about **20 mm on every edge** (§2), twice the typical computer-monitor border and wrong for video content, which period sets showed edge to edge.

## 2. What the game shows today (measured)

The desk CRT is `Kit_CRTMonitor` (`Tools/Blender/frontrooms_kit/assets/crt_monitor.py`, a beige 15-in office monitor):
- the bezel opening is 300 × 228 mm; the curved glass is 298 × 226 mm and sits behind the bezel lip;
- main draws the picture on a flat quad of 280 × 190 mm (`FrontRoomsScreenVideo.screenSize`, 4 mm in front of the screen anchor);
- Ad_03 is 640 × 480 (4:3) and plays into a 640 × 434 RenderTexture with `VideoAspectRatio.FitInside`, so it is pillarboxed inside the quad.

Measured on 游戏视觉's VL392 control witness (face-on, 1:1, no post; 2.46 px/mm): **about 20 mm of black at left and right** (7 mm of bare glass, then 13 mm of pillarbox inside the quad) **and about 21 mm at top and bottom**. That is 7 % of the opening width and 9 % of its height on every edge.

The terminal (computer UI, `research/computer_ui/20_design.md` §8.2) has its own quad, 0.253 × 0.19 m: about 23.5 mm of black at the sides and 19 mm at top and bottom.

游戏视觉's tube layer (run crt-tube-vl245, clone `W/proj_crttube`, package `crt_tube.sh`, not in main yet) replaces the flat quad with a curved picture on the kit's own glass, inset 3 mm, with the raster overscanned 4.5 % per edge. That brings the band to about 4 mm on the monitor and 0 mm on the TV (`Kit_CRTTV`, opening 445 × 336 mm).

## 3. Televisions: overscan, no black border

- **Why sets overscanned.** CRT receivers were adjusted so the picture overfilled the mask, hiding the blanking edges and keeping the screen full "even when the receiver's components had aged or the line voltage sagged" (SMPTE EG 2046-3:2010 §4, the history of the safe areas).
- **What broadcasters assumed.** SMPTE RP 27.3-1989 (in force for our whole period; the same sizes since 1972): safe action 90 % of width and height, safe title about 80 % (79.5 % of width), with rounded corners, "to ensure visibility of the information on the majority of home television receivers". So up to 5 % per edge could be lost on an ordinary set, and up to 10 % on a badly adjusted one.
- **How much real sets hid.** Typical consumer sets 8–15 % of each dimension in total, about 4–7.5 % per edge (American Cinematographer, Nov 1980: 9–15 % horizontal; Inglis, *Video Engineering*, 1993: 5–10 %; IBM patent US 5,119,082, 1989: "somewhat more than 10 %", "not more than 15 %"; Sony CKV-27EXR / 20EXR 1993: 15 %, a 28-in tube showing 27 in). Better 1988–93 sets were tighter (Popular Mechanics Feb 1985: best sets 5–6 %; APEL lab tests in Video Review 1990 and Popular Electronics 1992–93: 0–4 % total on enthusiast-grade sets). Cheap sets hid up to about 18 %.
- **In every case the visible black inside the bezel is 0 mm.** Showing the raster edge needed an underscan switch, which only professional monitors had (§5).

## 4. Office computer monitors: a thin unlit border at factory settings

- Computer monitors of the period were **underscanned**: the image was smaller than the visible glass, so an unlit band showed inside every bezel edge. MacUser (1992) wrote about "the black border you usually see on an Apple 13-inch monitor", and *MacUser Guide to Shareware* (1993) about "the wasted, thick black border that usually hems in the desktop".
- **Factory image sizes** (service alignment specs, verified):

| Monitor (year) | Tube | Visible area (approx.) | Factory image | Border per edge |
|---|---|---|---|---|
| IBM PS/2 8512 (1987) | 14 in | ≈ 264 × 198 mm | 240 × 180 mm (service manual) | ≈ 12 mm L/R, 9 mm T/B |
| IBM PS/2 8513 (1987) | 12 in | ≈ 224 × 168 mm (11 in visible) | 207 × 155 mm (service manual) | ≈ 8 mm L/R, 6 mm T/B |
| Apple 13-in RGB (1987) | 13 in | 12.8 in visible ≈ 260 × 195 mm | 640 × 480 at 69 dpi ≈ 236 × 177 mm | ≈ 12.3 mm L/R, 9.2 mm T/B |
| NEC MultiSync Plus (1989 review) | 15 in | — | 11.75 in diagonal with both size controls at maximum | ≈ 19 mm ("approximately 3/4 inch", PC Magazine) |

The border sizes are derived from the stated image size against the visible area. Fixed-frequency monitors (IBM 8512/8513, Apple 13-in) had no user size control, so this is the border the office worker saw. Multisync monitors had size knobs. By 1991–92 NEC advertised its FullScan monitors as delivering images "edge-to-edge with no black border" (PC Magazine, 29 Sep 1992), which tells us a border was the norm until then.

- **For our 300 × 228 mm opening** (a 16-in-class visible area), the period ratio (image ≈ 91–93 % of the visible width and height) gives **about 12 mm at the sides and 10 mm at top and bottom**, a lit image of about 276 × 208 mm. The plausible range is 8–15 mm; 0–3 mm for a user-adjusted multisync or a 1991+ FullScan monitor. The current 20 mm matches only the largest border found, the NEC MultiSync Plus at maximum size.

## 5. CCTV, security and video monitors: overscan by default

- Professional and CCTV monitors overscanned composite video by default. Sony PVM specs give normal scan as 5–7 % overscan (PVM-1271Q 1984: 5 %; PVM-8220 1985: 6 %; PVM-1340/1341/1342Q 1988 and PVM-1942Q 1989: 7 %). Plain CCTV monitors had no underscan control at all (Sony SSM-121 1990, SSM-920 1991, Ikegami VCM-901), and the Hitachi VM-906/910/129 service manual sets height and width so the picture "completely fills CRT mask".
- Underscan, with a visible black border, needed a non-default UNDER SCAN button on a pro monitor, and even then the border was thin: Sony's 3 % underscan with a factory target of 5 ± 3 mm vertical and 6 ± 3 mm horizontal on a 13-in tube.
- So a guard's desk monitor showing a camera or the station's TV picture filled its mask edge to edge.

## 6. What this means for the game (handed to 游戏视觉)

1. **Video content (Ad_03, the station TV, any broadcast or CCTV picture), on both `Kit_CRTMonitor` and `Kit_CRTTV`:** 0 mm visible black.
   - Overscan **5 % per edge** (游戏视觉's 4.5 % is inside the period range; keep it).
   - The picture surface reaches the bezel lip: no visible inset strip on the monitor (today's tube layer leaves ≈ 4 mm: the glass is 1 mm short of the opening and the face is inset 3 mm; tuck it under the lip instead).
   - Fill the full 4:3 frame; never pillarbox 4:3 content on these 4:3 tubes (`VideoAspectRatio.FitInside` into the 640 × 434 RT is what adds 13 mm per side today).
2. **Computer content (the terminal of `computer_ui/20_design.md` §8.2):** active area **0.276 × 0.208 m** on the 300 × 228 mm opening (12 mm / 10 mm of unlit glass), replacing 0.253 × 0.19 m. The underscan border keeps the phosphor's dark glass colour, never pure black.
3. **Ads must respect SMPTE safe areas.** With real overscan, content outside safe action (90 %) is cut by the bezel. Ad_03's "499" runs to x = 605 of 640 (safe action ends at 608, safe title at 574), so its last digit touches the bezel lip in VL429's TV tile. Owner: 平面视觉 · 广告 (told 2026-10-22 by this line's B session).

## 7. Gaps

- The SMPTE receiver-overscan surveys with measured averages (Zavada 1971, about 6,000 sets; Richards & DiGiulio, SMPTE Journal Feb 1994) are paywalled and were not read.
- No 1985–93 Sony, Panasonic or Zenith consumer service manual with a numeric overscan target was found; in-era targets exist only for a 1986 Portland set and the c.1993–94 RCA/GE CTC175–177 chassis.
- Most single-number overscan figures do not say whether they are per edge or per axis; this doc reads them as totals per axis (the conservative reading).
- No factory image size was found for the Zenith ZCM-1490, Compaq VGA, NEC MultiSync 2A/3D/3FG, Mitsubishi Diamond Scan or Princeton Ultrasync; their size controls are documented, their default picture size is not.
- No document was found for the Panasonic WV-5340/WV-5410 CCTV monitors.

## 8. How it was researched

Three research agents (TV overscan; office monitor image area; CCTV/pro video monitors) in workflow `wf_dd0b562c-660` (2026-10-22, account A), each continuing the sources found by this line's two account-B agents that died at the session limit. Every sourced number was then re-checked against its source by an independent agent: the first 10 per topic in the same run, the remaining 47 in a second run (`wf_1991196d-83c`). Of 81 findings, 67 were confirmed, 10 could not be confirmed (mostly Google Books and borrow-only books that would not load) and 4 are forum posts; this doc cites only confirmed ones. `SOURCES.md` lists all 81 with their state. Pages were read online only; no media was downloaded. Raw results: `~/FrontRoomsVisualWork/crt_border/research/`.
