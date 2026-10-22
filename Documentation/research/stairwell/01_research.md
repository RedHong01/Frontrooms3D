# 01 · Stairwell research (C22 exit → one floor deeper)

Workflow `c22-stairwell-look`, stage 1 (research). Written 2026-10-22 by 游戏视觉. Read-only on main except this folder.

**Scope.** Red decided C22 on 2026-10-21 (systems.json 2026-10-21f): each floor has one EXIT. A stairwell behind it leads one floor deeper, and a run still ends only when caught. 关卡设计 builds the map minimum. 游戏视觉 owns the stairwell look and the EXIT sign at the exit door. C23 (lit `Kit_ExitSign` locked to LIT over the exit door, plus `Kit_ExitDevice_Crossbar` on the leaf) is still **open** in systems.json.

**Labels.**
- **[read]**: the primary text was opened and read.
- **[search]**: only a search summary was read.
- **[on disk]**: media or docs already in the project.
- **[memory]**: from general knowledge of the work; verify before quoting a time.
- **[ESTIMATE]**: trade practice, not a code text.
- **[UNVERIFIED]**: stated but not checked.

Source IDs (S1…) are in `SOURCES.md`. No media was downloaded. Wanted media is in `media_candidates.md`.

**Revised 2026-10-22 in the fix stage** (`50_critic_fixes.md`): the alarm condition (§0.1), the two-level stair and 1.12 m flights (§0.2, §0.4, §3.3–3.5), the Kane timecode and the withdrawn stair claim (§0.10, §2.1), the NPS and ADA wording (§1.1, §1.5), the down-gate wording (§1.1, §3.5) and the arrival-door recommendation (§3.5, §3.6).

---

## 0. The findings that matter for the look

1. **The game's mechanic is a real 1990 code condition.** Stair doors locked on the stair side, with signs saying where re-entry is possible, were legal and common. NBS (1979) describes people "locked inside the stairwell and forced to descend" (S3).
   - NYC Local Law 5 (1973) required re-entry signs inside each stair (S4).
   - UBC 1991 requires stair doors locked from the stair side to unlock from a control station. Its existing high-rise appendix asks for a sign reading `ACCESS ONTO FLOOR THIS LEVEL` (S1).
   - So **"the door locks behind you" and "the only way is down" need almost no fiction.** A `NO RE-ENTRY` sign on the stair side of the exit door explains it.
   - **The one condition (critic era 10):** under UBC §1807(j) locked stair doors in a high-rise must all unlock on a signal from the central control station, and NFPA 101-1988 asks for re-entry or automatic release [memory]. EGRESS makes the Relay the fire-alarm responder, so a code-true 1990 stair would unlock during an alarm. The fiction needs "no alarm signal reaches the doors" or "the control station is dead" (a flag for 叙事).
2. **A code-true 1990 switchback stair fits exactly into 1 × 2 map cells** (3 × 6 m on centre lines, 2.84 × 5.84 m clear).
   - Floor to floor 3.50 m: 20 risers of 175 mm and 2 flights of 9 treads × 280 mm. That meets UBC 1991's 7 in / 11 in.
   - Flights 1.12 m wide (44 in, the minimum) with a 0.60 m well (fix stage; stage 2 had 1.22 m and 0.40 m).
   - Landings 1.40–1.66 m deep.
   - Both doors end up in the same end wall, one directly above the other (§3.3).
3. **Map doors are centred on a 3 m cell edge, so a stair door can only sit on the landing's end wall.** A door in the long wall would open over the steps, which UBC 3304(i)/(j) forbids.
   - An end-wall door opens straight onto the guard and the well. The first view in is down to the far landing.
4. **Build each stair for two levels: the exit floor and the far floor** (fix stage; stage 2 proposed an endlessly repeating shaft).
   - A slab closes the shaft over the exit landing and the far landing is the shaft floor, as 叙事's stair ID string says (`LEVELS 0 TO B1`, `NO ROOF ACCESS`). No up flight, no gate, nothing in the well below.
   - The two landings are identical except their floor sign, so the game can still rebase or descend for real (§3.4).
   - The floor sign carries the progress. The Exit 8 counter sign shows the device (S31).
5. **The period door kit already exists in main.** `Kit_DoorLeaf_Steel` (no lite), `Kit_DoorFrame_Steel`, `Kit_DoorCloser_*`, `Kit_ExitDevice_Crossbar` (bar at 1.0 m, inside UBC's 30–44 in), and `Kit_ExitSign` (6 in letters, 3/4 in stroke).
   - **Use the no-lite leaf on the exit door.** Through a lite the player would watch the old map being released.
   - The C23 lit sign over the door fits Standard zones (2.9 m). In Low zones (2.4 m) it ends at 2.392 m, which breaks the 0.05 m ceiling clearance.
   - **Ask 关卡设计 to place the exit on a Standard-zone wall** (§3.2).
6. **The descent covers the build.** The title handoff holds the far door until `ReadyAround`, at most 3 s, measured ≤ 0.3 s.
   - Two flights (3.07 m each along the slope) and the turn landing are about 8–9 m of path. That takes ≈ 2.7 s at `Walk` 3.2 m/s and ≈ 1.6 s at `Run` 5.5 m/s (`FrontRooms3DGame.cs:200`).
   - The measured ≤ 0.3 s build fits easily. The 3 s worst case could still hold the far door for a moment.
   - Real stair doors stick under stack pressure (§1.7). That gives a believable "heavy door" beat for such a hold.
7. **The period look is plain and specific.**
   - Painted 8 in concrete block in two tones, with a darker dado band that follows the stair slope. BSS 120 Fig. 2.7.5 calls this a "reliable cue" for the first riser (S3).
   - Grey steel pan stairs with concrete-filled treads, pipe handrails worn to bare metal on top, and a red Class I standpipe valve on every landing (UBC 3805(c)).
   - Big stencilled floor numbers. A twin-head battery unit at each landing (S12).
   - **No** photoluminescent stripes, no tactile/Braille signs, and no ISO running man: all are later or foreign.
8. **Light, cues and photosafety.**
   - Exits must be lit at ≥ 1 fc (10.8 lx) at the floor whenever the building is occupied (UBC 3313). A dark stairwell is a code violation, which agrees with "darkness is never cover".
   - Real stair lamps fail one at a time. (BSS 120's "83 % of stairs below 10 fc" is a 1970s residential sample and does not back a dim office stair; the fix stage stopped using it.) The renders hold the walk path at about one stop under the maze.
   - Any lamp change must stay ≤ 3 changes/s and follow Reduce Flashing.
9. **Two cross-line flags.**
   - (a) **Sound.** Red's 10-03 rulings (no panic-bar sound, no closer layers) were about the wooden map doors. This steel stair door is the one door where crossbar, closer and latch sounds are real (freesound 341176 is a real steel stairwell door). Ask 声音设计, without re-asking C22.
   - (b) **Strings.** Floor numbering for "deeper" (12, 11, 10… vs B1, B2…), the `NO RE-ENTRY` wording and the stair letter are strings. They belong to 叙事 and 平面视觉. I give only the period facts.
10. **Canon gives the device, not a design.**
    - Kane Pixels ties every change of look to something you can name. In *Found Footage* (368–416 s, the ledger's times; frames on disk), a grey back-of-house of corridors and **push-bar doors** ends in a door that hard-cuts back to Level 0. **The frames on disk show no stair** (critic era 2); a stair in that sequence is unconfirmed until the clip is approved and checked.
    - Fandom Level 1 is entered "by walking up a flight of stairs". Fandom Level 4 has "stairwells that loop back to the same floor".
    - FrontRooms' own twist: every stair goes one floor deeper and never back. Do not copy SCP-087's 13-step semicircular landings, Level !'s red chevron corridor or Exit 8's tiled subway.

---

## 1. The 1990 US egress stair (offices, hotels, back of house)

### 1.1 Code dimensions of the period

The UBC 1991 is the binding text read here (S1, CC0 scan, Chapter 33 pp. 632–646). The 1991 edition was in force from 1991 and was written in the 1988–90 cycle, so it falls inside the era lock (≤ 1993). Values in the other columns are marked by how they were checked.

| Item | UBC 1991 [read] | Metric | Earlier UBC / other codes | Notes for us |
|---|---|---|---|---|
| Riser | 4 in min, **7 in max** (§3306(c)) | 102–178 mm | Older UBC wording: max 7½ in, run min 10 in (California Title 24 text that follows the older UBC [search], S9). NFPA 101-1988 new stairs: 7 in / 11 in [memory, UNVERIFIED]. BOCA: 7/11 for residential only from 1992; commercial values for 1990 not read [search] (S10) | 175 mm in our proposal |
| Run (tread) | **11 in min**, measured nosing to nosing (§3306(c)) | 279 mm | — | 280 mm |
| Uniformity | largest run vs smallest, and largest riser vs smallest: ≤ 3/8 in within a flight | 9.5 mm | NBS survey (residential): 46 % of stairs had ≥ 1 in variation (S3, Table 8) | A "wrong" step is a period-true unease device. Keep it visual only; the player capsule must not trip |
| Width | **44 in min**; 36 in when occupant load ≤ 49 (§3306(b)) | 1,118 / 914 mm | — | 1.22 m flights |
| Projections into the width | handrails 3½ in each side; stringers and trim 1½ in (§3306(b)) | 89 / 38 mm | — | — |
| Landing depth | ≥ stair width; need not exceed 44 in on a straight run (§3306(g)) | ≥ 1,118 mm | — | 1.40–1.66 m |
| Rise between landings | **≤ 12 ft** (§3306(g)) | 3,658 mm | — | one flight = 1.75 m |
| Landings at doors | width ≥ stair or door, whichever is greater. Length ≥ 44 in in the direction of travel. A fully open door may not reduce a required dimension by more than 7 in (OCR damaged, wording reconstructed). With ≥ 50 occupants, a door in any position may not cut the landing below half its width (§3304(j)) | ≥ 1,118 mm; 178 mm | — | the leaf must clear the landing when open |
| Floor at doors | a floor or landing on each side of every door, level (§3304(i)). **A door may not swing over a step** (implied for commercial; explicit only in the R-3 exception) | — | — | end-wall doors only (§3.3) |
| Headroom | **6 ft 8 in**, measured vertically from the nosing line to the soffit (§3306(o)) | 2,032 mm | — | soffit = the underside of the flight above |
| Handrails | both sides (one if < 44 in wide). Top **34–38 in** above the nosings. Continuous. At least one extends **12 in** beyond the top riser and 12 in beyond the bottom riser. Ends returned or on newels. Grip **1½–2 in**. **1½ in** clear of the wall. Intermediate rail per 88 in of width (§3306(i)) | 864–965 mm; 305 mm; 38–51 mm; 38 mm | NBS residential survey: mean rail height 31 in (SD 5.0), so many older rails were low (S3) | 1.660 in OD (42 mm) is **1¼ in NPS**; 1½ in NPS is 1.900 in OD (48 mm). Both are inside the grip (corrected in the fix stage). The renders: A 42 mm, B and C 48 mm, all ends returned to the wall on flanges |
| Guards | open sides more than 30 in above the floor: **42 in** guard; along flights the guard may be at handrail height. Infill: a **4 in sphere** may not pass. Commercial/industrial areas not open to the public: **12 in sphere**. The triangle at riser/tread: 6 in (§1712(a)) | 1,067 mm; 102 / 305 / 152 mm | — | the floor's one EXIT stair is open to the public, so the 12 in exception does not apply: every guard closes under 4 in (pickets, mesh or balusters) |
| Doors | at least 3 ft × 6 ft 8 in door, 32 in clear; leaf ≤ 4 ft (§3304(f)(g)) | 914 × 2,032; 813; 1,219 mm | trade standard leaf 3070 = 36 × 84 in (0.914 × 2.134 m), SDI 111 (LEVEL_MODULE_SPEC §3) | our map leaf 0.98 × 2.08 |
| Swing | in the direction of exit travel for ≥ 50 occupants or hazardous areas. Latch releases at 15 lbf, door starts at 30 lbf, opens fully at 15 lbf. No key or special knowledge from the inside (§3304(b)(c)) | 67 / 133 N | — | the player pushes into the stair from the floor |
| Panic hardware | when fitted: UL 305 (UBC Std 33-4). Bar **30–44 in** above the floor. Unlatches at ≤ 15 lbf (§3304(d)) | 762–1,118 mm | — | `Kit_ExitDevice_Crossbar` at 1.000 m ✓ |
| Enclosure | **2-hour** walls in buildings of 4+ stories (or Types I/II), 1-hour elsewhere (§3309(b)) | — | — | 8 in CMU is the usual 2-hour wall [ESTIMATE] |
| Stair doors | **1½-hour** in a 2-hour shaft, 1-hour in a 1-hour shaft. Self-closing, or automatic-closing by smoke detector. ≤ 450 °F rise on the unexposed face at 30 min (§3309(c)) | — | — | a "temperature-rise" label |
| Openings into the shaft | only for exiting. Penetrations only for stair pressurisation, sprinkler pipe, standpipe, and conduit serving the stair, ending in a box ≤ 16 in² (§3309(c)) | ≤ 103 cm² | — | **no windows, no vents, no borrowed lights**: the shaft is blind, which suits the glass rule |
| Fire-door hardware | a closing device on every fire assembly. ≥ 2 hinges, plus one per extra 30 in over 60 in, so **3 hinges on a 7 ft leaf**. Ball-bearing hinges. Automatic latch (§4306(f)) | — | — | 3 hinges, visible closer |
| Glazing in fire doors | none in 3-hour doors. In 1½-hour and 1-hour doors: **≤ 100 in²** per leaf, wired glass (§4306(g), OCR damaged) | ≤ 645 cm² | `Kit_DoorLeaf_SteelLite` glass slab 0.119 × 0.652 m = 0.0776 m² ≈ **120 in² ✗**. That is over the limit for a 1-hour or 1½-hour stair door, though fine for ¾-hour doors (≤ 1,296 in², §4306(g)). A stair-door lite would need about 0.10 × 0.645 m | no lite on the exit door. On the far door only if resized (§3.5) |
| Under the stair | no enclosed usable space, and the open space under it may not be used for anything (§3309(f)) | — | — | commonly broken in practice: storage on landings [ESTIMATE]. A period-true "violation" pile |
| Basement barrier | the stair may not continue below the grade-level exit without an approved barrier (§3309(e), §3306(h)) | — | — | in code it marks the street floor. Our stair ends at the far floor, so no barrier is built; if a stair ever continued below, a gate there would be a **borrowed** discharge-level device, not the real-world one (§3.5) |
| Roof | in 4+ story buildings one stair goes to the roof, or the top of the shaft has a 16 ft² hatch (§3306(l)(n)) | 1.49 m² | — | sign content "ROOF ACCESS / NO ROOF ACCESS" |
| Standpipe | a Class I outlet at **every floor-level landing** of every required stairway, where standpipes are required (§3805(c), Table 38-A) | — | — | the red riser and valve repeat each floor: a strong vertical rhythm |

**ADA marker** (reworded in the fix stage, critic era 12). The ADA became law in July 1990, ADAAG followed in July 1991, and Title III covers new buildings first occupied after 26 January 1993, inside our ≤ 1993 lock. Tactile and raised signs and 12 in + one tread rail extensions were already in ANSI A117.1-1980 and UFAS (1984), and some states adopted them in the 1980s [memory, verify against UFAS 4.9.4 and 4.30]. So: **rare in private offices before 1992; we show a pre-1992 private building**, with no tactile signs. Our wall rails still carry the 12 in extensions, which UBC 1991 §3306(i) asks for anyway.

### 1.2 Construction

- **Enclosure.** Painted concrete masonry units (CMU, nominal 8 × 8 × 16 in, so the course is 8 in = 203 mm), or cast concrete in towers [ESTIMATE].
  - Joints are struck flush or concave. On the stair side they show under paint.
  - The shaft has no windows (§3309(c)). Light comes only from fixtures.
- **Stair types** [ESTIMATE; trade practice; S11 for the pan detail].
  - **(a) Steel pan stair**, the default in 1980s–90s offices and hotels. Steel channel stringers carry formed sheet-steel pans, filled on site with about 1½–2 in of concrete. Risers are closed steel plate. The landing is a pan-and-concrete platform on steel.
    - Nosing: the pan's turned edge, or a cast abrasive or steel angle nosing.
    - The underside (soffit) is exposed: stringer channels plus the corrugated or flat pan bottoms, painted.
    - Footsteps sound slightly hollow.
  - **(b) Cast-in-place concrete stair.** Monolithic flights with a sloped soffit. Common in parking garages and concrete towers. Dead, heavy sound.
  - **(c) Precast** flights in some 1980s towers.
  - For a 1990 suburban office or hotel, **(a)** is the likeliest. It also gives the most readable silhouette.
- **Well.** Switchback (U-return) stairs leave a gap between the two flights' guards. It is narrow in towers (100–300 mm) and wider in older buildings [ESTIMATE].
  - Looking down it you see guard after guard receding. This is the Vertigo image (§2.2).
- **Handrails and guards.** Steel pipe or tube, welded or with flanges. The guard is on the well side, the wall rail on brackets.
  - Office public stairs: vertical pickets (4 in rule). Service and back-of-house stairs: top rail plus mid-rail(s) (12 in rule) (§1712).
- **Services on the walls.** The standpipe riser (red), sprinklers (in a noncombustible shaft NFPA 13-1989 asks for heads only at the top and under the first landing above the bottom [memory, UNVERIFIED]; the renders put one head at the top), EMT conduit to the fixtures and the emergency unit, and a junction box ≤ 16 in² (§3309(c)).
  - These are the only "objects" a real shaft holds.

### 1.3 Finishes and wear (period, US)

[ESTIMATE unless cited. These are trade and observation norms. Check against media once approved.]
- **Walls.** Two-tone paint on CMU: a darker band to about 1.0–1.2 m, lighter above.
  - In stairs the band often **follows the stair slope**. BSS 120 Fig. 2.7.5 shows "the slope of the handrail and the paint pattern on the wall" as reliable cues for the first riser (S3, p. 110).
  - Colours of the era: institutional off-white, beige, putty and light grey above; grey, tan, brown or muted green below. Gloss or semi-gloss enamel at the band, eggshell above.
- **Treads.** Concrete fill, sealed or painted grey (porch-and-floor enamel). In back-of-house: painted safety-yellow nosing stripes (yellow = caution, OSHA 1910.144, S13 via `room_visuals/SOURCES.md`). Or bare concrete.
  - Office stairs sometimes had rubber treads with integral nosings. These are dark, worn grey-black and lift at the nosing (BSS 120 Figs. 2.1.3a/b show delaminated tread mats, S3).
- **Steel.** Stringers, pans, rails and doors in the same enamel, often grey, tan or brown. Red-oxide primer shows at chips.
- **Wear map.**
  - Black heel scuffs on the risers and lower walls.
  - Treads worn in the centre and paint chipped at the nosings.
  - Rail tops polished to bare steel along the descent hand-path, worst at the turns.
  - Grime halo round the crossbar, push-side kick plate scuffed.
  - Dust in the landing corners and on top of the closer.
  - Rust bloom at the pan nosings and the standpipe valve.
  - Paint runs on the CMU, and stencil overspray round the floor numbers.
- **Life.** Cigarette butts and a coffee-can ashtray on a landing. Smoking moved into stairwells as office smoking limits spread in the late 1980s [memory]. Also wedged-open doors, stored chairs and boxes against §3309(f), and graffiti in hotel service stairs.
  - EGRESS already has "the other occupant". A fresh butt is a narrative hook for 叙事, not a cue.

### 1.4 The stair door

- **Leaf and frame.** 1¾ in hollow-metal leaf in a welded or knock-down hollow-metal frame, painted. Three ball-bearing hinges. A **fire label** (metal tag) on the hinge edge of the leaf and of the frame. "FIRE DOOR · KEEP CLOSED" signs were common on the floor side [ESTIMATE wording].
- **Vision lite.** Optional, ≤ 100 in² of wired glass (§4306(g)). Wired glass was the period fire glazing (16 CFR 1201.1 exempts it, S14 via `interactables/02_period_hardware.md`).
- **Floor side (push side, the way in).** Crossbar or touch-bar panic hardware where required (UL 305, bar 30–44 in), or a lever. Kick plate. EXIT sign over the frame.
  - Our `Kit_ExitDevice_Crossbar` is the rim crossbar of the time. Panic hardware goes back to the 1900s (S12; `room_visuals/03_run_research.md` §5).
- **Stair side (pull side).**
  - Either a lever or pull trim, when re-entry is allowed. Or **no trim at all / a dummy plate** when the door is locked from the stair side. On a locked door, the stair side is a blank steel face.
  - Surface **door closer** at the head: a regular arm on the pull side, or a parallel arm on the push side [ESTIMATE].
  - Floor-number sign (NYC LL5: ≥ 3 in bold numerals, on the stair side of the door, S4).
  - Re-entry sign.
- **Self-closing is the law.** UBC 3309(c) says "Doors shall be maintained self-closing". So when the exit door swings shut by itself and latches, that is the closer, not magic.
  - The latch is automatic (§4306(f)3).
  - This is the honest explanation for `CloseTerminalDoor`.

### 1.5 Signs of the period (wording facts only; type and strings belong to 平面视觉 and 叙事)

| Sign | Where | Rule of the time | Source |
|---|---|---|---|
| EXIT | over stair enclosure doors on each floor, wherever two exits are required | block letters 6 in, stroke ≥ 3/4 in. Two lamps (one on battery or generator) or self-luminous. Lettering, arrows and symbols in high contrast with the background | UBC 3314 [read] S1; NFPA 101-1988 §5-10 and UL 924 (1989) via NISTIR 4399 [read] S5 |
| Stair identification | **inside** enclosed stairs, at each floor landing, buildings ≥ 4 stories | about **5 ft above the landing**, readable with the door **open or closed**. Shows the stair ID, roof access or not, the floor level, and the upper and lower terminus. UBC Std 33-2 | UBC 3306(p) [read] S1. NFPA 101 version for 5+ stories [memory, UNVERIFIED] |
| Floor number | inside each stair on every floor | numerals ≥ **3 in**, bold, contrasting, "securely attached to the stair side of the door" | NYC LL5 1973, C26-608.3 [read] S4 |
| Stair letter | occupancy side of the stair door, and elevator landings | a letter ≥ 3 in, bold, contrasting | NYC LL5 C26-608.4 [read] S4 |
| Re-entry | on the stair door at each floor, and inside the stair | "whether reentry is provided into the building and the floor where such reentry is provided" | NYC LL5 C26-608.5 [read] S4 |
| Access floor | stair side of doors left unlocked (at least every 5th floor) | `ACCESS ONTO FLOOR THIS LEVEL` | UBC 1991 Appendix Ch. 1 §125 item 11 [read] S1 |
| Use stairs | elevator landings | "IN CASE OF FIRE, USE STAIRS UNLESS OTHERWISE INSTRUCTED", red letters ≥ ½ in on white, with a diagram | NYC LL5 [read] S4 (already used by `research/placard`) |
| Low-level exit signs | **hotels only**: exit corridors serving guest rooms | sign bottom **6–8 in** above the floor, within 4 in of the door frame | UBC 3314(e) [read] S1 |
| `NO RE-ENTRY` | stair side of locked doors | common trade wording on stock signs [ESTIMATE]; the code asks only that re-entry be signed | — |
| Tactile, Braille, photoluminescent | — | **rare in private buildings before 1992** (ADAAG July 1991; earlier ANSI A117.1-1980 / UFAS 1984 [memory]). Photoluminescent stair markings followed NYC Local Law 26 of 2004 [memory] | do not use |

Common 1990 forms [ESTIMATE]:
- a stencilled painted numeral, 12–24 in high, on the CMU beside the door;
- a small engraved phenolic or painted steel plate (stair letter, floor, terminus);
- a red-on-white or white-on-red EXIT box sign with a stencil face, as in `Kit_ExitSign`.

### 1.6 Light in a 1990 stair

- **Required level.** ≥ 1 fc (10.8 lx) at floor level whenever the building is occupied (UBC 3313(a), S1). Emergency source (batteries or generator) where the exit system serves ≥ 100 people (§3313(b)). High-rise emergency systems start within **10 s** (§1807(i)3). High-rise stair doors locked on the stair side must unlock together on a signal from the central control station, with a phone at least every fifth floor (§1807(j)).
- **Current NFPA 101 emergency lighting** gives 1 fc average and 0.1 fc minimum, 90 min, ≤ 40:1 max/min. The 1990 wording is UNVERIFIED (S12).
- **Fixtures** [ESTIMATE].
  - Normal: a surface 2-lamp fluorescent wraparound on the wall or ceiling at each landing and mid-landing, or a guarded incandescent or vapour-tight globe in older and service stairs.
  - Emergency: a **twin-head battery unit** (lead-calcium or nickel-cadmium battery, two sealed-beam heads, red AC pilot) high on the wall near the door (S12; our hero `Kit_EmergencyTwinHead` exists clone-only, `room_visuals/11_run_dir_emergency_power.md`).
  - EXIT signs of the time were incandescent (two 20–25 W lamps), fluorescent, or tritium (S5).
- **What it looks like.** Hard local pools at the landings, dimmer mid-flights, and the soffit of the flight above lit from below.
  - The well below falls off with distance because each landing's light reaches only its own flights.
  - NBS (S3, residential): 83 % of stairs below 10 fc, and 26 % with glare (≥ 20 fc difference between tread and wall).
  - BSS 120 §2.4.8: "Any change in the amount, color or direction of the illumination may lead the frequent stair user to pay attention only to the change". **A per-floor lighting difference is therefore read strongly. Use it sparingly and steadily.**

### 1.7 Smells and sounds a level designer can use

| Sense | What a real 1990 stair gives | Use in FrontRooms |
|---|---|---|
| Reverb | Hard parallel CMU and concrete with steel: a long, bright tail and flutter echo between the walls [ESTIMATE; RT60 not measured] | Footsteps come back a beat later. **Keep the echo clearly the player's own** (same cadence, short delay) so it never imitates the Relay's W2 footstep warning |
| Footsteps | Pan stairs: a slightly hollow tap. Concrete: a dead thud. Rubber treads: a squeak | The material change marks the zone change. Sound owner: 声音设计 |
| Door | Crossbar clack, then latch retract, then the closer's hydraulic sigh, then the latch strike (S26: a real steel stairwell door with panic bar and slam) | The one door where these are real. **Flag to 声音设计**: Red's 10-03 rulings (no panic-bar sound, no closer layers) were about the wooden map doors |
| Pressure | Stack effect and stair pressurisation fans (§3310(b) Exc. 1: 2,500 cfm, 0.15 in w.c.) make stair doors heavy to open, whistle under the leaf and slam [ESTIMATE for behaviour; S1 for the numbers] | A diegetic reason for a heavy door, a door hold, or a draught sound at the far door |
| Hum | The emergency unit's charger and the fluorescent ballasts: a 60/120 Hz buzz | A quiet bed. No flicker above 3 changes/s |
| Distance | Doors slamming on other floors carry up and down the shaft | A narrative hook ("the other occupant", EGRESS) for 叙事. **Not a cue, never directional** |
| Smell (for materials only) | Cold concrete dust, damp at the base, oil enamel, stale cigarette smoke, rubber | Drives grime and damp masks: damper toward the "deeper" floors, dust on horizontal ledges |

### 1.8 Office, hotel and back-of-house differences

| | Office core stair | Hotel guest stair | Back-of-house / service stair |
|---|---|---|---|
| Finish | painted CMU two-tone, steel pan, pickets | same, sometimes carpeted landings near guest floors [ESTIMATE] | bare or painted concrete, two-rail pipe guard, yellow nosings |
| Signs | EXIT, stair letter, floor numbers, re-entry | plus hotel-only low-level exit signs in corridors (6–8 in, UBC 3314(e)); evacuation plan on the guest door | stencilled numbers, `NO SMOKING`, `AUTHORIZED PERSONNEL ONLY` [ESTIMATE] |
| Hardware | crossbar or lever, closer | crossbar, closer, often locked on the stair side (S3: guests getting ice "trapped" into a trip to the sub-basement) | crossbar, closer, kick plate, dents |
| Mood | neutral, institutional | uncanny: you leave carpet and wallpaper for bare block in one step | worn and lived-in: carts, boxes, butts |

For FrontRooms (Level 0 and Office themes): **office core stair**, with back-of-house wear. The step from carpet and wallpaper to bare block is the moment that says "you have left the floor".

---

## 2. References: stairs used for unease, and the stair as a transition

### 2.1 Backrooms canon (stairs as transition)

| Source | What happens | What works | Do not copy |
|---|---|---|---|
| Kane Pixels, *The Backrooms (Found Footage)* (2022-01-07), back of house **368–416 s** (the ledger `room_visuals/SOURCES.md`); the earlier lit wooden stair 4:44–5:56 [read via `level_transitions/01_ip_research.md` §3.1; frames on disk `room_visuals/images/run_research_r05_kane_backofhouse.jpg`, `run_ref_kane_ff_0398_fire_exit.jpg`] | A far lit wooden stair climbs into white rooms (a new look entered by stairs). Later: a grey concrete back-of-house, a sign over a grey door with a **push bar**, and the door opening hard-cuts back to Level 0. **The on-disk frames show corridors and push-bar doors, no stair** (fix stage) | Each change of look is tied to an object you can name (a stair, a fire door). The push-bar door is the cleanest cut in the series | The UK/ISO green running-man signs (not 1990 US). The exact service-tunnel composition |
| Backrooms Fandom, Level 1 [read via `level_transitions/01_ip_research.md` §6.2] | Entered from Level 0 "by walking up a flight of stairs"; left by stairs, hallways or an unlocked door | The vertical move is canon | — |
| Backrooms Fandom, Level 4 "The Abandoned Office" [read via `office_and_film/03_ip_canon.md` §2.2] | "Multiple stairwells that loop back to the same floor"; emergency exits | Office levels own the stairwell | **Ours never loops back**: every stair goes one deeper |
| Backrooms wikidot, Level 4 [read, same] | "Exits are office-style stairwells and occasional elevators" | Same | — |
| *Inside the Backrooms* (2022), 1:51:50–1:52:02 [read via `01_ip_research.md` §7] | Dark corridor, then a stairwell, then the office | The stair as the joint between levels | — |
| *Escape the Backrooms*, Level ! "Run For Your Life!" [on disk: `room_visuals/images/run_ref_etb_levelbang_0094.jpg`] | A ceiling-hung "‹EXIT›" with both chevrons over a red troffer row in a chase corridor | EXIT as the goal of a chase | The red corridor and chevrons. C23's later chevron signs must use our own 1990 grammar (NISTIR 4532 chevrons, S5) |
| *The Exit 8* (Kotake Create, 2023) [on disk `Research/week01/assets/clips/EX8_*`; `01_ip_research.md` §7] | Each loop ends round a corner and down a stair. A numbered exit sign shows how far you are. A 2023 Japanese subway, not a 1990 US reference | **A number on a sign is the whole progress UI.** The stair hides the reset | The tiled subway, the yellow tactile strip, its sign graphics |
| *POOLS* (Tensori, 2024), 54:42–54:56 [read via `01_ip_research.md` §7] | Ladder, stairs up, then a new tiled space with light from above | Light from the next space spills down the stair first | — |

### 2.2 Film and TV

| Title (year) | Scene | What works | Read |
|---|---|---|---|
| *Vertigo* (Hitchcock, 1958) | Bell-tower stair. The look down the well with the dolly zoom | The **well** as vertigo: repeated rails shrinking into depth. **Not ours:** a two-level stair with a 0.60 m well shows one landing below, not an endless shaft (the claim was dropped in the fix stage) | [memory] |
| *Jacob's Ladder* (Lyne, **1990**) | Subway: Jacob finds the station exit locked. He climbs the stairs, finds the exits locked, crosses the tracks and is nearly hit by a train | Era-exact. **A locked exit forces a worse route.** It is the dread of the `NO RE-ENTRY` door | [search] S20 (Wikipedia, Looper) |
| *Die Hard* (McTiernan, **1988**) | Nakatomi Plaza, a late-1980s office tower partly under construction. McClane moves floor to floor by the fire stairs | The office fire stair as the hidden spine of the building: bare concrete, steel doors | [memory] |
| *[REC]* (Balagueró & Plaza, 2007) | A sealed Barcelona apartment block. The stairwell round a central well is the only way between floors. The camera light looks up and down the well | A sealed building turns the stair into the only path. "Up" and "down" both feel wrong | [memory]; trailer listed [search] S21 |
| *The Raid* (Evans, 2011) | A 15-storey block taken floor by floor; stairwells between fights | The stair as the "between" beat: a breath, then the next floor | [memory] |
| *1408* (Håfström, 2007) | The evacuation map on the hotel door later shows only room 1408, with no exits | Period signage turned against you. Relevant to the placard, not a model for the stair | [memory] |
| TV | No strong stairwell case was verified in this pass. *Severance* (2022) has Lumon stairwell scenes to check | — | [UNVERIFIED] |

### 2.3 Games

| Game | Scene | What works | Read |
|---|---|---|---|
| *Silent Hill 2* (Konami, 2001; remake 2024) | Historical Society: a hole in the wall leads down a very long staircase into the Toluca prison. The remake's trophy for it is "Into the Abyss" | **Length is the dread**: no landing, no end in sight. It marks a deeper layer. Our descent is short (two flights), so take the "deeper layer" meaning, not the length | [search] S22 |
| *P.T.* (Kojima Productions as 7780s Studio, 2014) | The only open door is at the end of the hallway, reached down a short flight. Each loop changes the hallway | A short stair before the door builds the "next loop" beat | [search] S23 for the loop; the stair [memory] |
| *SCP-087-B* (2012), after SCP-087 (Zaeyde, CC BY-SA 3.0) | An unlit stair at 38°, 13-step flights, semicircular landings about 3 m wide, about 1.5 flights visible ahead | Endless switchback, short sight lines | [read] S24. **Do not copy**: the dark (our rule: darkness is never cover), the landing shape, the face |
| *F.E.A.R.* (Monolith, 2005) | Office-building stairs and ladders. The apparition at the foot of a ladder | A vertical move as a scare spot | [memory] |
| *The Exit 8* | see §2.1 | — | — |
| *Dark Deception* (Red's primary reference) | Chases end at an exit portal; no stair | A goal at the end of a run (`room_visuals/03_run_research.md` §3.5) | [on disk] |

**What all of these share.**
1. The stair is the one place where the player cannot see the destination. The next floor is hidden by the turn or the well, never by darkness.
2. Repetition: each landing looks like the last, so a single changed thing (a number, a lamp out, a stain) is read at once. BSS 120 §2.4.8 says the same about real users.
3. A locked door behind is stronger than a monster in front.

---

## 3. The map minimum (关卡设计): what the look must fit

### 3.1 What is decided

From systems.json C22 (answered 2026-10-21) and its rationale [read]. 关卡设计 has **no written stairwell plan yet**: `~/FrontRoomsMapWork/notes/RESUME.md` ends at "exit minimum when C22", and `briefs/map.md` lists it as not landed. Its Alpha order puts W1 lamps first, then the exit minimum (stairwell, 2–3 days), then W2.

- A **map-built stairwell room**.
- The exit door **shuts and locks behind you like the start door**.
- **The old map is released.**
- **A new seed starts at the stairwell's far door.**
- **The tier carries over.**
- A **FLOOR count on CAUGHT**.
- A placement rule **reachable without glass**.
- Not included, and ours: the EXIT sign in the map and the stairwell look. Not ours: the arrows (C08), strings and type.

### 3.2 Fixed numbers the look must respect

[read: `LEVEL_MODULE_SPEC.md` §1–4, `MAP_GENERATION.md`, kit sidecars in `Assets/Resources/Props/Models/*.json`, memory `frontrooms-title-handoff`]

| Constraint | Value | Consequence for the stairwell |
|---|---|---|
| Cell / chunk | 3.0 m; 8 × 8 cells = 24 m | footprint in whole cells: proposal 1 × 2 |
| Wall | 0.16 m, centred on the cell line; clear span n·3 − 0.16 | inside clear 2.84 × 5.84 for 1 × 2 cells. A thicker CMU look goes inside that box only by agreement |
| Ceilings | Low 2.4, Standard 2.9, Tall 5.4. Floor slab 0.20 below y 0, ceiling slab 0.16 above the ceiling. Props stay 0.05 below the ceiling | the exit door's room side should be **Standard**. Tall zones are window-only, so they fail "reachable without glass" anyway |
| Map door | opening 1.0 × 2.1 m centred on the cell edge. Leaf 0.98 × 2.08 × 0.05, hinge on the edge's low-coordinate jamb, pivot on the wall centre line. **Single-acting**. Keep-clear 1.2 m each side | the stair door can only be centred in a 3 m wall, so it sits in the **landing end wall** (§3.3) |
| Steel kit | `Kit_DoorFrame_Steel` head to 2.175, minCeiling 2.225. `Kit_DoorLeaf_Steel` minCeiling 2.145. Crossbar on the leaf at y 1.000, 0.909 long | fits both Low and Standard |
| EXIT sign | `Kit_ExitSign` 0.346 × 0.212 × 0.064, Wall placement. Spec mount: bottom = door top + 0.08 = 2.18 (`exit_sign/10_spec.md` :515), so the top is 2.392 | Standard: 0.51 m to spare. **Low (2.4): 8 mm, which breaks the 0.05 ceiling clearance** |
| Start area (title precedent) | `SetStartArea(rect, doorCell)`: the map builds nothing in the rect and walls it on three sides; the room's wall with the door is the fourth side. `MapRootFor` puts the root on a **192 m multiple**, with a **Standard Level 0** five-cell row past the door and an open edge straight ahead. The door is held until `ReadyAround` (≤ 3 s, measured ≤ 0.3 s). It closes and locks once the player is 4 m in (`CloseTerminalDoor`). The Relay ticks only after that. Lamps whose light would leak through the start walls are held (`StartLampsNorth/South`) | the stairwell is likely the new floor's start area (assumed: 关卡设计 reuses this path). Its shell must be **light-tight**. If `MapRootFor` is reused, the far door opens onto a Standard Level 0 cell. **Inside the stairwell there is no Relay** (the old map is released, and the new Relay starts after the far door shuts): it is the one safe room, still lit |
| World period | surfaces are world-projected; roots on 192 m multiples (XZ). `_CeilingHeight` is set per renderer from the chunk floor Y | a vertical offset per floor is possible. Paper phase may differ between floors, which is invisible because two floors are never seen together |
| Glass rule | cues never point at windows or glass | no lite in the exit door. The shaft is blind anyway (§1.1) |

### 3.3 Proposed stair geometry (to agree with 关卡设计)

Footprint 1 × 2 cells: 3 × 6 m on centre lines, **2.84 × 5.84 m clear**. Switchback, two flights per floor.

| Element | Size | Code check |
|---|---|---|
| Floor to floor | **3.50 m** (alternative 3.60 m = 22 × 163.6 mm, 11 per flight) | — |
| Risers | 20 × **175 mm**, 10 per flight | ≤ 178 ✓ |
| Treads | 9 per flight × **280 mm** = 2.52 m run | ≥ 279 ✓ |
| Flight width | **1.12 m** each, wall face to guard (fix stage; was 1.22) | ≥ 1,118 ✓ |
| Well | **0.60 m** between guards (was 0.40) | — |
| Levels | **two**: the exit floor and the far floor; a slab over the exit landing, the far landing is the shaft floor | §3306(o): only one stair needs to reach the roof |
| Landings | 2 per floor, each 2.84 wide × **1.66 m** deep (2.52 + 2 × 1.66 = 5.84) | ≥ width 1.22 ✓; ≥ 44 in ✓; the leaf (0.98) clears inside 1.66 ✓ |
| Headroom | about 3.25 m under the flight above (3.50 − 0.25 structure) | ≥ 2.032 ✓ |
| Guard | 1,067 mm at landings and the well; rail 864–965 mm along flights; pickets ≤ 100 mm apart | ✓ |
| Doors | **centred in the end wall of the door landing.** After two flights the player is back at the same end, so the **far door is directly below the exit door** | single-acting; never over a step ✓ |

The exit door swings **into** the stair, in the direction of egress. Its push side is the old floor, which carries the crossbar and the EXIT sign. Its stair side is blank, with the closer, the floor number and `NO RE-ENTRY`.

The far door, by code, also swings into the stair (it is the new floor's stair door), so the player **pulls** it from the landing. Map doors are single-acting with a pull give-way. 关卡设计 decides whether the far door opens toward the player (true to code) or away from the player (simpler).

### 3.4 Two ways to "go one floor deeper" (关卡设计's call; the look supports both)

- **(A) Real descent.**
  - The new map is built 3.50 m lower (y −3.50·n), at the same root or a new XZ root on a 192 m multiple.
  - The stairwell is one physical shaft spanning both floors.
  - Cost: a y offset in map, streaming and the Relay.
- **(B) Periodic shaft and rebase.**
  - The shaft looks identical every floor. After the exit door locks (old map released), the game moves the player and the stairwell by +3.50 m at an unseen moment, for example mid-flight.
  - The new map stays at y 0.
  - The look needs: identical landings except the floor sign; no unique decals across the period; geometry visible two floors up and down through the well, built as instances.

The two landings are identical except their floor sign, so either way works. The stair no longer repeats down the well (fix stage).

### 3.5 Things the look must add (proposal; 游戏视觉 builds, 关卡设计 places)

1. **No barriers needed** (fix stage). The stair serves two levels: a slab over the exit landing (`NO ROOF ACCESS` on the ID sign) and the far landing as the shaft floor. A gate across an up flight is not a 1990 device (critic era 3). If a stair ever continued below, a down-gate would be a **borrowed** discharge-level device (§3309(e) means the street floor), with a mid-grey plate.
2. **Signs on the stair side**: the stair ID sign on the **wall beside the latch jamb** at 5 ft (§3306(p)), re-entry / `NO RE-ENTRY` on the leaf (LL5), a stencilled numeral on the CMU (hinge side). Only the far door has pull trim and a floor-access plate. The type is 平面视觉's.
3. **Light.** One twin-head battery unit and one normal fixture per landing. Steady, ≥ 1 fc. A deeper floor may have one more dead normal lamp, never a dark landing, and changes stay ≤ 3 per second.
4. **Exit door kit.** C23 recommendation: `Kit_DoorLeaf_Steel` (no lite), `Kit_DoorFrame_Steel`, `Kit_DoorCloser_*`, and `Kit_ExitDevice_Crossbar` on the push face, with the lit `Kit_ExitSign` over it, locked to LIT.
   - **A13:** two states on the push face, LOCKED (a chain round the crossbar to a wall eye bolt, a padlock) and UNLOCKED (the chain on the floor); the sign stays LIT in both (`10_directions.md` §1.2).
   - **Arrival door on the new floor:** **no** dead EXIT sign (changed in the fix stage; both critics): a DEAD `Kit_ExitSign` still reads "EXIT", puts a second EXIT where the player starts and reads as decay. A non-EXIT stair-ID or floor plate goes on its floor side; strings 叙事, art 平面视觉.
5. **Standpipe riser and valve at both door landings, conduit, one sprinkler head at the top.** These are the real objects of a shaft.

### 3.6 Open questions for 关卡设计 (one message, with recommendations)

1. Real descent (A) or rebase (B)? Recommendation: whichever is cheaper. The look is periodic either way.
2. A 1 × 2-cell footprint, 3.50 m floor to floor, doors in the end wall? Recommendation: yes.
3. Exit door only on a Standard-zone wall? Recommendation: yes. The EXIT sign fits, and Tall zones are glass-only anyway.
4. Far door: pull toward the player (true to code) or push? Recommendation: pull, if the existing pull give-way handles it.
5. Does the arrival door get `Kit_ExitSign_Dead` on the floor side? Recommendation (revised): **no**; a stair-ID or floor plate instead.

---

## 4. Game rules and the look (checklist for stages 2–4)

- **Darkness is never cover.** The shaft is always ≥ 1 fc on every walkable surface. The well may fall off with distance but is lit at each landing.
- **Cues are never a radar.** Floor signs and stencils are static and tell only "where you are". The EXIT sign is only on the exit door. Nothing in the stair points anywhere except the code-real "EXIT DISCHARGE" arrow, which we do **not** use, because the exit discharge is not a game target.
- **No glass cue.** No lite in the exit door. The shaft has no windows. A far-door lite, if any, is wired glass ≤ 100 in², so the current `SteelLite` slab (≈ 120 in²) would need resizing, and no cue points at it.
- **Photosafety.** No strobe or flicker above 3 changes/s. Reduce Flashing respected. Emergency heads are steady.
- **No Relay state on the HUD.** The FLOOR count shows on CAUGHT only (C22). The in-world sign is diegetic.
- **Desktop is the reference.** LOD0 for the door, crossbar, closer and twin-head. Instanced repeats for the shaft. A WebGL tier separately, later.
- **Era lock ≤ 1993, no real brands.** No ADA tactile signs, no photoluminescent strips, no ISO running man, no maker logos on the exit device or emergency unit.

---

## 5. Open and UNVERIFIED

1. NFPA 101-1988 stair numbers (§5-2.2: 7/11, 44 in, 6 ft 8 in, 12 ft; stair ID for 5+ stories; re-entry). From memory and consistent with UBC 1991, but **not read**. The *Life Safety Code Handbook* 1988 exists on archive.org only as a lending copy (S7).
2. BOCA National Building Code/1990 (11th ed.): not read (lending copy, S8). Its commercial stair values may match UBC. The only BOCA fact found is about residential stairs (S10).
3. UBC 1988 versus 1991 differences in §3306: not checked. The older 7½ in / 10 in values are from a California Title 24 text [search].
4. Typical 1990 office floor-to-floor (3.66–4.11 m) and stair-light fixture types: ESTIMATE. A 3.96 m option that fits 1 × 2 cells (24 × 165 mm risers, 11 × 280 mm treads, 1.38 m landings) is passed to 关卡设计 (`10_directions.md` §1.1).
7. The 1991 Hamlet, NC poultry-plant fire as the period case of chained exits: [memory], verify before quoting.
5. Film and TV scenes marked [memory]: check times before any frame is used.
6. Wording such as `NO RE-ENTRY` and "FIRE DOOR · KEEP CLOSED": common stock wording, ESTIMATE. The strings are not mine.
