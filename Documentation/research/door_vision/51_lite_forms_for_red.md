# Door lite forms: what the window in a 1990 door can be

*游戏视觉, account B (session local_a9a1dce2), 2026-10-30. For viewing only. Nothing here changes the game, and nothing lands until you pick. Everything was built and rendered in the clone `W/proj_doorvision`. Research, rules and sources: `50_lite_forms.md`.*

Your ask (10-30): "目前在不同门+窗户的样式上，我需要你做research 思考符合那个时代要求的前提下，门上的开窗可以是什么形式的". Earlier (10-03): doors differ by level / zone (lobby, office, run, the real final exit).

**DECIDED, Red 2026-10-30 17:3x, in the 游戏视觉 chat: "A 按区域四种".** Set A: lobby F4b 10 x 48, office F1 10 x 30, run F5 15 x 15 (6 screws), exit F13 4 x 25. The exit keeps its slot, so `30_design` §1.1 (no lite on the C22 door) is reversed; the lobby lite stays centred on its leaf.

## 0. The one question

| Question | Options | Recommendation | Blocks |
|---|---|---|---|
| **Which set of door windows?** | **A** one form per zone: lobby 10 x 48, office 10 x 30, run 15 x 15, exit 4 x 25. **B** today's 10 x 30 on every double-acting door, nothing new. **C** set A plus the two extras: a porthole on kitchen-type doors, a louvre under the lite on janitor doors. | **A** | The form list of the door-vision package. Nothing else waits on it. |

Reply with a letter. These two are already inside A, so say so only if you want them changed:
- **The exit slot reverses an earlier call.** `30_design` gave the C22 stair door no window. A gives it a 4 x 25 slot. If you want it to stay blind, say "A, exit no window".
- **The lobby lite stays centred on its leaf**, as the prototype already is, not moved toward the hinge. The title pair has no break code, so nothing forces it.

**How each option plays:**
- **A:** you can tell which part of the building you are in by the shape of the window, and each shape shows the Relay differently. Lobby: knees up. Office: head and torso. Run: torso, no head. Exit: a slit of light.
- **B:** every door reads the same. The run's "lit square at the end of a dark corridor" and the exit's slot are lost. It costs nothing, and the peek stays as tested.
- **C:** two landmark doors, at most one per floor. It costs the most by far: both extras are well over the triangle budget, and the porthole needs a new sight test.

**Sheets (Shot format):**
- `W/door_lite_forms/sheet/door_lite_forms_compare.jpg`: 14 columns, one per form, recommended set first. Rows: walnut 3 m, steel 3 m, walnut 1 m, the peek with the Relay 3 m behind the glass.
- `W/door_lite_forms/sheet/door_lite_forms_zones.jpg`: set A by zone. Rows: 3 m, then 1.5 m with the Relay 1 m behind, then the peek (or 1 m where a door has no peek).
- Raw 1920 x 1080 frames and `renders.tsv`: `W/proj_doorvision/Verification/door_lite_forms/`.

## 1. The rule that decides everything

- **The law:** a double-acting door needs a window of at least 200 sq in per leaf, with its bottom no higher than 48 in (Cal. Title 8 §3235(i), from the 1979 UBC, https://www.dir.ca.gov/title8/3235.html). That removes most catalogue sizes: 4 x 25, 5 x 20, 10 x 10, 12 x 12, 5 x 35, 6 x 27, the 9 x 14 kitchen window, and 8 in and 10 in rounds.
- **Our door break:** the leaf's lock end must stay solid, so on a breakable leaf the window can be at most 15 in wide. Half and full glass are out.
- **What is left** is a short, real menu. Every form below has its bottom at 48 in or lower unless the table says otherwise.

## 2. Each form as you see it

Tris = triangles added at LOD0 against the same leaf with no lite, walnut / steel. Budget: walnut +400, steel +650. Peek = share of the shot's width that is glass.

### The recommended set

| Form | What you see | Period evidence | Gameplay read | Tris |
|---|---|---|---|---|
| **F1 10 x 30**, 300 sq in, office | A tall narrow window at face height. Today's lite. | The standard corridor lite; the size the period rule and the hardware pass both reach (`10_period` §2). | Head and torso of the Relay. Peek 91 %. Tested. | +208 / +568 |
| **F4b 10 x 48**, 480 sq in, lobby | The same window stretched up the 2.64 m leaf: same width, same 45 in bottom, one family. | Architects scaled lites with the leaf. No catalogue page for this exact size [E]. | The Relay from the knees up, which teaches early that it can be seen through glass. No peek on the title pair. | +0 against today's title lite (same mesh, one edge moved; not measured) |
| **F5 15 x 15**, 225 sq in, run | A low square, below your eye, like a hospital service door. | Eliason traffic doors list 14 x 16 as standard and 15 x 20 as an option, so squares over 200 were catalogue items (undated listings, `50` L7, L8). | You look slightly down into the next room. Torso only, the head cut off by the rail: more dread, less information. Peek 100 %. | +208 / +704; Ward leaf +720 |
| **F13 4 x 25**, 100 sq in, exit | A thin wired slot. The only lite under 200 sq in in the game. | The fire-door default ("many plans default to 4 x 25 or 5 x 20", iDigHardware). Legal only because a stair door is rated and single-acting; NFPA 80 caps it at 100 sq in. | The shape alone says "stair, way out". No peek planned: 37 %, mostly frame. | +192 / +568 on a test leaf; the real C22 door not measured |

F5 on steel is 54 tris over budget, and on the Ward leaf 70 over. Going from 8 screws to 6 brings them to +568 / +584, which I would do.

### The alternates (option C)

| Form | What you see | Period evidence | Gameplay read | Tris |
|---|---|---|---|---|
| **F7 porthole**, 16 in clear, 201 sq in | A big low round window with a metal ring. Steel leaf is the real one; on walnut the ring is brass. | The kitchen and pantry door. The Eliason rounds were 8 and 10 in, which fail the 200 rule; steel kits came in 12, 18 and 24 in (seen in a search summary only, `50` L11). | The strongest landmark. Eyes at the rim. Peek 100 %. Needs a circle sight test. | **+1,296 / +1,826**, LOD1 and LOD2 also over |
| **F12 10 x 30 + louvre**, 300 sq in | Today's lite with a vent panel low down. A steel door form. | 20 ga steel louvres and wood louvres "of matching specie" (San Diego 08150, February 1992, `50` L2). | No view, but light and sound pass: a low "something is there" cue. Peek as F1. Louder steps would be 声音设计's call. | **+580 / +1,316**, LOD1 also over |

### Not chosen

| Form | What you see | Why not | Peek | Tris |
|---|---|---|---|---|
| **F2 8 x 25**, 200 | A shorter office lite. | Legal at exactly 200, but too close to F1 to read as a different zone. | 73 % | +208 / +568 |
| **F6 12 x 20**, 240 | A tall square. | The 15 x 15 reads better as "square". Fills the shot, and the eye stays at 1.62 m. | 100 % | +208 / +704 |
| **F3 6 x 34**, 204 | A narrow slit, longer than the exit's. | Peering through a crack: the frame takes 45 % of the shot. No stock kit seen at this size [E]. | 55 % | +208 / +568 |
| **F4 6 x 60**, 360 | A slot running nearly floor to top. | The same crack, weak for 1990 (reads 1960s or post-2000), and it hits the Ward leaf's armour plate. | 55 % | +224 / +704 |

### Rejected

| Form | What you see | Why not | Peek | Tris |
|---|---|---|---|---|
| **F10 twin 6 x 34**, 204 each | Two slits side by side. | No period double-acting evidence. The bar between them reads as a cell block. | 55 % | +416 / +1,136 |
| **F11 stacked 8 x 25**, 200 each | One window above another. | The upper lite's bottom is 50 in, so it is legal only on a loose reading. No period evidence, and it hits the Ward armour. | 73 % | +400 / +1,136 |
| **F8 half glass** | The top half of the door is window. | Crosses the break window by 278 mm. Shows the room from anywhere. Wired half glass reads institutional, not lobby. | 100 % | +208 / +840 |
| **F9 full glass** | Almost the whole leaf is window. | As F8. There is nothing left to peek at. | 100 % | +224 / +976 |

F8 and F9 are drawn on the map leaf here (745 and 1,623 sq in). The 1,499 and 2,548 in `50` are their title-leaf sizes.

**Draws for every form:** +1 glass draw per lited leaf, +0 opaque. Two-lite forms share the one glass slot. These come from the kit's slots; GPU counters cannot be read in batch mode.

## 3. What the render round found

- **The peek needs a small change for low windows, and `50` had this wrong.**
  - `50` said the camera sits at 1.38 m on the 15 x 15. In the code the peek keeps your eye height, so it stayed at 1.61 m, just above the top of the glass, and only 27 % of the shot's height was glass.
  - In the clone the eye now holds half a shot inside the glass: 1.50 m on F5, 1.52 m on F7. Every other form keeps 1.62 m, and today's lite behaves exactly as before.
  - Any form below the eye needs this. Owner: 交互设计 with 游戏视觉.
- **A Relay in view blocks the peek today, on the tall forms only.** In the first run, with the Relay already 3 m behind the glass, the prompt never came on F1–F4, and E opened the door instead. On the low forms F5–F7 the peek opened, because the Relay was not on screen from 0.8 m. This is the old threat gate, which your C36 removes, so it goes when the peek lands. For these frames the Relay is placed after the peek opens.
- **One shadow shape per form.** The shadow proxy holds one shared mesh with today's hole. Each extra form needs one more shared mesh, about 32 tris, no extra caster. In these renders the leaf casts its own shadow, so the light through the window is correct.
- **The lobby pair needs a 2.9 m room.** It does not fit the 2.4 m rooms, so its frames come from a Standard zone at another spot in the same map. Its light is similar but not identical.

## 4. Set A, by zone

| Zone | Doors | Form | Frame | What changes in play |
|---|---|---|---|---|
| Level 0 lobby | title pair, station entrance skin | F4b 10 x 48, centred | walnut bead | The dark state is just taller; the title has no peek. |
| Office | Veneer_DA walnut | F1 10 x 30 | walnut bead | Nothing: as built and tested. |
| Run | Ward_DA, Steel_DA | F5 15 x 15, low | pressed steel kit | Torso only; peek eye at 1.50 m. |
| Final exit | C22 steel EXIT door | F13 4 x 25 | 18 ga rated steel kit | A slit of the stairwell's steady light. No peek. |

**Before the exit slot lands:** the C22 door's own model has to be checked, and 信息设计's stair plates (Figma 3545:7368). The exit tile on the sheet is the slot on a test door in this room, so the stairwell's light is not behind it. The bright 4 in line is expected from the design, not seen in a render.

**Run doors need one thing from the map.** Today `DoorMemberFor` returns only Lobby or Office, so no map door is a Run door until 关卡设计's hook M8 lands. Until then F5 would not appear in a real run. The harness forces the Run row for these frames.

**Work set A adds:**
- the per-form generator (done in the clone) and a second Steel_DA kit, because the locked Run row takes F5 while the Lobby and Office rows keep F1;
- the peek eye band and the shadow meshes;
- the lobby top edge;
- the exit slot on the C22 model.

The map's sight rule already reads each lite's own rectangle, so set A needs no new code there.

## 5. How this was rendered

- **Same setup for all:** Game camera, FOV 76, seed 516574485, High quality, RT glass off, the Relay held. One map door re-dressed per form (door (95,-69), both rooms 12.3 m deep, lamps lit). Peeks are the real peek with E, 28 of 28 opened, the pull-back held off (C36).
- **Light:** the left wall reads 0.149–0.163 at 3 m across the forms. That 9 % spread is the game's own lamp variation (lamps 0.96–1.00).
- **Checks:** every break-safe form clears the break window by ≥ 50 mm and the damage mask by ≥ 45 mm, checked by the generator; F8 and F9 are the exceptions. The sign clears every map-leaf frame by ≥ 48 mm. On the lobby leaf the sign would move to Y 2.519 if that leaf ever carries one.
- **Not done:** no phone or WebGL renders, no frame timing, and no Figma or VL entry (batched daily). The Relay is the current capsule rig. Walnut louvre blades are 9 mm deep, against 20 mm on steel, to stay under the bead. The steel F4b and walnut F13 tiles are for comparison, not doors we would build.
- **Evidence limits:** the Eliason listings are undated. No catalogue page or film frame from 1985–93 was viewed for these exact sizes; the film leads in `50` L16 are still unviewed. The law and the San Diego specs are the firm sources.
