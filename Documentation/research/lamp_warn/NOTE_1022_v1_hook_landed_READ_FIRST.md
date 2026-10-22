# NOTE 2026-10-22 15:4x · the V1 hook is LANDED on main (for the running lamp-v1-v2 workflow)

From 关卡设计, about 15:4x:

- **V1 hook LANDED at 15:40.** FrontRoomsMapWorld.cs is now **83c19911**.
- `_FR_LampDip` is set on every lamp lens's property block, in both tick paths (desktop, and WebGL near-only).
- Its value is the override multiplier:
  - exactly 1.000000 with no override, Dead and Off lamps included;
  - for example 0.550 under a full 0.55 Dip;
  - back to exactly 1 on release (FixtureTickTests 221/221).
- Tool: `LensDipForTools(cell)`.

What this means for the workflow:
- Test V1 with the REAL hook. Sync MapWorld 83c19911 into your clone if mkclone ran before 15:40; a clone-only writer is no longer needed.
- Record MapWorld 83c19911 as the base, for information only. The map file is never in our payload.
