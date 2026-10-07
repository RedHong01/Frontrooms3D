# 04 — Key icon (K1): for Red

Fix stage, 2026-10-07. Main checked at `279c144`. Nothing was installed in the game. No Unity, no code, no `Assets/` file and no other chat's Figma section was touched. No media was downloaded.

Red's ask (2026-10-03): put the key icon into the UI design, research it, and make it echo the 3D key.

---

## 1. First: a bug in main hides the key panel on desktop

- **What:** since the mobile work (`c1f2d31`, 2026-10-04), `FrontRooms3DGame.cs:1883` anchors `HUD / Key` top-left on every platform. On desktop it keeps the old position `(72, 118)`, which was meant for a bottom-left anchor. So the panel sits 96–118 px **above** the top of the screen.
- **Effect:** Editor, Mac and Win show **no key HUD at all**. Any new key PNG is also invisible on desktop until this is fixed. Phones are fine (`ApplyMobileSafeAreaLayout` re-anchors them).
- **Fix (map chat owns the file; a one-line contract request, item 1 in §5.3):** anchor bottom-left again on desktop only.

```csharp
// FrontRooms3DGame.cs:1883 (279c144) — proposed
keyPanel = TypographyGroup(g.transform, "HUD / Key", mobileHud ? new Vector2(0, 1) : new Vector2(0, 0),
    mobileHud ? new Vector2(78, -72) : new Vector2(72, 118), mobileHud ? new Vector2(150, 22) : new Vector2(147, 22));
```

---

## 2. Where to look

| What | Where |
|---|---|
| **Verification slides (new)** | Figma, page `2099:76`, section **FRONTROOMS · VISUAL VERIFICATION LOG** (`2595:6093`): **VL100–VL105** (this stage) and VL075–VL077 (images refreshed). Ids in `03_figma.md` §8 |
| The HUD variations in Figma | 平面视觉's section **FRONTROOMS · HUD KEY · UI VARIATIONS** (`2532:4038`). It still shows the 2026-10-03 set; it needs their re-sync (`03_figma.md` §0) |
| Review boards 01–10 | `design/01_overview.png` … `design/10_recommendation.png` (re-rendered today) |
| The recommendation in one board | `design/10_recommendation.png` |
| Locked prompt on six grounds | `images/12_locked_prompt_card.png` |
| Phone, today vs proposal | `images/15_phone_hud.png`, full frames `design/hud/phone_*_3x.jpg` |
| Design notes / hand-off | `02_design.md` (design), `03_figma.md` (FINAL hand-off to 平面视觉), `01_research.md` (research) |

All paths are under `Documentation/research/ui_key_icon/`.

---

## 3. Recommendation

**A · Cut key.** The HUD icon is `Kit_Key_Zone`'s own side outline (bow, shoulder step, six cuts, bevelled tip), in one paper colour `#F4F1E8`, like the HUD type. The crosshair dot stays white `#FFFFFF`. Main's FBX still matches the trace (IoU 0.99996, re-run today, VL075).

| Part | Proposal |
|---|---|
| Held (this zone) | the filled key, 49 × 22 px; label "KEY" in Bayon 20 + the zone number in Courier Prime Bold 25 (the tag's own type) once zones have numbers. Until then the zone-name label stays |
| Held for another zone | the **outline** key + paper label + Plex Mono 13 "NOT THIS ZONE" in paper. Shape says "this key does not open this door"; no more 40 % / 70 % dimming |
| Locked door (no key) | the outline key + "LOCKED" (Bayon 20, paper) + "NEEDS KEY 14" (Plex Mono 13), on the HUD's own hint card `#141414` at 90 %, padding 12 × 8 px. The spec makes every locked leaf light almond enamel, so the prompt needs its own ground |
| Used | not a state today (keys are never spent). Shown only as "IF KEYS BECOME SINGLE-USE": filled key at 70 % + paper type |
| Phone | the same design at the touch type ramp: label 17 pt, numeral 21 pt, meta 11 pt; its own @3x sprites |
| Tag chip (option) | the zone's tag left of the key, now hung on a 1 px ring wire through both holes. Needs map data that does not exist yet |
| B (key on tag), C (sign plate) | not recommended (B needs a 48 px panel and saturated colours; C drifts from our key and reads like a keycap) |

**Contrast (WCAG: 3 : 1 for graphics, 4.5 : 1 for small text).** Measured per pixel under each element (`03_figma.md` §2.5).

| Element | Worst frame | Before | After |
|---|---|---|---|
| Locked prompt, outline key | almond leaf | 1.31 : 1 | **5.92 : 1** |
| Locked prompt, "LOCKED" | almond leaf | 1.19 : 1 | **8.04 : 1** |
| Locked prompt, "NEEDS KEY 14" | almond leaf | 1.19 : 1 | **4.68 : 1** |
| Today's code prompt (no glyph) | almond leaf | 1.45 : 1 | — |
| Other zone, key | lit carpet (p10) | 2.24 : 1 | **3.16 : 1** |
| Other zone, label | lit carpet (p10) | 1.95 : 1 | **4.04 : 1** |
| Other zone, meta | lit carpet (p10) | 2.16 : 1 | **3.72 : 1** |
| Held key and label | lit carpet (p10) | 4.10 / 4.04 : 1 | unchanged |

Limit: on lit carpet, paper itself only reaches 3.9–4.3 : 1, so no small paper text there can reach 4.5 : 1 without a ground. The bottom-left row has no card on purpose (the room typography doesn't either). That is a HUD-system choice for 平面视觉.

---

## 4. Decisions for Red

1. **Direction:** A (recommended), A + chip, B or C?
2. **Locked prompt:** outline key + LOCKED + NEEDS KEY nn on the hint card (recommended), or a 1 px shadow without a card (lighter, but a shadow gives no measurable ground for the 13 px meta)?
3. **Other zone:** show the panel with the outline key, or keep it hidden as today?
4. **Label:** "KEY 14" with the number in Courier Prime Bold, once zones have fixed numbers? Until then, the zone-name label?
5. **Drop-in now:** may the visual chat copy `design/dropin/slot_40x22/HUD_KeyGlyph.png` into `Assets/Resources/UI/` today? It changes the generic ring key into our key. On desktop it only shows after the anchor fix (§1).
6. **Tag chip:** keep it as an option (needs per-zone tag data)?

---

## 5. What landing means

Nothing lands from this workflow. After Red reviews the Figma:

### 5.1 PNGs into `Assets/Resources/UI/` (visual chat; these are its files)

| Source (`design/dropin/`) | Target | Size | When |
|---|---|---|---|
| `slot_40x22/HUD_KeyGlyph.png` | `UI/HUD_KeyGlyph.png` (overwrite) | 80 × 44 | today, no code change |
| `slot_49x22/HUD_KeyGlyph.png` | `UI/HUD_KeyGlyph.png` (overwrite) | 98 × 44 | after contract item 2 |
| `slot_49x22/HUD_KeyGlyph_Touch.png` | `UI/HUD_KeyGlyph_Touch.png` (new) | 147 × 66 | after contract item 3 (`slot_40x22/…_Touch.png`, 120 × 66, if the rect stays 40 × 22) |
| `prompt/HUD_KeyOutline.png` | `UI/HUD_KeyOutline.png` (new) | 102 × 48 | after contract item 5 |
| `prompt/HUD_KeyOutline_Touch.png` | `UI/HUD_KeyOutline_Touch.png` (new) | 153 × 72 | after contract items 3 and 5 |
| `crosshair/HUD_Crosshair_Touch.png` | `UI/HUD_Crosshair_Touch.png` (new) | 144 × 144, dot 72 | after contract item 3 |

- Keep main's `.meta` for `HUD_KeyGlyph.png` (GUID `eb266a1345aeb45f69bf39f9c6d34fb4`); change only its import fields. New files get new GUIDs.
- Import: Sprite (2D and UI), sRGB, Alpha Is Transparency, Bilinear, no mipmaps, **Compression None** (main's `.meta` still says Normal), Max Size 256.
- All of them: corners alpha 0, one RGB value in every pixel (no dark fringe under bilinear filtering). Checks in `$W/keyicon_figma/export_touch_checks.json` and `export_3x_checks.json`.
- The desktop `HUD_Crosshair.png` (64 × 64, dot 32) does not change.

### 5.2 Figma

- 平面视觉 re-syncs section `2532:4038` from `03_figma.md` §0 (13 SVG masters changed, 80 crops changed, 21 added; three path fixes in their tools). Their call: the prompt card (their UI06 mockup has the same contrast problem), B's yellow rule, keeping the "used" column.

### 5.3 Contract request to the map chat (they own `FrontRooms3DGame.cs` and the map files; exact lines at `279c144`)

1. **Desktop anchor bug** (§1): `FrontRooms3DGame.cs:1883`, desktop anchor `(0, 0)`.
2. **Glyph rect:** `:1885` `new Vector2(40, 22)` → `new Vector2(49, 22)`; label `:1888` `new Vector2(54, 0)` → `new Vector2(63, 0)`.
3. **Phone sprites and size:** `:1886` `LoadHudSprite(mobileHud ? "UI/HUD_KeyGlyph_Touch" : "UI/HUD_KeyGlyph")`; `:1867` `LoadHudSprite(mobileHud ? "UI/HUD_Crosshair_Touch" : "UI/HUD_Crosshair")`. Today one 40 × 22 px sprite is stretched ×3 on phones, and the 64 px crosshair is stretched into 72 px. Label size `:1888` `mobileHud ? 11 : 20` → `mobileHud ? 17 : 20` (TOUCH_CONTROLS §4 ramp; also flag to the touch chat).
4. **Zone number:** a second Text after "KEY": Courier Prime Bold, 25 (phone 21), paper, 6 px (phone 5 pt) after "KEY", named without "Key" so `UiFont()` does not give it Bayon. Courier Prime is in `Assets/Fonts/Period1990/`, so the HUD needs a reference or a `Resources` copy. `KeyLabel()` (`:2253`) → "KEY".
5. **Locked prompt:** today `FrontRoomsMapWorld.cs:2203` returns the string "LOCKED  ·  NEEDS THIS ZONE'S KEY" and the game prints it in Bayon (Bayon has no "·"). Proposed: when the aimed door is locked, the game draws the outline Image (`UI/HUD_KeyOutline`, 51 × 24 rect, 1 px up-left of the 49 × 22 key slot) + "LOCKED" (Bayon 20, paper) + a meta Text (Plex Mono 13, muted `#BDBAB0`) "NEEDS KEY nn" (until zones have numbers: "NEEDS THIS ZONE'S KEY"), on a card Image `#141414` at 90 % (the `media` colour), padding 12 × 8 px. Desktop: row centre 44 px under the crosshair centre (as today), meta baseline 30 px under that, card y 565–622 at 1080p. Phone: row centre 32 pt under the dot (was 26), label 17, meta 11, padding 10 × 7 pt. The map needs to expose "locked" and the door's zone instead of only a string.
6. **Other zone:** `:2214` shows the panel only when `HasKeyFor(zone.id)`. Proposed: also when `map.KeysHeld > 0` (`FrontRoomsMapWorld.cs:57`) and not `HasKeyFor` (`:830`): outline sprite + paper label + Plex Mono 13 "NOT THIS ZONE" in paper.
7. **Map data:** `ZoneInfo` (`FrontRoomsMap.cs:136`) has no tag number, colour or shape. Add a fixed tag number (00–99), colour (red / blue / white) and shape (rect / round / long) per zone, from the same source that dresses the 3D key, its tag and the door number plate. Decide what the room meta's "ZONE nn" (a visit counter, `:2182`) shows next to it.
8. **The 3D key:** `SpawnKey` (`FrontRoomsMapWorld.cs:2852`) still spawns a 0.32 × 0.12 × 0.12 m cube with the yellow `keyGlow` material. Proposed: `FrontRoomsKitLibrary.Spawn("Kit_Key_Zone", …)` + `Kit_KeyRing` + `Kit_KeyTag_<Shape>[_<Colour>]`, driven by item 7's data, placed per `research/interactables/10_spec.md` §4. **The icon only echoes the model once this lands**; today the player never sees the key the icon is traced from.

---

## 6. What this fix stage changed (critic, 18 issues)

| # | Issue | Fix |
|---|---|---|
| 1 | Desktop key panel off-screen in main | Found and written up as contract item 1 (§1, §5.3). Not edited (map chat's file) |
| 2 | Locked prompt unreadable on the almond leaf | Prompt on the hint card, LOCKED in paper; new 5th ground `leaf` (`design/bg/leaf_48_almond.png`, made from in-engine frame 48) plus today's wood leaf; worst case now 5.92 / 8.04 / 4.68 : 1 (VL100) |
| 3 | Pipeline lost in the `/private/tmp` wipe | Rebuilt from the transcripts into `$W/keyicon_design`; re-run reproduces 331 / 334 Oct 3 files byte for byte (3 boards differ only in the typed tag digits of the model renders) (VL104). Durable copies: `scripts/*.py.txt` |
| 4 | Outline masters cut half their stroke | A_L, A_S and C outline boxes grown 1 px where the stroke ran out; sprites placed 1 px up-left (VL102) |
| 5 | Other-zone and used states fail on lit carpet | Dim by shape: outline key, paper type; used = filled key at 70 % (VL101) |
| 6 | Phone HUD never designed or checked | Phone composites at 874 × 402 pt @3x (today vs proposal, held and locked); `_Touch` sprites with loadable names; §5 of `03_figma.md` corrected (24 pt rect, 12 pt dot) (VL103) |
| 7 | Stale code line numbers | Contract rebased on `279c144` (§5.3) |
| 8 | The echo is not in play | Contract item 8 (SpawnKey → kit key) |
| 9 | VL076 image text overlap | Image 09 rebuilt (Bayon labels, no caption lines); the before crops regenerated and checked pixel-identical to the Oct 3 sheet; slot fill replaced |
| 10 | Board 10 hero showed the default text | Hero = the target (A_L + KEY 14) with a 1 : 1 inset |
| 11 | Captions and file lists on images | Board 10's file list and meta label removed; images 08, 09, 10 rebuilt with Bayon labels only; VL075's image re-laid out 2 × 2 so it fills the slot height |
| 12 | "like the crosshair dot" | Boards 01 and 10: "paper, like the HUD type; the crosshair dot stays white" |
| 13 | "Used" shown as a real state | Column renamed "IF KEYS BECOME SINGLE-USE" on boards 03, 07, 09 |
| 14 | A + chip reads as a battery | 1 px ring wire from the tag hole to the bow hole (VL105, Red's call) |
| 15 | iCloud copies tracked in git | Listed in `03_figma.md` §0 as not part of the set; not deleted |
| 16 | Wrong section reference in VL077's row | Fixed to §10.1 in `VERIFICATION_LOG.md` |
| 17 | C collapses into A on dark frames | The note stays on board 09 only |
| 18 | B's yellow rule in a calm state | Listed as 平面视觉's decision (board 07, `03_figma.md`) |

---

## 7. Open risks

- The almond leaf ground is a worst case (full albedo, lighting 1.0). An in-engine capture of a real L0-K door, once the kit doors are in play, should replace it.
- Plex Mono 13 meta in paper on lit carpet is 3.5–3.9 : 1. Only a ground can lift it (see §3).
- The B outline in the other-zone state is 2.97 : 1 at its single worst pixel on lit carpet (p10 3.12). B is not recommended.
- The codex audit never wrote `20_findings.md`; `00_main_state.md` lists the key icon as not touched, and no `10_review_*.md` names K1.
