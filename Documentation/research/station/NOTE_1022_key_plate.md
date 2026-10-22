# NOTE 2026-10-22 12:4x: the key-rack warning plate (from 教程设计, for V1 and the station look)

教程设计 decided (tutorial/10_proposal.md §4.7; V1 note in tutorial/20_requests.md): the key rack keeps 叙事设计's warning plate (`STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL`, wording v2 since 12:2x; see the addendum at the end), on the **close-reading layer only**.

1. The plate reads only in EXAMINE or within about 1 m, never from the counter or from the first view of the rack. Its size comes from 平面视觉 against the EXAMINE readability rule (research/inspect/10_design.md; computer_ui/20_design.md §3.5 AT-T19 for glance-size text).
2. Its EXAMINE aim box must never take the aim from the key: aiming at the key always gives `E · TAKE KEY` (crosshair grammar §4 priority; the key's trigger collider wins over the plate's aim box).
3. The plate stays on the wall after the take.
4. The conduit to the annunciator (30_look.md §4.3) keeps carrying the wordless "this key is wired" hint.

No other change to V1. Stages of station-look that build the rack, the Figma slides or the joint draft must follow this.

## Addendum 12:5x: 叙事设计 accepted every "For 叙事" item of 30_look.md §7 (20_layout.md v1.1 §4.0, d59dfd2c)

Build the blockouts on **W′ and D′**:
1. W′: the set on the E-wall bracket, E1 in the W wall's south cell.
2. D′: including the optional desk shift ~1.6 m east (chair + jacket against the wash from E1); the reveal is "the glow and the empty chair, then the key"; no spill light (c).
3. G5 reads: "at the take, the light on the key board drops in view" (≥ 70 %, one change), the green POWER lamp stays lit; blockout check 3 matches.
4. In B the guard's chair is turned to face the set.
5. Phone line lamps and the charger lamp are dark (no hold lamp; G9).
6. `STAIR A` is final for the tag (plan, tag, door plate and stair sign all use it); the floor token stays on the HUD tag (C28).
The plate over the key board is now decided by 教程设计 (above): EXAMINE layer only. The joint draft `40_for_red_draft.md` goes to 叙事设计 for review before Red.

## Addendum 12:1x: station lamp rule (教程设计; replaces "one lit lamp by the entrance")

**At least two Steady (not Dim) lamps on the entrance side; the TV corner stays Dim/Off** so the set is still the focal light near it.
- Why: W1's warning dips need ≥ 2 live lamps in the player's room set, and a dip on a Dim lamp barely shows; the ballast groan isn't built yet, so a burst in the station could otherwise be invisible. On Floor 1 the first burst should land right there just after the take (系统设计 is asking Red C32: hunter sent 1 s after the take at 22–30 m).
- Applies to both modules (W′, D′), their lamp arrays and every light-plan number in 30_look.md §3–§4 (re-check the TV light's share at the doorway and on the key with the two Steady lamps on).
- Source: tutorial/10_proposal.md §4.2 and the V1 note in tutorial/20_requests.md (教程设计 updating them).

## Addendum 2026-10-22 12:2x: plate wording v2 (from 叙事设计)

The plate now reads **`STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL`**. The v1 line `ALARMED: REMOVAL SOUNDS ALARM AT PANEL` is withdrawn: nothing rings in the room at the take, and 教程设计 flagged that "SOUNDS ALARM" could read as a local bell. Never bring back "SOUNDS". Source of truth: `01_research.md` §6. Every blockout, placeholder texture and draft (incl. `40_for_red_draft.md`) uses v2; the final type size stays 平面视觉's call. The v2 second line is shorter than v1 (35 vs 38 characters with spaces), so the 0.30 × 0.10 plate size in `30_look.md` S21 still holds.
