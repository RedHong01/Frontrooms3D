# R1 · Internal iconography inventory (FrontRooms), 2026-10-30

Read-only audit for 平面视觉 (Red's ask: audit the iconography system). Every icon / pictogram / glyph / symbol the game and its specs use today, with construction specs.

- `P` = `/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D`. Doc paths are relative to `P`; `:n` = line number in that file (read 2026-10-30).
- `W` = `~/FrontRoomsVisualWork`. Nothing in `P` was edited; Unity was not run; nothing was downloaded.
- **INFERENCE** marks my reading, not a doc statement. "Decided" = Red's pick recorded in the doc cited.
- Record format per icon: **where · meaning · construction · colour · states/motion · type neighbour + alignment · source · issues.**

---

## 1. HUD key family (crosshair cluster + key panel), desktop + phone

Owner: 游戏视觉 builds (`Assets/Scripts/Rendering/UI/FrontRoomsKeyHud.cs`, in main since 10-29: KeyHud cf2cb7fc, Motion 7555d735, HudStyle f1f7e64e, `hud_build/DECISIONS.md:62`); 平面视觉 owns type/graphics. Decisions log = `Documentation/research/ui_key_icon/hud_build/DECISIONS.md` (newest entry wins, `:3`). Lab source of every curve: `W/keyicon_motion/lab_template_v2.html`.

**Shared HUD tokens (this family)**

| Token | Value | Source |
|---|---|---|
| paper | `#F4F1E8` (key, padlock rim/shackle/insert, eye lids, broken-door frame/leaf, all HUD text) | `research/ui_key_icon/03_figma.md` §2.4; `FrontRoomsHudStyle.TextPaper` (`Assets/Scripts/Rendering/UI/FrontRoomsHudStyle.cs:49`) |
| dot white | `#FFFFFF` (crosshair dot, eye pupil) | `04_for_red.md:41`; `FrontRoomsKeyHud.cs:175` |
| keyline ink | `#141414`, design 85 % (sRGB blend); Unity linear-light alpha **0.95** (calibrated 10-22 on lit L0 / dark cell / Office / evac plan) | DECISIONS `:12`, `:43`, `:52`; `FrontRoomsHudStyle.cs:7-19` |
| tag-number ink | `#1E1D1A` | `lock_motion/README.md:14`; `FrontRoomsKeyHud.cs:175` |
| accent yellow | `#F4DF3B` (key-panel rule only, in this family) | `03_figma.md` §2.4; `FrontRoomsKeyHud.cs:176` |
| zone tint | red `#8C0F0D` / blue `#0F2973` / white `#FFFFFF` (the `Prop_Plastic*` base colours) | `README.md:14`; `FrontRoomsKeyHud.cs:177` |
| muted (retired here) | `#BDBAB0`, was prompt line 2 + panel meta; replaced by paper 10-29 ("quieter only through face and size, never a dimmer ink", the T5 rule) | DECISIONS `:60` (#39); `FrontRoomsHudStyle.cs:45-48` |
| card (retired) | `#141414` at 90 %, 12 × 8 px padding: proposed 10-07, rejected by Red 10-14 ("我不希望有背后的这个黑色底") | `04_for_red.md:47`; `README.md:38` |

Units: 1080p px at 1x around the crosshair point (960, 540); **phone = × 0.75 (`ls`) for crosshair geometry** (16 px dot → 12 pt), **× 0.85 for the key panel** (`10_design.md:139`, `:391-398`).

### 1.1 Crosshair dot

- **Where:** HUD desktop + phone, screen centre. **Meaning:** aim point / idle (no target). One meaning, but it is also the *seed* of every crosshair state (lock, broken door, eye are "built from the dot"; Red 10-14: "利用好鼠标中心的原点，而不是直接覆盖一个动效在上面", `README.md:73`).
- **Construction:** filled circle Ø 16 px desktop / Ø 12 pt phone; keyline = exact ring Ø 18 at 85 % (not a Unity `Outline`, which gives a 1.0–1.41 px ring). Drawn by KeyHud's SDF quad (`FrontRoomsHudVectorGraphic`), map's `crosshairImage` hidden while KeyHud is attached. Legacy sprite: `UI/HUD_Crosshair.png` 64 × 64, dot 32, shown in a 32 × 32 rect (`03_figma.md` §2.1 "Crosshair"); proposed `HUD_Crosshair_Touch.png` 144 × 144, dot 72 (`04_for_red.md:94`).
- **Colour:** `#FFFFFF` + `#141414` keyline.
- **States/motion:** alpha 1 idle; 0 while a lock / door / eye shows; fades back in during F·1 (`clamp((f−0.25)/0.25)`) (`10_design.md:379-385`). G-rule 10-29: **min dwell ≥ 200 ms** between dot opening and closing so the HUD never flashes > 3 Hz (photosafety) (DECISIONS `:60` #39 (2)).
- **Source:** `hud_build/10_design.md` §2.4 (`:207-217`), §4.6.
- **Issues:** (a) **dot white `#FFFFFF` vs paper `#F4F1E8`**: a 4 % step at the dot → rim hand-off, ported as is (`10_design.md:728`, O-14). The dot is the only white HUD mark; everything else is paper. (b) The legacy map dot (`HUD_Crosshair.png`) still exists for when the shader fails (`OwnsDot = false`, `10_design.md` §3.2).

### 1.2 Key glyph "B" (key + split ring + tag) — the collected-key icon

- **Where:** key panel, HUD bottom-left on desktop (glyph top-left (90, 914), 72 × 48), top-left on phone (slot (93.3, 72) pt at × 0.85) (`README.md:60`, `10_design.md:391-398`); also flies across the screen in P4 / KP-1b. **Meaning:** "you hold the stair key for this floor". Decided Direction B, phone + desktop (Red 10-14, DECISIONS `:9` #1). A (cut key), A + chip, C (sign plate) are history.
- **Construction:** traced from the 3D kit at 0.69 px/mm: `Kit_Key_Zone` (58 × 26 mm; rounded-square bow 30 × 26 mm R 8 with Ø 4.8 mm hole; 3.9 mm shoulder step; 25 × 8.6 mm blade with 6 cuts; tip bevel) + `Kit_KeyRing` (Ø 25 mm) + `Kit_KeyTag_*` = the pickup shot's last frame (`02_design.md:60-64`, `:131-138`). Two deliberate exaggerations (inherited from A): cuts × 2.5 deep, hole × 1.5 (`02_design.md:80-82`). Box **72 × 48 @1x** (144 × 96 @2x, 216 × 144 @3x, 432 × 288 @6x for the P4 take-off); view = side, tip right, cuts up (= how the key enters the lock) (`02_design.md:78`). Ring stroke **1.2696 px**; tag = tag rim (1 px paper) + body (zone tint) + tab rim + tab + paper insert + typed number (`10_design.md:528`). Masters are M/L/Z polylines only (arc = 48-segment polyline). Hinting: structural edges on even 2x values (`02_design.md:84`). Split into two layers for U·3: **key part** (pivot glyph anchor (36, 24)) and **ring+tag part** (swing pivot = key bow (35, 10) = Unity (0.4861, 0.7917)) (`10_design.md:262-279`). Shapes: rect / round / long tags × red / blue / white = 9 `B_<shape>_<colour>_held.svg` masters + 36 runtime sprites `UI/KeyHud/KeyB_<shape>_<colour>@{n}x.png` (`10_design.md:528-530`).
- **Colour:** key + ring paper `#F4F1E8`; tag body zone tint; insert paper; number `#1E1D1A`. Keyline: **1 px `#141414` 85 % OUTSIDE the silhouette** (O-1, Red-confirmed 10-22, DECISIONS `:43` #22).
- **States/motion:** *Rest* in panel; **P4 pickup flight** (decided 10-14): starts at the hand position viewport (0.65, 0.30) = (1248, 756) at 1080p at 3× scale, takes off at `KeyPickup.HoldUntil`, reaches the slot at 1× in 720 ms ease-in-out on a dipping curve; ring+tag keep the held swing (25°, 2.2 Hz, decay 2.5/s) + a landing swing 1.7 Hz, damping 0.12, peak 18°; rule grows 160 ms at +680, label 220 ms from −8 px at +740, meta at +840 (`README.md:69`). **KP-1b** (Red 10-29, DECISIONS `:58` #37): on today's instant pickup the glyph alone holds 0.5 s at the hand position, then flies into the panel (desktop bottom-left, phone top-left). **U·3 "Push, fold, click"** unlock gesture (decided 10-14, DECISIONS `:31` #15): push dx 6 px (100 ms), key-only scaleY 1 → 0.15 → 1 (0.10–0.42 s, a side-on turn), tag swingKick −9° / +6°, click at 0.24 s = yellow rule 4 → 6.4 px + key bounce 1 px; synced in mode (d) to the door shot beats KeyInsert 0.55 / KeyTurn 0.68 / BoltRetract 0.86 s (DECISIONS `:27-31` #13–#15). Cancel before BoltRetract → eases back 0.15 s (#26).
- **Type neighbour:** label `STAIR KEY` Bayon 20 (phone 17) paper, baseline panel +22 (desktop y 936); meta `OPENS THE STAIR DOOR` IBM Plex Mono 13 (phone 11), tracking 0.2, baseline +41 (y 955), paper since #39; label x = glyph x + 72 + 14 (`10_design.md:391-398`; `03_figma.md` §2.1 "Full form"). The Courier Prime Bold 25 number next to the label is **gone** (floor token now on the tag only; `FrontRoomsKeyHud.cs:495`). **Alignment:** 48 px glyph ≡ 48 px rule height; label caps 7.7–22 px and meta baseline 41 sit inside the glyph's height; panel bottom 962 = compact-row bottom (`03_figma.md` §2.1). INFERENCE: label is not cap-aligned to any glyph feature; it is a two-line block centred-ish on the 48 px module.
- **Source:** `02_design.md` §1–2; `03_figma.md` §2.3 (`B_*` 72 × 48); `lock_motion/README.md` §2; `hud_build/10_design.md` §2.5, §5, §7.1; Figma HUD KEY section `2532:4038`, review page artifact `ArLGmLysHRtswHQjWKSthC`.
- **Issues:** (a) **Shape + colour now encode nothing the player uses.** Floor keys replaced zone keys (DECISIONS `:49` #28), the number = floor token, but tag shape/tint are still a **hash of the zone** in a stub (`FrontRoomsKeyHud.cs:233-251`, "until the map's rule lands (D1.6)"). The 10-07 spec warned "only the rect tag reads easily at 1080p" (digit height rect 10.2 / long 7.9 / round 7.7 px, `02_design.md:136`). (b) B was *not recommended* on 10-07 because it "adds the only saturated colours to a quiet HUD" (`02_design.md:138`); Red chose it anyway → the HUD's only saturated colours are the tint and the yellow rule. (c) The ring stroke 1.2696 px and the 1 px tag rim are the thinnest marks in the HUD (padlock shackle 2.6, door frame 2, eye lids 2) — INFERENCE: below the type stems of Bayon 20 (~2 px per G6, DECISIONS `:59`). (d) The 3D key is still a yellow glowing cube (`SpawnKey`), so the "echo of the model" is not yet visible in play (`04_for_red.md:114`; DECISIONS `:58`, D1.6 pending). (e) `held_pose/measure.json` absent → P4 start factor is a parameter (3× / 1.117 at 402 pt) (#24).

### 1.3 Floor token on the tag (`00`, `B1` … `B99`)

- **Where:** inside the B tag insert (panel) and inside the padlock insert (crosshair). **Meaning:** the floor the key opens out of: `00` = LEVEL 0, `B1`, `B2` … going down (DECISIONS `:53-54` #32–#33; `FIX_CARD_VL217.md:105-108`).
- **Construction:** Courier Prime **Regular** (not Bold; O-2 corrected, DECISIONS `:44` #23), sized so the ink height = the tag's digit height (rect 10.212 / round 7.659 / long 7.936 px; long sx 1.035), ink box centred on the insert centre; runtime = number atlas `UI/KeyHud/KeyNum_<shape>@{n}x.png`, 10 × 20 cells (`10_design.md:530`). From `B10`: the three characters at **0.8×** on the same baseline, centred in the same tag, never stretched (#33).
- **Colour:** `#1E1D1A` on paper insert. **Motion:** in the padlock the digits appear per glyph at u 0.8 / 0.9 of the iris build (400 / 450 ms) (`README.md:113`).
- **Type rule behind it:** "Bayon draws 1 like I and 0 like O" → **no digits in Bayon** (`02_design.md:174`; `FIX_CARD_VL217.md:51`).
- **Issues:** `00` for LEVEL 0 vs HUD room line `LEVEL 0 / FRONT ROOMS` (Source Serif 4) — two notations for the same floor (INFERENCE: `00` ≠ `0`; a player may not map `00` to LEVEL 0). The 10-07 warning about two numbers for one place (`ZONE 06` visit counter vs `KEY 14`) is the same class of risk (`02_design.md:187`).

### 1.4 Key-panel yellow rule

- **Where:** key panel, left of the glyph. **Meaning:** INFERENCE: "this is your inventory slot / it is active" — no doc states a meaning; it comes from Figma HUD / Key `2256:161` (`02_design.md:287`).
- **Construction:** filled bar **4 × 48 px** at x 72 (phone 3.4 × 40.8), 14 px gap to the glyph; grows from its top (P4, 160 ms); U·3 click widens it 4 → 6.4 px from its left edge (`10_design.md:273`, `:391-394`).
- **Colour:** `#F4DF3B`, no keyline.
- **Issues:** **same yellow, several meanings**: stamina bar + hold bar in the map's HUD code (`02_design.md:287`), menu focus/selection marker (4 × 14 px, `FIX_CARD_VL217.md:29`, "the same mark as the HUD key panel's yellow rule"), settings selected row (`FIX_CARD_VL217.md:32`), click feedback in U·3. House rule "never yellow" for the key itself (`02_design.md:283`) vs a yellow rule beside it.

### 1.5 Padlock at the crosshair (L3·D) — "locked door"

- **Where:** HUD crosshair, desktop + phone (× 0.75). **Meaning:** the aimed door is locked; it wears the key's tag, so the door's lock and the right key match at a glance (Red 10-14: "a red round tag means a red round padlock", `README.md:5`). Same symbol for *locked, no key* and *locked, you have the key* (the prompt and E reaction differ).
- **Construction:** box 80 × 48, **pivot = insert centre (40, 32)** on the crosshair point, so the aim point never moves. Body = the tag body cloned from the B masters: rect rim 39.33 × 20.01 r 3.45; long rim 52.44 × 15.18 r 3.45; round rim Ø 26.22; body = rim − 2(r − 1); insert rect 27.6 × 13.11 r 1.035 / long 34.5 × 9.66 / round Ø 17.94. Shackle: "steel, drawn in paper", stroke **2.6**, butt ends, round joins, half-width a = 8.5 rect / 6.5 round / 10.5 long, 4 px straight leg above the rim, long (left) leg 6 px into the rim, short leg 3 px, drawn behind the body, 48-segment arc. Keyline: one 85 % group = ink copy of shackle (stroke 4.6) + rim + 2(r + 1), behind everything. Layer order back→front: keyline, shackle, rim (paper), body (tint), insert (paper), number (ink) (`README.md:12-30`, `:99-107`; `10_design.md:141-160`). Masters: 18 SVGs `lock_motion/svg/Lock_<rect|round|long>_<red|blue|white>_<closed|open>.svg`; Figma `3113:6999` (18 variants), boards KV11 `3113:7000`, KV12 `3113:7147` (`README.md:22-30`). Runtime: SDF quad, ≤ 16 primitives, analytic AA, union keyline (`10_design.md:262-296`).
- **States/motion (all decided 10-14):** **IN = L3·D "the dot opens like an iris"** (500 ms): dot swells into the whole tag in paper (150 ms), tint closes in from the edge to the insert (175 ms), shackle rises with ease-out-back (190 ms), digits at 400 / 450 ms; prompt alpha = clamp(u/0.6) (`README.md:94`, `:101-110`). **No key = D·3 "tug, then shake"**: one tug (shackle up 2.5 px into its stop, clacks 0.6 px past rest; body dips 0.8 px), then ±3.5 px shake at 8 Hz from 140 ms for 360 ms, padlock and both text lines shake together; 0.50 s window, unscaled time (DECISIONS `:13` #5, `:37` #19). Iris blinks rejected ("没有明确的communicate 打不开这个概念", `README.md:120`). **Leave (no key) = famOut**: build reversed, 320 ms (#6). **Has key:** shackle lifts 5 px (150 ms), swings 180° round the long leg (cos-projected, constant 2.6 px width), then **F·1 Drop**: 60 ms beat, falls ½·560·f² px, tips 10° about the insert centre, fades from 150 ms over 350 ms; dot returns from 250 ms; 0.55 s (DECISIONS `:16` #10). Under mode (d)/(b) the padlock is hidden during the door shot and returns OPEN at 1.45 s, F·1 at +0.10 s (#25). Sound cues `LockTug`, `UnlockDrop` raised at motion start (#34).
- **Type neighbour:** line 1 Bayon 20 (phone 17), paper, centred, baseline **591** (51 below the dot centre; phone 239); line 2 IBM Plex Mono 13 (phone 11), tracking 0.2, baseline **614** (phone 259) (`10_design.md:209-213`). Strings: `LOCKED` / `NEEDS STAIR KEY`; `E  ·  UNLOCK` / `STAIR KEY`; phone `UNLOCK` (DECISIONS `:50` #29). Keyline: line 1 **2 px** desktop (G6, #38), 1 pt phone; line 2 1 px.
- **Issues:** (a) O-3: at u = 0 the shackle cap pokes 0.5–1.9 px (paper) / 2.9 px (keyline) outside the 16 px dot for 1–2 frames (`10_design.md:717`). (b) One symbol, two situations (have key / no key) distinguished only by text and E reaction — INFERENCE: acceptable (a lock is a lock), but there is no static visual difference before E. (c) The padlock's colour/shape inherits the zone-hash issue of 1.2(a). (d) The **placard padlock** (§5) is a different drawing (see §9.2).

### 1.6 Broken door at the crosshair (B·4 "Hangs off its hinge")

- **Where:** HUD crosshair, desktop + phone. **Meaning:** this door is broken (by the Relay) and can't be shut, permanent for the run (Red: "门一直坏着"; rule in `Documentation/RELAY_PURSUIT_REDESIGN.md` §10) (`README.md:151-160`, `:154`). Decided 10-14 (DECISIONS `:17` #11). Red asked for a real door silhouette: "很多图标没有门的廓形" (`README.md:161`).
- **Construction (1080p px, SVG y-down; × 0.75 phone):** frame = inverted U `M-7 13 L-7 -12 L7 -12 L7 13` (14 × 25, path length 64), paper stroke **2**, butt ends, round joins, keyline 4 at 85 %, drawn first; leaf = rounded rect 11 × 23.5 rx 0.8 centred at y +1.25, keyline +1 px; knob = Ø 2.2 **ink `#141414` at 100 %** at (3.4, 2.2); hinge pivot (−5.5, −10.5) (DECISIONS `:17`; `10_design.md:182-191`). Paint order: frame keyline, frame, leaf keyline, leaf, knob (separate opacities, not a union).
- **States/motion:** IN 800 ms: dot → leaf (0–240 ms, ease-out), frame draws by partial length (160–440 ms), knob pops (384–464 ms), leaf drops 1.5 px and swings to 12° on a 2.4 Hz spring (damping 0.3). E ("won't shut", 550 ms, no retrigger): θ 12 → 3° in 90 ms then springs back (2.6 Hz). LEAVE 300 ms back into the dot (DECISIONS `:17`). Sound cues `BrokenBuild`, `BrokenKnock` (#34).
- **Type neighbour:** line 1 `BROKEN` (Bayon 20/17), line 2 `WON'T SHUT` only during the 0.55 s reaction (`FrontRoomsKeyHud.cs:1163`); phone USE verb `BROKEN`.
- **Issues:** (a) **Knob is the only ink-filled (`#141414` 100 %) part** in any crosshair state (all else paper on keyline) — a figure/ground inversion. (b) O-8: two tiny angle steps (11.6° → 12° at IN→HOLD; end of E window) ported as is. (c) Door outline here (frame 14 × 25 + leaf) vs the **placard's door marks** and the **TOUCH broken verb** (text only) — no shared door pictogram exists elsewhere (see §9.2). (d) B·3 "ring with a gap" was dropped partly because "at 16 px it can read as a loading spinner" (`README.md:173`) — a recorded legibility lesson.

### 1.7 Look-eye at the crosshair (E·1 "Opens from the dot")

- **Where:** HUD crosshair. **Meaning:** "there is something here to READ / EXAMINE" (plan, sign, READ terminal, the first key during its EXAMINE hold). Must read as "you can look", **not** "it sees you": never stares, pulses or turns red (`README.md:211`). Decided Red 10-15, kept 10-22 (DECISIONS `:35` #17, `:51` #30).
- **Construction (1080p px, × 0.75 phone):** lids = two cubic arcs (−w,0)→(w,0), controls (∓w·c, 4a/3), paper `#F4F1E8` **stroke 2, round caps**; pupil = the dot itself, `#FFFFFF`, ellipse r `pr`, clipped between the lids; keyline `#141414` one 85 % group (lids stroke 4, pupil r + 1). Poses: DOTEYE (w 8, up −8, lo 8, pr 8, c 1 = the dot), REST (w 20, up −8, lo 7, pr 5, c 0.45 → 40 × 15 px almond, upper lid slightly lowered), FOCUS (up −9.5, lo 8, pr 4) (`README.md:219-226`).
- **States/motion:** IN ≤ 1.05 s: DOTEYE → REST 0.30 s (readable as an eye ≤ 0.25 s), prompt in ≤ 0.30 s, **one** slow blink from 0.42 s (close 0.22 s, hold 0.07, open 0.34); re-blink only after the aim left **and** ≥ 1.0 s; E (only on `InspectOpened`) = 80 ms focus; leave 0.30 s (0.12 s when another state waits). **Reduce Flashing:** no blink. **Reduce Motion:** ≤ 0.15 s alpha cross-fade (`README.md:228-241`; grammar §2–§5).
- **Type neighbour:** line 1 `E  ·  READ PLAN` / `READ SCREEN` / `READ SIGN` / `EXAMINE KEY`; phone drops the E, USE verb READ / EXAMINE (split by InspectKind, O-28) (DECISIONS `:50`, `:60`).
- **Issues:** (a) Skyrim-style risk of reading as "being seen"; playtest item + switch rule to E·3 (lowered gaze) if ≥ 2 testers or ≥ 25 % code BEING SEEN (`FIX_CARD_VL217.md:110-117`). (b) Pupil is dot-white while lids are paper (same O-14 step). (c) The eye is a HUD-only symbol; the placard and signage never use an eye (consistent, since it means "look at this" in-world).

### 1.8 Prompt separator "·" and the `E` key letter

- **Where:** prompt line 1 (`E  ·  UNLOCK`, `E  ·  TAKE KEY` …), desktop. **Meaning:** separates the key letter from the verb. The **key letter `E` is plain Bayon text — there is no keycap glyph in the HUD** (the house rule "never a keycap box", `02_design.md:283`).
- **Construction:** Bayon has no `·`; Bayon's importer falls back to **IBM Plex Mono Regular** (`Bayon-Regular.ttf.meta` c68a3a3b); the dot sits at 0.39–0.43 of the caps (1–1.5 px low at 20 px), accepted (`FIX_CARD_VL217.md:50`, `:144-145`). Two spaces either side. KeyHud draws it as a Plex run between two Bayon runs (`FrontRoomsKeyHud.cs:1417`; desktop 2 px keyline around it forms a "small dark cluster", kept, O-35 (c), DECISIONS `:60`).
- **Issues:** the dot is optically low; a fix needs a renamed OFL Bayon fork (ruled out). On phone the `E` and `·` disappear (verb only on the USE disc), so the desktop and phone prompt grammars differ by design.

### 1.9 Legacy / superseded key-icon assets still on disk

| Asset | Status | Source |
|---|---|---|
| `Assets/Resources/UI/HUD_KeyGlyph.png` (generic ring key, 40 × 22 slot; GUID `eb266a13…`) | map's interim "A panel" still draws it, label `STAIR KEY`; do not swap before KeyHud B's panel replaces the map's (O-13, DECISIONS `:48` #27) | `04_for_red.md:96` |
| A · cut key (`A_S` 40 × 22, `A_L` 49 × 22, outline `_missing` 51 × 24 at 1 px up-left; paper; outline stroke paper 70 %) | history (Red chose B) | `03_figma.md` §2.3 |
| A + chip (77 / 69 / 85 × 22 with 1 px ring wire) / C sign plate (40 × 22, R 2; AIGA/DOT grammar) | history | `02_design.md` §2, `:286` |
| "outline key = not this zone" state, "used = 70 %" state | history (zone keys gone; keys never spent) | `02_design.md` §4 |

---

## 2. Crosshair grammar (what the dot may become) + the meters under it

Owner of rules: 系统设计 (`Documentation/research/computer_ui/30_crosshair_grammar.md`, 10-15, amended 10-22/10-29); curves: 平面视觉 · 信息设计; build: 游戏视觉.

**Rules quoted (they are iconography rules, not just HUD rules):**
- "**The crosshair shows what E will do here, now. Nothing else.**" State and prompt come from **one predicate** (one `Describe` result) so they can never disagree (`30_crosshair_grammar.md:16-18`).
- "**One glyph, one meaning.** The eye means only 'there is something to look at closely'. It is never used for detection, visibility or the Relay anywhere in the game or its HUD. The prompt verb carries the difference between targets … not the glyph." (`:64`)
- "**No translucent ink** on the paper and keyline glyphs … and **no meaning by hue alone. Every state is a shape.**" (`:92`)
- The crosshair never shows anything about the Relay (`:19`); decoration gets no state (`:20`; one exception C35 10-29: the playing station TV shows the eye).
- Photosafety: ≤ 1 blink/s, far below RELAY §9.5's 3 changes/s; Reduce Flashing = no blink; Reduce Motion = alpha cross-fade ≤ 0.15 s (`:85-92`).

**State inventory (`:26-35`) and whether each has a glyph:**

| State | Means | Glyph at the dot | Prompt (desktop line 1 / line 2) | Touch USE verb | Status |
|---|---|---|---|---|---|
| Idle | nothing to do | dot | — | (USE hidden) | live |
| Use | E opens / shuts this door | **none: dot stays** | `E  ·  OPEN DOOR` or `E  ·  SHUT DOOR` (one line) | OPEN / SHUT | live |
| Hold | hold E (glass) | **none: dot + hold bar** | `HOLD E` … (bound key, key map) | BREAK / TAP | live |
| TakeKey | key lying in the world | **none** | `E  ·  TAKE KEY` | TAKE | live |
| Locked | you lack the stair key | padlock L3·D (§1.5) | `LOCKED` / `NEEDS STAIR KEY` | LOCKED | in main 10-29 |
| Unlock | your key opens it | same padlock | `E  ·  UNLOCK` / `STAIR KEY` | UNLOCK | in main |
| Broken | can't be shut, ever | door B·4 (§1.6) | `BROKEN` / `WON'T SHUT` (reaction only) | BROKEN | in main |
| Read / Examine | E opens a close-up | eye E·1 (§1.7) | `E  ·  READ PLAN` etc. | READ / EXAMINE | in main |
| Hidden | HUD not yours (pause, caught, title, takeover shot, close-up) | none | — | — | live |

Priority: Hidden > latched reaction > on a door leaf Locked > Broken > Use > Hold > Read > Idle (`:66-74`; DECISIONS #19). Switching: old state leaves (cut to ≤ 0.12 s if a new one waits) so a new meaning lands in ~0.4 s (`:80-83`).

**Observations:** (a) The glyph set at the dot is **{dot, padlock, door, eye}**; Use/Hold/TakeKey are text-only. INFERENCE: "every state is a shape" (`:92`) holds only for the states that have glyphs; the door Use state and the glass Hold state are distinguished by the prompt verb alone on desktop (on phone, the mini USE / Hold glyph in front of the prompt, §3.9). (b) Desktop prompts name keys as Bayon letters (`E`, or the rebound key: `F  ·  OPEN DOOR`, `HOLD F`, INTERACTION_DESIGN.md "Key map" §, `:171-189`) and pad words (`A · OPEN DOOR`, `CROSS` because "the HUD's Bayon has no ✕", `TOUCH_CONTROLS.md:112`): **no button glyphs anywhere in the HUD.**

### 2.1 Stamina dashes (the only run/stamina representation on desktop)

- **Where:** under the crosshair, desktop + phone. **Meaning:** remaining sprint stamina; shown **only while stamina < full** (`FrontRooms3DGame.cs:4011-4015`; `research/interaction_audit/01_interaction_inventory.md:244`).
- **Construction (code, main 10-29):** 5 filled rectangles; desktop **24 × 6 px on a 30 px pitch**, centred row at y −92 under the dot centre; phone **20 × 5 pt on a 25 pt pitch**, at −58 pt ("own line, centred 58 pt under the dot, always", `TOUCH_CONTROLS.md:133`) (`FrontRooms3DGame.cs:3460-3463`; DECISIONS `:41` #21 "desktop stamina 24 × 6 px, phones 5 px"). Square corners, no keyline.
- **Colour/states:** a segment is filled **`#F4DF3B` (.957,.875,.231)** when stamina ≥ (i + 0.5) s, else **white at 25 % alpha** (`FrontRooms3DGame.cs:4015`). Each segment = 1 s of the 5 s pool; refill after 1 s at 1/s; run dry → **winded** until 1 segment (1 s) is back (`MAP_GENERATION.md:28`; `RELAY_PURSUIT_REDESIGN.md:107`).
- **Winded:** **no distinct desktop visual** (INFERENCE from the code: only filled vs empty colours exist; winded = all/most empty). Phone shows the word `WINDED` in the stick label slot + the stick tints muted (`TOUCH_CONTROLS.md:333`).
- **Issues:** (1) **translucent white 25 %** empty segments break both "no translucent ink" (`30_crosshair_grammar.md:92`; `TOUCH_CONTROLS.md:364`) and the keyline rule (no `#141414` keyline, unlike every other HUD mark). (2) Yellow = the same token as the key-panel rule and the menu focus marker. (3) A playtester "did not understand the stamina bar" (`research/tutorial/01_research.md:40`). (4) No glyph or label says *what* the dashes are (INFERENCE: they rely on appearing when you sprint).

### 2.2 Hold bar (glass)

- **Where:** under the crosshair, desktop (phone hides it during the glass takeover; USE's ring carries the hold, `TOUCH_CONTROLS.md:133`). **Meaning:** progress of "hold E to break glass".
- **Construction:** track **120 × 4 px** (phone 100 × 3) at y −68 (phone −48.5), **white 25 %**; fill = same height, `accent` `#F4DF3B`, grows from the left (`FrontRooms3DGame.cs:3456-3458`, `:4003-4008`). Square ends, no keyline. The world image shows no strain while holding: "The hold bar is the only picture" (`research/interaction_audit/10_audit_report.md:116`).
- **Issues:** same translucent-white track; same yellow; its touch twin is a **ring** (hold ring r 46, §3.5) and the legend Hold glyph is a 60 % arc (§3.8) — one meaning, two shapes (bar vs ring) by platform.

---

## 3. Touch glyphs (phone + iPad), owner 交互设计; type/graphics signed by 平面视觉

Sources: `Documentation/TOUCH_CONTROLS.md` (§1 rules `:22-33`, gaits §2.1 `:52-68`, X2 hints §2.2 `:70-77`, layout §3 `:123-142`, type ramp §4 `:170-193`, kit §5 `:195-202`, motion §10 `:318-386`); `Documentation/INTERACTION_DESIGN.md` X2 (`:117-139`); code `Assets/Scripts/Input/FrontRoomsTouchSprites.cs` (all touch shapes are procedural, AA'd at device density) and `FrontRoomsTouchControlsView.cs`; X2 spec `~/FrontRoomsInteractionWork/x2/X2_PHONE_HINTS_SPEC.md` v3.3 (Figma board `3564:7362`). Units pt (phone, 874 × 402 landscape); iPad controls × 1.4.

**Touch palette (code, `FrontRoomsTouchControlsView.cs:28-34`, `:136`):** Paper `#F4F1E8`, Muted `#BDBAB0`, Accent `#F4DF3B`, Ink `#0A0A0A`, Media `#141414`, Card `#0E0E0D`, Wash (.93,.92,.88) = `#EDEBE0`, Phosphor `#D6DEBD`.

**Design-language rule for touch (quoted):** "Every touch element is built from parts the desktop HUD kit already has (UI MOCKUP 2256:8 · UI02): the 4 px yellow rule `#F4DF3B` marks what matters; the keycap outline (2 px paper `#F4F1E8`) becomes the USE button and the pause button; the hold ring (paper @30 % track + 3.5 px yellow arc) becomes the stick's run line … and USE's hold progress; the yellow key chip … becomes the tappable chip; a pressed USE fills yellow the same way" (`TOUCH_CONTROLS.md:25-30`).

### 3.1 Floating stick (move) + slow ring + run line + thumb — the gait instrument

- **Where:** phone/iPad, left thumb; resting ghost centre (150, 290). **Meaning:** move; the thumb's distance encodes the **gait**: Slow (7–36 pt, inside the slow ring), Walk (36 pt → run line), Run (past the run line r 84, within ±70° ahead) (`TOUCH_CONTROLS.md:56-61`, `:135`). This is the **only place in the game where gait is drawn**.
- **Construction (`FrontRoomsTouchControlsView.cs:641-660`):** base disc Ø120 + 2 pt paper ring (inside stroke); slow ring Ø72, 1.5 pt, dashed 4 on / 4 off (lit = solid ring); run line = arc r 84, ±70°, 2 pt dashed 6/6 (lit = 3.5 pt solid); thumb = disc Ø52. Text-free ghosts are translucent and scaled by CONTROLS OPACITY (default 34 %) (`TOUCH_CONTROLS.md:381`).
- **States/motion (`TOUCH_CONTROLS.md:325-334`):** Idle ghost; **Slow** = slow ring 25 % dashes → solid 60 % in 100 ms; **Walk** = slow ring dashed, run line dashed (firms 35 → 75 % as the thumb nears); **Run** = line + thumb turn **solid yellow** in 120 ms, thumb pops 1.15 → 1, one rigid haptic click, `RUN` rises 6 pt; **Winded** = stick tints muted (200 ms), label `WINDED`, the line refuses the thumb. Kit: `Touch / Stick` (`2530:3828`) states Idle / Slow / Walk / Run / Winded (`:197`).
- **Type neighbour:** the stick label slot — one word at a time, **Bayon 17 paper (RUN yellow) with a 1 pt `#141414` keyline, no chip** since X2: priority `WINDED` > running `RUN` (yellow) > `RUN` preview (paper) > `SLOW · UNHEARD` (after 0.25 s of slow; retires after 1.5 s of counted creep) (`TOUCH_CONTROLS.md:76-77`, `:369`). Placed above the thumb (+33·Scale + 14) or hung under the ring.
- **Issues:** (a) The gait reads only while the thumb is down and only on touch. (b) Walk has no positive mark (it is "slow ring dashed + line dashed", i.e. the absence of Slow and Run). (c) `RUN` yellow = the same token as stamina, key-panel rule, menu focus, USE pressed.

### 3.2 USE button (disc + verb)

- **Where:** right thumb, centre (740, 262); exists only while the dot is on something (`TOUCH_CONTROLS.md:31`, `:136`). **Meaning:** the E key; its **word is the verb** keyed on `AimKind`: OPEN / SHUT / TAKE / LOCKED / UNLOCK / BROKEN / BREAK or TAP / READ / EXAMINE / BACK (in a close-up) (`:41`; `30_crosshair_grammar.md:96-98`).
- **Construction:** disc Ø72 visual / Ø88 hit, **solid `#0E0E0D` 100 %** behind the verb ("text-bearing controls are solid", `TOUCH_CONTROLS.md:380-385`), 2 pt paper ring; verb Bayon 17 paper; LOCKED in muted (9.9 : 1). Kit `Touch / Use` (`2530:3854`) states Open / Shut / Take / Locked / Hold / Tap mode / Pressed (`:198`).
- **States/motion:** press = yellow fill + ink verb in 70 ms, scale 0.92; **locked press** = shake 3 cycles ±4 pt (240 ms), never fills, its ring flashes full paper; **broken try** = swings once round its top point to 4° (B·4's won't-shut curve), never fills; hold = ring r 46 outside the thumb (track 3 pt + arc 3.5 pt yellow); fades with the door/unlock/glass shot (`:335-341`).
- **Issues:** the button is a disc on a dark solid ground. That is within the written scope of the no-card rule: "QUIT TO TITLE keeps its dark fill. A button's fill is the button itself … The rule covers backings for text, not controls" (`research/placard/50_tutorial_placard.md:287`, §11 Q5). But that scope sentence lives in the placard doc, not in the HUD rule texts (`INFO_TYPE_SYSTEM.md:179`, DECISIONS #4).

### 3.3 RUN button (BUTTON mode only)

- Disc r 28 (Ø56), solid ink (`#0E0E0D`) or **yellow when on**, 2 pt paper ring, `RUN` Bayon 17 paper (`FrontRoomsTouchControlsView.cs:630-636`, `:672-674`, `:722`). Kit `Touch / Run button` Off / On / Winded (`TOUCH_CONTROLS.md:200`, Figma `2530:3877`). **Meaning:** run toggle. Text, not a pictogram.

### 3.4 Pause button — the game's only standard UI pictogram

- **Where:** top-right, centre 28 pt from the right edge, 34 pt under the safe top; 32 visual / 44 hit (`TOUCH_CONTROLS.md:131`). **Meaning:** pause = open the settings sheet.
- **Construction (`FrontRoomsTouchControlsView.cs:618-626`, `:670-671`, `:707-708`):** a **keycap** = rounded rect 32 × 32, r 4, 2 pt paper stroke at 90 %, fill Media `#141414` at 35 % (pressed → toward paper); inside, **two paper bars 3 × 12 pt at x ±4** (the "‖" pause sign). Press: scale 0.9 in 70 ms (`TOUCH_CONTROLS.md:342`).
- **Issues:** (a) translucent fill (35 %) on a control, vs "no translucent ink or surfaces" (`:364`) — INFERENCE: tolerated because it carries no text (`:381` text-free ghosts stay translucent). (b) The "keycap" outline here is the only keycap left in play; the key icon brief's house rule is "never a keycap box" (`research/ui_key_icon/02_design.md:283`) — consistent only if "keycap" is read as a button frame, not as a key symbol.

### 3.5 Hold ring (USE's hold progress)

- Ring r 46 round the USE disc: track 3 pt + arc 3.5 pt yellow, clockwise progress, 60 ms smoothing; replaces the desktop hold bar on phones (`TOUCH_CONTROLS.md:42`, `:341`; view `:665-669`). Kit origin: "the hold ring (paper @30 % track + 3.5 px yellow arc)" (`:28`).

### 3.6 TEXT chip (close-up transcript toggle)

- Close-up only, at (778, 28); 48 × 26 chip; **off = the paper word `TEXT` with the HUD keyline, no fill; on = paper chip with the word in ink** (`TOUCH_CONTROLS.md:88`; view `:448-456`). Word, not pictogram. With a pad/keyboard it hides (Y / TRIANGLE / T is TEXT).

### 3.7 Chips (RESUME, END RUN, CANCEL, RESTART, TRY AGAIN, QUIT TO TITLE)

- Words in Bayon 17; **keyline chip** = word in paper, no fill (RESUME); **focused / pressed** = yellow chip, word in ink; **QUIT TO TITLE** = solid ink chip with paper caps ("an exit, not a go"). "Yellow means focus only on a settings page" (`TOUCH_CONTROLS.md:48`, `:111`, `:213`). No icons.

### 3.8 Legend glyphs (title + pause / CONTROLS › GESTURES), 22 pt — the touch pictogram set

- **Where:** title lockup and the gestures page; row A `[move] MOVE  [drag] DRAG · LOOK  [slow] SLOW · UNHEARD  [run] PAST THE RING · RUN` (`[button] RUN BUTTON` in BUTTON mode); row B `[use] USE · DOOR  [hold] HOLD USE · BREAK GLASS` (`TOUCH_CONTROLS.md:75`). Type: **IBM Plex Mono 11 caps** (iPad 12); **glyphs 22 pt centred on the caps**; gaps 6 pt (glyph → word) / 26 pt (entry → entry). Ink on the pause frost; on the title, 100 % paper with the 1 pt keyline (X2 spec v3.3 §3.2; 77 % paper withdrawn because `Outline` draws under the fill).
- **Construction (exact, `FrontRoomsTouchSprites.cs:170-237`; 22 × 22 pt box, centre (11, 11), y up, drawn white and tinted; ring radii are OUTER radii with the stroke inside, as Figma's inside stroke):**

| Glyph | Drawing | Strokes | Partial alpha |
|---|---|---|---|
| Move | ring r 10 + disc r 4 offset (+3, +3) (thumb pushed up-right) | 2 | — |
| Drag | horizontal line y 11, x 3–19 (2 pt) + disc r 3.5 at (4, 11) + disc r 2.5 at (19, 11) | 2 | end disc 45 % |
| Run | faint stick ring r 6.75 at (11, 7) + run-line arc r 10.75 ±60° + thumb disc r 2.25 at (11, 19.4) | 1.5 | stick ring 35 % |
| Use | ring r 10 + centre disc r 3 | 2 | — |
| Hold | track ring r 9 (22 %) + 60 % clockwise arc from the top | 2.5 | track 22 % |
| SprintButton | ring r 10 only | 2 | — |
| Slow | stick ring outer r 10.5 (35 %) + slow ring outer r 5.05 + knob disc r 2.25 (Ø 4.5); centrelines 9.75 / 4.30 = the doc's "stick ring r 10 … slow ring r 4.29", ≈ 0.43 × r = Ø72 slow ring / r 84 run line (`50_tutorial_placard.md:268-283`; Figma `3503:7311`, signed by 平面视觉 10-22) | 1.5 | stick ring 35 % |

- **Issues:** (a) **Three stroke weights in one row** (1.5 / 2 / 2.5 pt) and translucent parts (22–45 %) inside glyphs, against "no translucent ink" (`TOUCH_CONTROLS.md:364`; `30_crosshair_grammar.md:92`). (b) **Silhouette collisions:** SprintButton (plain ring) = Use without its dot = Move without its offset thumb; Run uses ±60° for the line while the real run line is ±70° (`TOUCH_CONTROLS.md:40`). (c) The Run glyph's thumb sits above the line (pushed ahead) — the only glyph that encodes direction. (d) These glyphs have **no desktop counterpart**: desktop shows key names (CONTROLS › KEYBOARD & MOUSE keycaps, §4.4).

### 3.9 Mini USE / Hold glyph in front of the phone prompt

- **Where:** phone prompt line under the dot. **Meaning:** "press USE" (replaces the desktop's `E  ·`). **Construction:** the legend `Use` glyph (or `Hold` on glass) at **18 × 18 pt**, its centre 15 pt left of the prompt's left edge, tinted the prompt colour (`FrontRooms3DGame.Mobile.cs:490-512`). **None for a refusal** (LOCKED / BROKEN), and hidden with a pad or keyboard (`TOUCH_CONTROLS.md:112`; `Touch / Prompt` Tap / Hold / Locked, `:199`). T-3: hidden while the key HUD holds the prompt (DECISIONS `:60`).
- **Issues:** no keyline on the glyph while the text beside it has one (INFERENCE from the code: no `Outline`/keyline is added to `mobilePromptGlyph`); 18 pt ≠ the legend's 22 pt (two sizes of one symbol).

### 3.10 iPhone Duo / settings-only marks (touch)

- Settings controls on touch (SWITCH 40 × 22 keyline box, SEGMENTS keyline cells, SLIDER 170 pt track with ticks, 6 × 22 thumb, changed-row dot, frame meter) are the shared settings kit, see §4.3 (`TOUCH_CONTROLS.md:210`).
- Transcript panel glow lines: 2 pt **phosphor** rule `#D6DEBD` at the left of each glow line (U+0001 lines) (`TOUCH_CONTROLS.md:89`).

---

## 4. Desktop HUD text blocks, menus, settings, key map (Mac / Windows; settings shared with touch)

Sources: `Documentation/research/desktop_ui/README.md` (Figma desktop masters `2911:6716`, flows `2920:6693`, kit `DK · Desktop kit masters` `2911:6720`), `FIX_CARD_VL217.md`, `SETTINGS_KIT_REVIEW_1024.md`, `EXAMINE_TEXT_G9_G10.md`, `Documentation/DISPLAY_SETTINGS.md`, `research/interaction/settings/IA.md`, `INTERACTION_DESIGN.md` key map (`:171-190`); code `FrontRooms3DGame.cs` BuildHud (`:3385-3470`), `FrontRoomsDesktopSettingsView.cs`, `FrontRoomsSettingsMenu.cs:450-451` (metrics), `Input/FrontRoomsKeyMapView.cs`.

**Iconography stance on desktop (quoted):** the desktop HUD is "direct typography overlays" (`FrontRooms3DGame.cs:3389-3391`); G10: "Touch words, not new glyphs … So G10 needs one new control, the TEXT chip, **and no icons**" (`EXAMINE_TEXT_G9_G10.md:13-15`). Desktop flows table: title has "no 'press' text on desktop"; Winded = "all segments dim; **no text on desktop**" (`research/desktop_ui/README.md`, D01, D05).

### 4.1 HUD text blocks (no icons; listed because they carry the keyline + yellow tokens)

| Block | Construction | Tokens | Source |
|---|---|---|---|
| Room block (top-left, 72 px margin) | meta Plex Mono 13 paper + keyline; room name Source Serif 4 50 (phone 26) paper + keyline; **phone only: yellow zone rule 3 × 40** | paper, keyline, accent | `FrontRooms3DGame.cs:3392-3403` |
| Threat readout (assist, off by default; "IT 23 M / TIER 2") | state Bayon 20 **bold, accent yellow**; distance Plex Mono 13 **muted `#BDBAB0`**; INFERENCE: no keyline added in BuildHud | accent, muted | `:3405-3416`; `DISPLAY_SETTINGS.md:50`, `:52` |
| Flash line (glass hint), captions | paper + keyline, **dark card removed** (A6; R4 Red 10-29) | paper, keyline | `:3418-3440`; `FIX_CARD_VL217.md:55` |
| Prompt line | Bayon 20 / 17, paper, **2 px keyline desktop** (G6), 1 pt phone | paper, keyline | `:3447-3455`; DECISIONS #38 |

### 4.2 Menu focus marker (the yellow bar)

- **Where:** desktop settings sheet rows, key map, (earlier) pause/caught CTA lines; touch settings rows. **Meaning:** "you are here" (focus). "Yellow means focus only on a settings page" (`TOUCH_CONTROLS.md:213`); "yellow means focus or the primary action only" (`IA.md:130`); kit YELLOW rule: "Focus only (the focus bar). No dark backing cards: keylines." (`SETTINGS_KIT_REVIEW_1024.md:28`).
- **Construction:** **4 px wide `#F4DF3B` bar**; in the CTA form it is **as tall as the caps (0.70 × 20 = 14 px)**, right edge **10 px** left of the text, centred on the caps (`FIX_CARD_VL217.md:29`, verified VL413 `:133`); in the settings list it is the **full row height (48 px)** at the row's left edge (`FrontRoomsDesktopSettingsView.cs` `UpdateRows`: `PlaceIn(focusBar, 0, f.Y, 4, f.H)`). Motion B1: the bar slides 160 ms (`INTERACTION_DESIGN.md:183`).
- **Issues:** "It is the same mark as the HUD key panel's yellow rule" (`FIX_CARD_VL217.md:29`) — so one mark means *focus* in menus and *your key slot* in play; two heights (cap-height vs row-height).

### 4.3 Settings controls (desktop metrics / touch metrics, `FrontRoomsSettingsMenu.cs:450-451`)

| Control | Construction | States | Notes |
|---|---|---|---|
| SWITCH | word `ON`/`OFF` (Bayon label size) + keyline box **48 × 24** (touch 40 × 22) with a knob **16** (touch 14) | on = knob right, paper 100 %; off = knob left, paper 30 %, word 70 % | "switches always show their word" (`SETTINGS_KIT_REVIEW_1024.md:61`) |
| SEGMENTS | keyline cells, height 32 (touch 30), min cell 48 (44), digits in Plex Mono | selected = paper fill + ink text; unselected = text 75 % paper | "no numbers in segments" for amounts (Red 10-24, `TOUCH_CONTROLS.md:206`) |
| SLIDER | track **240** (touch 170) px with a 1 px tick per stop (**default's tick 12 px tall, others 6 px**, paper 35 %), **3 px fill**, **thumb 6 × 24** (touch 6 × 22), value Plex Mono 20 | — | Red: "直接放更简单直的horizontal scroll bar" (`DISPLAY_SETTINGS.md:28`) |
| Frame meter (FRAME) | **10 dashes 16 × 4 px on a 19 px pitch**, paper 20 % unlit; value `9.8 MS · 60 FPS` Plex Mono 13 | lit per measured ms | "in the stamina-dash look … acceptable: it is labelled" (`SETTINGS_KIT_REVIEW_1024.md:46-47`) |
| Changed-row "dot" | **6 × 6 px square** (an `Image` with no sprite) before the control, paper 80 % | shown when the row differs from its preset | "A changed row says so by shape (a dot before its control), not by colour (R3 rule 11)" (`FrontRoomsDesktopSettingsView.cs` `UpdateRows`) |
| Tabs + Q / E keycaps | tab = Bayon 20 + **2 px underline** (selected 100 %, others 60 %); flanking **keycaps = Plex Mono 13 in a 24 × 24 px 1 px keyline box** (`Set·D / Keycap`) | — | `SETTINGS_KIT_REVIEW_1024.md:63`; view `:495-517` |
| Page row (KEYBOARD & MOUSE) | keycaps (Plex Mono 13, 1 px keyline 50 %, 24 px tall) + `›` (Plex Mono 13, 80 %) | — | view `:611-628` |
| Cost bars ("four-step cost bar", COST heading) | in the Figma kit `3692:7842`; **in code the cost is a text line** `FRAME COST  HIGH: CHANGE THIS FIRST / LOW` | — | `SETTINGS_KIT_REVIEW_1024.md:64`; `FrontRoomsDesktopSettingsView.cs:739` |
| RESET / BACK / RESUME | keyline button, word in paper, no fill; RESET armed = yellow fill + ink | — | `SETTINGS_KIT_REVIEW_1024.md:24-28` |

- **Issues:** (a) the changed "dot" is a **square**, the crosshair dot a **circle**: two "dots" (INFERENCE from code: default UI sprite). (b) Translucent paper (20–80 %) is used throughout settings marks (ticks 35 %, meter 20 %, keycap keylines 50 %, off-knob 30 %) — the kit justifies it on the 97 % surface, but it contradicts the blanket wording of "no translucent ink or surfaces" (`TOUCH_CONTROLS.md:364`). (c) The frame meter deliberately echoes the stamina dashes (16 × 4 vs 24 × 6) — the review accepts it (`:46`).

### 4.4 Key map (CONTROLS › KEYBOARD & MOUSE, landed 10-29)

- **Keycaps:** a keyline tenkeyless keyboard + mouse in columns 1–8; unit **U = 40 px, keycap 36 × 36 px**, 1 px keyline; keys in use = paper, free keys = **25 % keyline**, chosen action = yellow (focus); key names in Bayon words where Bayon has no glyph (UP, COMMA, LEFT CLICK) (`FrontRoomsKeyMapView.cs:10-25`, `:182`; `INTERACTION_DESIGN.md:175-180`). Leaders: 1 px lines routed by A* through the gutters.
- **Padlock (third drawing in the game):** "fixed keys carry one": **a 5 × 3 shackle in a keyline over a 7 × 5 body**, built from four 1 px rectangles + one filled 7 × 5 rect, paper at **75 %**, placed before the action word (Bayon 20) (`FrontRoomsKeyMapView.cs:291-294`, `:315-321`). Meaning: "this binding can't be changed" (SETTINGS Esc, START, arrows' MOVE, ZOOM STEP, LOOK).
- **Changed dot:** 6 × 6 square, the settings rule.
- **Issues:** **the padlock now has three unrelated constructions** (HUD L3·D tag padlock, placard padlock §5, key-map pixel padlock) and **two meanings** (door locked vs setting fixed). See §9.2.

### 4.5 CAUGHT, title, wordmark

- CAUGHT: Bayon 88 (touch 48) + subtitle `OCCUPANT ACCOUNTED FOR` + stats Plex Mono 20 — **no icon** (`FIX_CARD_VL217.md:57-72`).
- Title: logo only (`FrontRoomsLogo_White.svg` at 0.75 in the kit; "Brand / FrontRooms logo"), legend in Plex Mono 13 (desktop T5) (`research/desktop_ui/README.md:11`; `FIX_CARD_VL217.md:138-139`). Logo rules in §8.

---

## 5. Evacuation-plan placard pictograms (in-world print; 平面视觉 owns layout/type/art generator)

Sources: `Documentation/research/placard/50_tutorial_placard.md` (rules §2 `:55-75`, type §4 `:109-128`, X2/T5 answers §11 `:224-300`, phosphor §18 `:431-466`, wall arrows §19 `:485-495`); generator `Tools/print/ink/art_from_graphic/placard/tutorial_placard.swift` (pictograms `:63-142`, sheet/legend `:143-245`, inks `:580-605`); `Tools/print/ink/art_from_graphic/placard/README.md`. Sheet 432 × 279 mm (17 × 11 in snap frame), units mm, y down, rendered 6 px/mm. Figma section `3459:7280` (TP00–TP07). In game: Phase 0 sheet landed 10-22 21:03; Floor 1 room sheets FD-b on HOLD (pairing A35: LEVEL 0 = `sheetA_room/`, B1+ = `sheetA_phase0_B1/`, README `:84`).

**Placard rules (quoted):**
- "**One symbol, one rule.** The padlock means 'a key opens this', and nothing else. The start door is drawn shut with NO REENTRY, never with a padlock: no key opens it." (`50_tutorial_placard.md:63-65`)
- "Turned to the reader (ISO 23601 §5 j …): up on the plan is the wall the placard hangs on … north points down." (`:66-68`)
- "1990 US print: Heros Bold caps; black, and white on safety red, on off-white; the existing 17 × 11 in snap frame; no brands; the enemy never drawn and never named." (`:69-73`)
- "Never a radar: nothing on the sheet moves or knows where the hunter is." (`:74`)
- Pictograms "8–40 mm; strokes ≥ 0.6 mm; walls 1.8 mm poché (ISO §6 c, d)" (`:120`); generator header: "stroke >= 0.6 mm, ISO 23601 §6 d; sign height >= 7 mm, §6 c" (`tutorial_placard.swift:63`).

**Inks (lit print, `tutorial_placard.swift:604`):** paper `#F4F1E8` (= the HUD paper), print black `#171717` (0.09), **safety red `#C70F2E`** (0.78, 0.06, 0.18; ≈ ANSI Z535.1 safety red Munsell value 4, `50_tutorial_placard.md:457`; same hex as the Shot-format FAIL chip), tint `#9E9C94` (unused by current sheets), phosphor underprint band `#DDE5C3` (rev 2; v3 `#D6DEBD`), 1.5 mm round every glowing mark (`:431-466`). Glow mask = YOU ARE HERE, route, stair door + padlock + EXIT (`:128`).

**Type beside the symbols:** TeX Gyre Heros Bold caps (Helvetica clone) — legend labels / YOU ARE HERE / EXIT **5.0 mm caps**; header 15 mm caps; block heads 3.6–4.4 mm; fine print 2.8–3.6 mm (`:113-119`). **Legend alignment (INFERENCE, computed from the generator):** symbols are centred at x 310, labels start at x 326 (16 mm); each symbol's centre sits on the label block's cap middle (YAH at y 72 ↔ label baseline 74.5 − 2.5; the stair box centre 97 ↔ two-line block middle 97.3) — the same rule as the touch legend ("glyphs centred on the caps").

| Pictogram (function) | Meaning | Construction (mm; k = scale) | Stroke check vs ≥ 0.6 mm (INFERENCE: computed) | Used |
|---|---|---|---|---|
| **YOU ARE HERE** `yah` | the reader's position | red filled dot r 3.4 + red ring r 5.3, stroke 0.6 (`:74`) | ring 0.6 OK | plan (glows) + legend (printed only) |
| **Route** `route` + `head` | the way to go | red polyline **1.4 mm**, stopped 3.6 before the tip; filled triangular head len 4.2, half-width 2.6 ("ISO 3864-3 arrow type D spirit", `:68-71`) | OK | plan; glows |
| **Stair in plan** `stairPlan` | the stair (EXIT), one floor down | frame 0.6; treads **0.35**; centre run line 0.8 + black head (3.6 × 2.0) pointing down the run; start dot r 0.9; `DN` 2.8 mm caps (`:76-82`) | **treads 0.35 < 0.6** | plan + legend |
| **Stair room** `stairRoom` | the goal enclosure | poché walls 1.8; shut leaf 14 × 0.9; stair plan inside; padlock 11 on the door; red `EXIT` 5.0 mm caps over it; phosphor block (glows as one, the safe area) (`:192-199`) | OK | plan |
| **Padlock** `padlock` | "a key opens this" | "in the HUD padlock's proportions (Lock_rect_*: body 39.3 × 20, shackle half-width 8.5, stroke 2.6), scaled to body width w": filled ink body (r 3.4k), **symmetric** legs 4k + semicircle, butt caps; a **vertical paper slot 6k × 10.2k** knocked out of the body (`:83-90`). Sizes: legend 8, LOCKED DOOR row 9, room-sheet door 10, stair door 11 | shackle = 2.6 × w/39.33: **w 8 → 0.53, w 9 → 0.595** (< 0.6), w 10 → 0.66, w 11 → 0.73; total height at w 8 ≈ 6.9 mm (< 7 mm §6 c) | legend, plan |
| **Security desk (key)** `station` | the key is here (desk + playing TV + key on a hook) | desk top 40k × 2.4k, leg, pedestal with drawer line (0.35k, paper), console CRT `tv` (16k), hook dot, **`keyTag` 14k wide**; optional sound arcs: 3 dashed arcs 0.45 mm [1.4/1.4] (`:181-191`) | key-ring arc **1.0 × k_tag = 0.26 mm (legend, w 22) / 0.47 (plan, w 40)**; sound arcs 0.45 | legend (w 22), plan (w 40 / 26) |
| **Key + split ring + tag** `keyTag` | the key (B glyph as an ink silhouette) | rect tag 22k × 13k (r 2.4k) with paper insert (blank, **no floor token**), neck, ring = 280° arc r 4.6k stroke 1.0k (the split ring's gap), bow 8.2 × 7.6 with paper hole r 1.1, blade 8.6 × 2.8, teeth = 3-peak zigzag (`:91-104`) | see station | inside station only |
| **CRT TV** `tv` | the playing TV (heard through walls) | rounded body (r 0.08w), paper screen 0.62w × 0.70h, 2 knobs, 2 feet; optional 3 sound arcs ±38°, 0.6 (`:105-112`) | OK | inside station |
| **North arrow** `north` | orientation (north points DOWN) | ring r 6, stroke **0.35**; filled arrow poly; `N` 3.2 mm (`:241-243`) | **ring 0.35** | notes column |
| **Drafting break** `brk` | "not to scale beyond here" | two slanted ticks 7 × 2.4, stroke 0.6 (`:244-245`); legend row `BEYOND BREAK: NOT TO SCALE` on room sheets (`:568`) | OK | plan, legend (room sheets) |
| **Wall-arrows swatch** `swatch` | "WALL ARROWS / POINT THE WAY OUT" | the wallpaper's turned motif cut from `legend/flow.png` at 1/25, 15 × 15 mm, 0.35 frame; printed, never glowing (`:202-214`; `50_tutorial_placard.md:485-495`) | frame 0.35 | legend (L0 sheets) |
| **Walls / door** `wallH/V`, `planDoor` | building | poché 1.8 (legend 1.4); `planDoor` leaf 0.5 + swing arc 0.3 (`:131-139`) | planDoor 0.5 / 0.3 (function unused today) | plan |
| Library, **unused** in current sheets: `lampDip` (troffer + dip rays), `steps` (two shoe prints behind a wall stub), `bell` (fire-alarm gong), `marker` (numbered ink disc) (`:113-141`) | v1/B/C ideas: lamps dim = near, heard steps, alarm | strokes 0.5–0.6 | — | none (grep: no call sites) |

- **Issues:** (a) the generator's own ≥ 0.6 mm rule is broken by treads (0.35), the north ring (0.35), frames/rules (0.35), the key ring inside the station (0.26–0.47) and the legend padlock (0.53 at w 8). (b) **The placard padlock is a third construction** (filled body, symmetric legs, vertical keyhole slot, no tag insert/number) vs the HUD padlock (tag body in zone tint + paper insert + floor token, asymmetric legs) vs the key-map pixel padlock. (c) The placard key tag is **blank** while the HUD tag shows `00`/`B1`. (d) Period tension: the placard cites ISO 23601 (2009/2020) and ISO 3864-3 for its grammar, while the print research says "Leave … ISO-style pictograms. ISO 7010 dates from 2003. A 1990 US exit is red EXIT lettering (NFPA 101)" (`research/print_reference/FINDINGS.md:185`) and the key research says "Use AIGA/DOT, not ISO, as the 1990 reference" (`research/ui_key_icon/01_research.md:279`). INFERENCE: the placard uses ISO 23601 as legibility/geometry rules only; its drawn symbols are architectural plan symbols + custom object drawings, not ISO 7010 graphics — but this is not written down as a rule.

---

## 6. Signage (in-world, 3D textures)

| Sign | Meaning | Construction | Colour | Type | Source |
|---|---|---|---|---|---|
| **EXIT sign** (Kit_ExitSign family: wall, hanging double-face, flush, end; each with `_Dead`) | the goal beacon: the floor's way on (lit-locked on its own emergency circuit) | **stencil face, lettering = geometry, not a font**: H 152.4 mm (6 in), stroke 19.05 (¾ in, 1:8), E/T 54.0 (0.354 H), X 60.0 (0.394 H), gaps 12.0, square ends, stamped R 0.5 corners; no bridges needed | red diffuser (sRGB red primary transmission); colour approved 10-15: hue 0–8°, sat ≥ 0.8 close / ≥ 0.6 at 3 m | — | `research/exit_sign/10_spec.md:224-250`; `GRAPHIC_VISUAL_TASKS.md:316-334`; `70_lit_lock.md:49` |
| **EXIT chevrons** | direction to the exit | knockout chevrons 57.15 high (2.25 in), 24.6 wide (w/h 0.43), arm 11.1, 10 from the letters; **default both closed** (scored, unlit: "no arrows; the exit is straight ahead"); `_ChevL`/`_ChevR` open variants (C23 step 2: chevron hanging signs in the exit zone) | lit red when open | — | `10_spec.md:199-204`; `VISUAL_CHAT_TASKS.md:28` |
| **Stencil floor token** (stair, hinge side) | which floor you are on | TeX Gyre Heros Bold cut as a stencil, **cap 15 in (381 mm)**, bridges 0.052 cap; `B10+` Heros Bold Condensed 13.5 in | black enamel `#1E1E1C` | Heros Bold | `research/info_type/STAIR_SIGNS_SPEC.md:33-41` |
| **Stair ID sign** 12 × 14 in | `STAIR A` / `NO ROOF ACCESS` / `LEVEL` + token / `LEVELS 0 TO B1` | two-ply engraved plastic 1/16 in, routed 0.3 mm, 45° bevel, r ⅛ in; 4 screws | **white on black** (= "where you are / where you can go") | Heros Bold caps (token 5 in) | `:9-20`, `:43-70` |
| **Door plates** `NO REENTRY` / `REENTRY` 11 × 5 in; **`NOT AN EXIT`** 12 × 3 in; stair letter `STAIR / A` 6 × 6 in | re-entry rules; "this door is not the exit" | engraved plates as above | **pale plates (black on white) say no**; "Red stays only on EXIT signs and the standpipe"; NOT AN EXIT "one line at one size, black on white. A bigger EXIT, or red, would read as an EXIT sign at a glance" | Heros Bold caps 1 in verdicts | `:58-62` |
| **Area of rescue assistance** (lit, every exit landing) | the stair is a safe zone | 18.5 × 6.5 in face in a white steel box; **ISA (static 1968, ADAAG 1991 Fig. 43(a)) drawn from strokes**, 118 mm tall = text-block height | white `#F0F2F0` on **accessibility blue `#004A8C`**, lit steady; "never red, no arrows" | `AREA OF / RESCUE / ASSISTANCE` | `STAIR_SIGNS_SPEC.md:19`; `W/info_type/stair_signs/out/rescue/README.md:6-26` |
| First-room signs | `SECURITY` (brushed-aluminium letters on the desk front), BUSINESS HOURS plate 18 × 6 in | letters / plate | — | — | `INFO_TYPE_SYSTEM.md:195-196` |
| Locked-door marks (world) | a key door reads differently | almond enamel storeroom leaf, 10 in kick plate, dark engraved `EMPLOYEES ONLY` plate at 60 in, zone number plate | — | engraved caps | `research/interactables/05_locked_door_type.md:32-40` (10-02 spec; INFERENCE: zone plates predate the floor-key change) |

- **Issues:** (a) **Arrows: three unrelated drawings for "this way"** — EXIT chevron (open knockout, w/h 0.43), placard route head (filled triangle 4.2 × 5.2), wallpaper turned chevron band (55° arms, §7) — plus the touch Run glyph's arc. (b) Signage is words-first by period rule (NFPA/UL lettering, LL5 plates, OSHA "Not an Exit" words only); the only pictogram on any sign is the ISA. (c) The stencil `0` for LEVEL 0 vs the HUD tag's `00` vs the HUD room line `LEVEL 0`: three notations for one floor (§1.3).

---

## 7. Wallpaper arrow cue (the motif that turns to point the way) — no overlay glyph

Sources: `Documentation/research/cue_driver/CUE_DRIVER_INTERFACE.md` (v0.2.5, 10-24); `research/wallpaper_motion/32_pattern_native_hints.md` §2, §9; placard legend row §19. Status: prototype (`W/proj_pgcue2`), **not in main**; map side after the Alpha (`CUE_DRIVER_INTERFACE.md:33-37`).

- **The element:** wallpaper WP03 (`Tools/print/patterns/hard_edge.py`), per 750 mm roll: static stripes (rails), **field chevron stacks** (two per unit, half-dropped, **206.7 mm wide, 55° arms, pointing UP at rest**), motif-band down-arrows with diamonds (4 per roll) (`32_pattern_native_hints.md:22-25`). The piece that turns is the **64 mm grey band of the lower chevron** (turned it is 211.6 mm, fitting its 206.7 mm field edge to edge) (`:214-216`).
- **Meaning:** FLOW = the way on: points at the station TV's cell until the floor key is taken, then at the EXIT door (C24 = A + A20); in a chase, a pressure route toward shuttable doors leading away (`CUE_DRIVER_INTERFACE.md:111-118`). Placard legend: `WALL ARROWS / POINT THE WAY OUT`.
- **Motion (decided Red 10-22 15:05, A + M4 + V1):** A = every arrow on a route wall turns to point along the route; **M4 = the turn ratchets in three 30° clicks** (cross-fade 0.07 s per click); **V1 = rows then step along the route**, 6 clicks × 0.25 s = one 750 mm roll; chase wave 8 m/s; retract 2 s outside-in; **Reduce Motion / WebGL: turn only** (`:20-24`, `:136-153`).
- **Rules:** "No new shapes, no type, no glow overlay. If the player doesn't look, it is just wallpaper." (`32_pattern_native_hints.md:18`); "WP03's own up-chevrons look like arrows already. The hint must be a change against that baseline … never 'up'" (`:178`); walls never glow (Red Q4 = B); no darkness gate; the cue never leads toward the Relay (`CUE_DRIVER_INTERFACE.md:3-6`).
- **Issues:** (a) The resting wallpaper is covered in up-pointing chevrons that mean nothing (ground), so "arrow = way" holds only for *turned* arrows — a meaning carried by change, not by shape. (b) HERE / STOP / BREACH vocabulary is reserved but not in v1. (c) Its direction marker (a chevron band) shares nothing with the EXIT chevron or the placard arrowhead (§6 (a)).

---

## 8. Other symbol systems (not in play, but part of the language)

| System | Marks | Source |
|---|---|---|
| **Logo / wordmark** FRONTROOMSSS | the two trailing "relay S" fade from the background into the ink: **S1 to 100 %, S2 to 25 %**; white logo on black, black on white, white on yellow, yellow on black, transparent start on photos (Figma icons `2687:10454 / 10459 / 10464 / 10469`, lockup `2235:44`); never recoloured, stretched or semi-transparent over evidence; clear space ≥ ½ letter height | `GRAPHIC_VISUAL_TASKS.md:229-236`; `SHOT_FORMAT.md` §4 |
| **Shot-format verdict chips** (system screenshots) | paper fill = PASS…; **safety red `#C70F2E`** fill = FAIL / RISK; **yellow `#F4DF3B`** fill = WAIT-RED ("the one colour for an open decision of Red's"); 1 px paper outline = FLAG…; dashed muted outline = PENDING / SUPERSEDED; Plex Mono Medium 14 in a 24 px box; header ink `#0A0A0A` ("not the HUD keyline #141414") | `SHOT_FORMAT.md:24-30`, `:58-66` |
| **Info status tags** (docs, Figma `3189:8204`) | three classes, one look each: ink fill (LIVE, PART) / 1 pt outline (BUILD, CLONE, OFF) / 0.75 pt dashed grey (DESIGN, OPTION, NONE); decision yellow fill | `INFO_TYPE_SYSTEM.md:44-53` |
| **Print forms** (playtest FR-PT04/05) | fill targets = **12 pt boxes on the 6 pt grid**; Red rejected ovals ("不是特别喜欢椭圆形") and the wallpaper arrow ("箭头没办法fit到网格里面"); canary paper `#FCF7CE` ("the brand yellow #F4DF3B at 25 %") | `GRAPHIC_VISUAL_TASKS.md:71`; `GRAPHIC_VISUAL_SPLIT.md:55`, `:92` |
| **Transcript glow rule** | 2 pt phosphor `#D6DEBD` rule at the left of a glow line, "so colour is never the only cue" | `EXAMINE_TEXT_G9_G10.md:44` |

---

## 9. Cross-cutting observations

### 9.1 Shared construction rules that already exist

| # | Rule (quoted / condensed) | Source | Where it is applied |
|---|---|---|---|
| R1 | **No dark backing cards; a keyline instead.** "Legibility comes from a 1 px ink keyline, #141414 at 85 %: Unity `Outline` on text … and an ink copy 1 px larger behind every paper part of a sprite, in one 85 % group." Red 10-14: "我不希望有背后的这个黑色底". One shared constant `FrontRoomsHudStyle` (linear alpha 0.95; prompt line 1 = 2 px desktop, 1 pt phone). | `INFO_TYPE_SYSTEM.md:179`; `lock_motion/README.md:38-44`; DECISIONS #4/#22/#31/#38; `FrontRoomsHudStyle.cs` | dot, padlock, door, eye, B glyph, prompt, panel label, room block, captions, flash line, X2 stick words, title legend + its glyphs |
| R1a | **Scope:** "The rule covers backings for text, not controls" (QUIT TO TITLE keeps its dark fill; "A button's fill is the button itself"); reading surfaces (settings, transcript) use `#0E0E0D` at 97 %. | `50_tutorial_placard.md` §11 Q5 (`:286-287`); `EXAMINE_TEXT_G9_G10.md:27`; `SETTINGS_KIT_REVIEW_1024.md:17-23` | USE disc, RUN button, chips, settings sheet |
| R2 | **Motion comes out of an existing element** (the crosshair dot) and goes back into it; never an overlay. Red 10-14: "利用好鼠标中心的原点，而不是直接覆盖一个动效在上面". States = build / react / leave with no-retrigger windows, unscaled time. | `INFO_TYPE_SYSTEM.md:180`; `README.md:73`; `GRAPHIC_VISUAL_SPLIT.md:118`; DECISIONS #16 | L3·D, B·4, E·1, F·1, P4 (the glyph itself flies), U·3 (the panel key itself turns), touch USE's locked shake / broken swing |
| R3 | **One glyph, one meaning / one symbol, one rule / one colour, one meaning.** | `30_crosshair_grammar.md:64`; `50_tutorial_placard.md:63-65`; `INFO_TYPE_SYSTEM.md:191` ("Not the glow-ink yellow-green: one colour keeps one meaning"); `research/print_reference/FINDINGS.md` "FrontRooms keeps one yellow per world"; `SETTINGS_KIT_REVIEW_1024.md:27` ("yellow should mean only one thing here, focus") | eye; placard padlock; terminal colour; settings |
| R4 | **Every state is a shape; no meaning by hue alone.** | `30_crosshair_grammar.md:92`; settings "changed row … by shape (a dot …), not by colour (R3 rule 11)"; glow lines get a 2 pt rule "so colour is never the only cue" (`EXAMINE_TEXT_G9_G10.md:44`) | crosshair states, settings, transcript |
| R5 | **No translucent ink or surfaces in in-game UI** (Unity blends in linear light); hierarchy from face, size, tracking; a second line is quieter "only by its face and size, never by a dimmer ink" (T5). Exception: text-free ghost controls. | `TOUCH_CONTROLS.md:364-367`, `:381`; `INFO_TYPE_SYSTEM.md:181`; DECISIONS #39 | HUD text, frost cards |
| R6 | **Words before icons.** "Touch words, not new glyphs … no icons" (G10); the USE disc speaks in Bayon verbs; desktop prompts name keys as letters; key-icon house rule "Never brass, never yellow, never a keycap box". | `EXAMINE_TEXT_G9_G10.md:13-15`; `02_design.md:283` | prompts, USE, TEXT chip |
| R7 | **Numerals never in Bayon** (1 → I, 0 → O); Bayon's "·" falls back to IBM Plex Mono; digits in Courier Prime (typed tags) or Plex Mono (settings, stats). | `TOUCH_CONTROLS.md:188`; `FIX_CARD_VL217.md:49-51`; `SHOT_FORMAT.md` §3 | tag token, settings values, CAUGHT stats |
| R8 | **Photosafety:** ≤ 3 changes/s; one slow blink per build; dot dwell ≥ 200 ms; Reduce Flashing (no blink); Reduce Motion (alpha ≤ 0.15 s / 108 ms touch); "Any future 'waiting' signal may only be a hard on/off of a solid element". | `30_crosshair_grammar.md:85-92`; DECISIONS #39 (2); `TOUCH_CONTROLS.md:351` | eye, dot, title |
| R9 | **Glyphs centred on the label's caps** (touch legend 22 pt glyphs "centred on the caps", 6 / 26 pt gaps); the placard legend does the same in mm (INFERENCE §5); the menu marker is cap-height (14 px) | `TOUCH_CONTROLS.md:75`; `FIX_CARD_VL217.md:29` | legends, menus |
| R10 | **Period reference for pictograms = AIGA/DOT 1974–79, not ISO 7001/7010**; "everything dated 1990 or earlier … AIGA/DOT symbols. No 1991 orange WARNING, no ISO 7010." | `research/ui_key_icon/01_research.md:271-280`; `research/print_reference/IMMERSIVE_MATERIALS.md:86`; `FINDINGS.md:185` | key glyph research, print |

### 9.2 Where the icon families disagree

1. **Padlock: three constructions, two meanings.** HUD L3·D = tag-shaped body in zone tint + paper insert + floor token, asymmetric legs, stroke 2.6 px (§1.5); placard = filled black body, symmetric legs, vertical keyhole slot, no number, shackle 0.53–0.73 mm (§5); key map = 7 × 9 px pixel padlock, square shackle, paper 75 % (§4.4). The key map's padlock means "this binding is fixed" — against the placard rule "The padlock means 'a key opens this', and nothing else" (`50_tutorial_placard.md:64`).
2. **Key: four pictures.** HUD B (paper key + tinted tag + token, ring 1.27 px), placard `keyTag` (ink silhouette, **blank** insert, ring 0.26–0.47 mm), the map's interim `HUD_KeyGlyph.png` (generic ring key, 40 × 22), and the 3D pickup (still a yellow glowing cube until D1.6) (§1.2, §1.9, §5).
3. **"This way": no shared arrow.** Placard route (red 1.4 mm line + filled triangle), EXIT chevrons (knockout, w/h 0.43, default closed), wallpaper turned chevron band (55° arms, 64 mm band), touch Run glyph arc (±60° while the real run line is ±70°), settings `›` (§3.8, §5, §6, §7).
4. **Door:** crosshair B·4 (elevation: frame + leaf + knob, stroke 2) vs placard plan symbols (shut leaf rect; `planDoor` swing, unused) vs touch BROKEN (word + disc swing). The only door *pictogram* is B·4.
5. **Hold progress:** desktop bar (120 × 4, white 25 % + yellow) vs touch ring (r 46, 3 + 3.5 pt) vs legend Hold glyph (60 % arc, track 22 %).
6. **Stroke vs type stems:** HUD crosshair strokes 2–2.6 px + keyline next to Bayon 20 (~2 px stems per G6, `FrontRoomsHudStyle.cs:27`) — consistent; but the B ring is 1.27 px, the touch legend mixes 1.5 / 2 / 2.5 pt beside Plex Mono 11, and the placard mixes 0.35–1.4 mm beside Heros Bold 5 mm caps. No doc ties icon stroke to the neighbouring stem except G6 and "same stroke weight and outer diameter as the 10-08 Run glyph" for the Slow glyph (`50_tutorial_placard.md:268-271`).
7. **"Dot" means three things:** aim point (crosshair, white circle Ø16), "changed" (settings/key map, 6 × 6 paper **square**), "you are here" (placard red dot + ring); also the Use glyph's centre disc and the Slow glyph's knob.
8. **Yellow `#F4DF3B` carries many meanings in play:** key-panel rule (+ U·3 click), stamina fill, hold-bar fill / hold arc, touch RUN word + lit run line + thumb, USE pressed, menu/settings focus bar, RESET armed, threat-readout state text, phone zone rule; in docs: decision yellow / WAIT-RED; in print: canary paper at 25 %. The written rules say "focus only" (settings) and "one yellow per world" — one hue, but not one meaning.
9. **Near-blacks without a family table:** `#141414` (keyline, Media), `#0A0A0A` (touch Ink, doc ink; "never" for the keyline, `50_tutorial_placard.md:298`), `#0E0E0D` (card, surfaces), `#1E1D1A` (tag digits), `#171717` (placard black), `#1E1E1C` (stencil paint), `#1D1C1A` (plate black). Whites: `#FFFFFF` (dot, pupil) vs `#F4F1E8` (paper, O-14), `#ECEAE4` (plates), `#F0F2F0` (ISA).
10. **Translucency vs R5:** touch legend glyph parts 22–45 %, stamina/hold empties white 25 %, pause keycap fill 35 %, key-map padlock 75 %, settings ticks 35 % / meter 20 % / off-knob 30 %.
11. **Keyline gaps vs R1:** stamina dashes, hold bar, threat readout, and the phone's mini USE / Hold prompt glyph have none (code, §2.1, §2.2, §3.9, §4.1).
12. **Keys drawn three ways:** bare Bayon letter in the HUD (`E  ·`), Plex Mono 13 keycap in a 24 px keyline box (settings tabs, page row), 36 px keycaps (key map); the touch kit's origin was "the keycap outline (2 px paper) becomes the USE button and the pause button" (`TOUCH_CONTROLS.md:27`) while the key-icon house rule says "never a keycap box".
13. **Floor notation:** tag `00` / `B1`, stencil `0` / `B1`, room line `LEVEL 0 / FRONT ROOMS`, placard `LEVEL 0`, CAUGHT `LEVEL B2` (§1.3, §6).
14. **Stale docs:** `INFO_TYPE_SYSTEM.md:175` and `GRAPHIC_VISUAL_SPLIT.md:24` still say "HUD numbers (key tag): Courier Prime Bold 25", and `TOUCH_CONTROLS.md:182` lists "Key numeral Courier Prime Bold 25 / 21 / 23"; decided: tag digits Courier Prime **Regular** (DECISIONS #23), and the Bold panel number is gone (`FrontRoomsKeyHud.cs:495`). Settings "cost bars" exist in the Figma kit but the build writes a text line (§4.3).
15. **Tag shape/colour encode nothing now:** floor keys replaced zone keys, but the HUD tag and padlock still take shape + tint from a zone hash (`FrontRoomsKeyHud.cs:233-251`) (§1.2).

### 9.3 States the player must read that have no icon today

| State | Today's representation | Gap |
|---|---|---|
| **Gait: slow / walk / run (desktop)** | **Changed today.** Red 10-30 (Q9/Q10, "电脑版要加慢走，目前整个游戏的系统需要三端保持一致…", then "按住 Ctrl 奔跑"): hold **Shift = SLOW** ("the walk the Relay never hears", key-map note `HOLD · UNHEARD`), hold **Ctrl = SPRINT**, forward only (`HOLD · FORWARD ONLY`); Right Ctrl / Right Shift are locked second keys; a pad has no SLOW button (small tilt = slow) (`INTERACTION_DESIGN.md:178`, `:181`, doc saved 10-30 10:48; input `FrontRoomsInput.SlowHeld` exists in main). Until then desktop had no slow gait and silent walking (`TOUCH_CONTROLS.md:66`, open question 9 `:315`). **Readout: none.** The gait shows only in the key map's words; in play the only sign of running is the stamina dashes appearing (desktop flow D04). **Stale:** the desktop title legend in code still reads `WASD  MOVE    MOUSE  LOOK    SHIFT  SPRINT    E  USE` (`FrontRooms3DGame.cs:197`, T5 spec `50_tutorial_placard.md:240`). | no gait mark on desktop; slow walk now exists on desktop with no mark at all |
| **Gait (touch)** | drawn only by the stick while the thumb is down: slow ring solid 60 % (Slow), run line + thumb yellow and `RUN` (Run); Walk = nothing lit; `SLOW · UNHEARD` is a teaching word that retires after 1.5 s of creep (`TOUCH_CONTROLS.md:76-77`, `:329-334`) | after teaching, Slow has no word; Walk has no positive mark |
| **Gait (pad / keyboard on phone or iPad)** | the stick, USE, RUN and pause hide (`TOUCH_CONTROLS.md:96`); pad tilt < 0.7 = slow; keyboard now Shift = slow (`INTERACTION_DESIGN.md:181`) | INFERENCE: no gait readout at all (the stick's words never show) |
| **How loud am I / who hears me** (slow silent, walk 9 → 12.6 m, run 26 → 36.4 m) | words only: `SLOW · UNHEARD` (touch, teaching), placard line 1 `WALK, DO NOT RUN.` | the placard library's `steps` pictogram (shoe prints heard through a wall) exists but is unused (§5) |
| **Stamina / winded** | 5 yellow dashes (24 × 6 px / 20 × 5 pt), empty = white 25 %, shown only while < full; desktop winded = "all segments dim; no text on desktop"; phone: `WINDED` word + muted stick + the run line refuses the thumb | unlabelled, translucent, no keyline; a playtester "did not understand the stamina bar" (`research/tutorial/01_research.md:40`) |
| **Lights dim = it is near** | placard line 3 `IF THE LIGHTS DIM, LEAVE THE AREA.` (words); the world's lamp dips | `lampDip` pictogram unused |
| **Shut doors help** | placard line 2 `CLOSE DOORS BEHIND YOU.` (words) | no pictogram |
| **Open / shut / take key / glass** | verb only (`E  ·  OPEN DOOR`, `TAKE KEY`, `HOLD E  ·  BREAK GLASS`); phone mini USE / Hold glyph | the dot does not change (by design: grammar Use / Hold rows) |
| **Locked with key vs without** | same padlock; the prompt line differs | no static visual difference before E |
| **Safe zone (stairwell)** | world: steady light + lit ISA rescue sign (blue) | none in the HUD (consistent with "never shows the Relay") |
| **Floor / depth** | text: room line, tag token, stencil, placard header | no depth/floor glyph |
| **Threat readout (assist, off)** | words + numbers `IT 23 M / TIER 2` in yellow / muted | no icon (deliberate: "the player text never says 'relay'", `DISPLAY_SETTINGS.md:50`) |

---

## 10. The game's graphic design language in one page

**Premise (INFERENCE, summarising the docs below).** FrontRooms is a 1990 US office building you are trapped in. Every in-world mark is something that building would print, stamp, engrave or light: plans, plates, stencils, EXIT boxes, forms, typed tags, wallpaper, a CRT. The HUD is a thin non-diegetic layer that borrows the building's paper and ink and speaks mostly in words; its few icons are built out of the crosshair dot.

**Era lock.** "FrontRooms is set in 1990." (`FONTS_PERIOD_1990.md:3`; type kit 1988–93). "Everything dated 1990 or earlier, using real period form grammar (SF 63 rev. 8-81, ANSI Z35.1 headers, AIGA/DOT symbols). No 1991 orange WARNING, no ISO 7010." (`research/print_reference/IMMERSIVE_MATERIALS.md:86`). Pictogram reference = AIGA/DOT 1974–79, "not ISO" (`research/ui_key_icon/01_research.md:279`). EXIT = NFPA 101-1988 / UL 924 (1989) stencil lettering (`research/exit_sign/10_spec.md:49-59`); plates = NYC LL5 1973, OSHA 1910.37, ADAAG 1991 (`research/info_type/STAIR_SIGNS_SPEC.md:23-31`); the placard prints `PRINTED 11/89` (`50_tutorial_placard.md` §15).

**The vernaculars it draws on.**
- **Building-services print:** the evacuation plan — Heros (Helvetica) Bold caps, "black, and white on safety red, on off-white", 17 × 11 in snap frame, poché walls, red route, YOU ARE HERE (`50_tutorial_placard.md:69-73`).
- **Safety signage:** stencil EXIT (6 in letters, ¾ in strokes, red diffuser, knockout chevrons); engraved two-ply plates; 15 in stencil floor tokens; the ISA on accessibility blue (§6).
- **Office forms:** SF 63 grammar; the playtest answer sheet in 12 pt boxes on a 6 pt grid, canary paper (`GRAPHIC_VISUAL_SPLIT.md:43-56`, `:92`).
- **Typed key tags:** a coloured plastic tag with a paper insert typed in Courier — the source of the HUD key and its floor token (`02_design.md:131-138`, `:283-288`).
- **CRT and phosphor:** the terminal raster 640 × 480 (`INFO_TYPE_SYSTEM.md:187`); phosphor ink for glow lines and the placard's photoluminescent underprint (§5, §8).
- **Wallpaper print:** hard-edge WP03 stripes and chevrons whose motion is quantized clicks (§7).
- **1990 TV CG and furniture ads:** the Ad system and the Period1990 faces (`GRAPHIC_VISUAL_SPLIT.md:28-38`).

**Type: two layers.**
- **UI (non-diegetic):** Bayon (labels, prompts, display caps: Title 88, Label 20 / phone 17), IBM Plex Mono (meta 13 / 11, digits, key names, the "·"), Source Serif 4 (room name 50 / 26, captions 20 / 17, explanations). Base = the P1 deck styles (`GRAPHIC_VISUAL_SPLIT.md:13-21`); these "are a separate system and are not changed by" the period kit (`FONTS_PERIOD_1990.md:7`). Touch ramp × 0.85 phone / × 0.95 iPad, 11 pt floor, tracking 0 (`TOUCH_CONTROLS.md:170-188`).
- **World (diegetic):** TeX Gyre Heros = Helvetica, the Swiss/International sans of US building signage and TV CG (`FONTS_PERIOD_1990.md:25`; placard `50_tutorial_placard.md:31`; signs `STAIR_SIGNS_SPEC.md:33-46`); Courier Prime for typed tags (the one face that crosses into the HUD, on the key tag); EXIT is geometry, "not a font" (`exit_sign/10_spec.md:224-250`).

**Colour tokens.** paper `#F4F1E8` (HUD text and marks, and the placard's paper) · dot `#FFFFFF` · keyline `#141414` at 85 % (Unity 0.95) · document ink `#0A0A0A` · reading surface `#0E0E0D` at 97 % · muted `#BDBAB0` (leaving the HUD, #39) · brand / accent yellow `#F4DF3B` (focus, decision; canary paper `#FCF7CE` = the yellow at 25 %) · safety red `#C70F2E` (placard, FAIL chip; EXIT = the sRGB red primary through a diffuser) · zone tints red `#8C0F0D` / blue `#0F2973` / white · phosphor `#D6DEBD` (placard band rev 2 `#DDE5C3`) · accessibility blue `#004A8C` · wash `#EDEBE0`. Rich black C60 M40 Y40 K100 only in InDesign; Figma uses plain RGB black (`GRAPHIC_VISUAL_SPLIT.md:109-110`; `INFO_TYPE_SYSTEM.md:36`).

**The rules, quoted.**
1. "**No dark cards.** Legibility comes from a 1 px ink keyline, #141414 at 85 %" (`INFO_TYPE_SYSTEM.md:179`); "The rule covers backings for text, not controls" (`50_tutorial_placard.md:287`).
2. "**Motion comes out of an element already on screen** (the crosshair dot) and goes back into it. It is never an overlay." (`INFO_TYPE_SYSTEM.md:180`); Red: "利用好鼠标中心的原点，而不是直接覆盖一个动效在上面" (`lock_motion/README.md:73`).
3. "**The crosshair shows what E will do here, now. Nothing else.**" and never anything about the Relay (`30_crosshair_grammar.md:16-19`).
4. "**One glyph, one meaning.**" (`30_crosshair_grammar.md:64`); "**One symbol, one rule.**" (`50_tutorial_placard.md:63`); "one colour keeps one meaning" (`INFO_TYPE_SYSTEM.md:191`); "FrontRooms keeps one yellow per world" (`research/print_reference/FINDINGS.md:183`).
5. "No meaning by hue alone. **Every state is a shape.**" (`30_crosshair_grammar.md:92`).
6. "**No translucent ink or surfaces in in-game UI.**" (`INFO_TYPE_SYSTEM.md:181`); a second line is quieter "only by its face and size, never by a dimmer ink" (`FrontRoomsHudStyle.cs:45-46`, DECISIONS #39).
7. "**Numerals never in Bayon**" (`TOUCH_CONTROLS.md:188`); "Bayon has no `·`; set it in IBM Plex Mono" (`INFO_TYPE_SYSTEM.md:183`).
8. "**Touch words, not new glyphs**" (`EXAMINE_TEXT_G9_G10.md:13`); for the key icon: "Never brass, never yellow, never a keycap box" (`research/ui_key_icon/02_design.md:283`).
9. "**World first.** The HUD keeps to the top band and around the dot. Controls are ghosts until touched" (`TOUCH_CONTROLS.md:31`).
10. Wallpaper: "No new shapes, no type, no glow overlay. If the player doesn't look, it is just wallpaper." (`research/wallpaper_motion/32_pattern_native_hints.md:18`); the walls never glow (Red Q4 = B).
11. Placard: "Never a radar"; the enemy never drawn or named; never `RUN`, `HIDE` or "relay" (`50_tutorial_placard.md:56-74`).
12. Signage: "dark plates (white on black) tell you where you are and where you can go … Pale plates (black on white) say no … Red stays only on EXIT signs and the standpipe." (`STAIR_SIGNS_SPEC.md:62`).
13. Photosafety: nothing on the HUD changes faster than 3 Hz; "Any future 'waiting' signal may only be a hard on/off of a solid element" (`TOUCH_CONTROLS.md:351`; DECISIONS #39).
14. Research: "Research leads with real sources (original photos, clips, footage) … diagrams only support it." (`GRAPHIC_VISUAL_SPLIT.md:117`).
15. Platforms: desktop is the visual reference, WebGL a reduced tier; since Red's 10-30 decision the game's systems and controls behave the same on desktop, iPhone and iPad (`INTERACTION_DESIGN.md:181`).

**What this means for an icon system (INFERENCE).** The game's default is a word. Icons exist only where a word fails at a glance: the four crosshair states, the touch legend, the pause sign, and the placard. They split into two families with different construction rules. HUD icons are paper with a 1 px `#141414` keyline, built from the dot or another element already on screen, with strokes near the neighbouring Bayon stem (≈ 2 px at 20 px). World icons look printed, stamped or engraved in 1990: Heros-weight black, safety red and paper, AIGA/DOT construction, ≥ 0.6 mm at placard scale. The biggest gaps are the movement and stamina states (§9.3); the biggest inconsistencies are the three padlocks, the many uses of one yellow, and the unrelated arrows (§9.2).
