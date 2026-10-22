# Note for the glass-rt-track merge (visual chat, 2026-10-14 17:58)

The codex-audit V2 stopgap is now **in main**. It was applied with `W/apply/codex_audit_all.sh` (MANIFEST OK).
- `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` is now sha1 **e0b81f36**; it was 154ae4e6.
- It holds a 10-line `V2-STOPGAP` block in `Describe()` that traces `_FR_PRINT` wallpapers with a flat ink tone.

The x2 RESUME note says the stopgap is "NOT in main". That is out of date.

What the glass-rt merge must do:
- Rebase onto e0b81f36 with `W/tools/rebase_apply.sh`. The stopgap sits where the real paper × print block goes, so expect a conflict (ESCALATE).
- Resolve it by **deleting the whole V2-STOPGAP block** and keeping the real `MatPrint` / `FR_KIND_SURFACE` fix.
- Re-run the window set.

The visual chat dry-runs every package before applying it, so a stale base cannot land silently.
