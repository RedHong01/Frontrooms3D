R2_key_material_contract.git.diff — contract patch for the map chat (关卡设计). Map-owned file: NOT applied to Red's project.
  Target: Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs at main ee5c9bb (md5 07ce188a9b646bb6bc03c150795b183b; unchanged since b5f381f, 2026-10-04).
  Apply (map chat, from the project root): git apply --check Documentation/research/codex_audit/runtime/head_ee5c9bb/patches/R2_key_material_contract.git.diff && git apply <same path>
  Checked 2026-10-07 23:3x: `git apply --check` passes on a copy of HEAD's file.
  Code = the one-line change verified in W/proj_cx_fix (fix_keys run: key keeps 'Map test / key' after R, 0.00 % magenta) + 2 comment lines.
  The older runtime/head_279c144/patches/R2_key_material_contract.diff is a readable note, not a git patch (its hunk header has no line ranges).
R1: no patch here. Use runtime/rt/rt_failclosed.diff (10_review_rt.md F1; patch -p0), owned by glass-rt-track.
