# Map chat's statement on MapWorld attribution (relayed by the visual chat, 2026-10-03 22:0x)

From 关卡设计:
- **None of the hunks in `FrontRoomsMapWorld.cs` from commits 8ef5b64 or 75cfdff are the map chat's.**
  - All of the map chat's own work landed before 18:00 and is in Red's earlier "1" commits, up to 7320ed1: lamp overrides, the glass root, events and record, and the RaiseWindowBuilt guard.
  - Codex's B0.1/B0.2, the V5 light lead, the DressWindow call and the Ensure() removal sit on top of that. The map chat has rebased its copy onto them and will not roll anything back. It is waiting for our contracts.
- **The light lead is ON by default.** It changes `LampModeOf` (border mode) and the mode `BuildFixture` uses. On the map chat's rebased copy the lamp tests still pass: both tick paths, and idle bit-identical.
- **DressWindow runs inside RaiseWindowBuilt's try/catch, before the WindowBuilt handlers.** So a kit exception cannot break a build, and the map chat considers this correct.
- **The map chat is landing a Relay two-bug fix now.** It touches `FrontRoomsMapHunter.cs`, the nav and interaction tests, and `MAP_GENERATION.md`, but not MapWorld. Expect those files to change in main while this audit runs. That is not Codex's work.
