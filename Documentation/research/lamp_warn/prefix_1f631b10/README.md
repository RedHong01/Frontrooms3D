# C10 lamp-look capture, pre-fix (WarnLamps 1f631b10)

The first C10 capture (2026-10-22 07:36–08:14, clone `W/proj_es_fix1` synced 07:18 from main dfc0086: Game.cs 93a58d76, MapWorld 7ef0ca45, WarnLamps 1f631b10, MapHunter fb2fe3dd). It ran before 关卡设计's 09:11 W1 fix (WarnLamps b11d8f93), so it is **stale for verdicts** and kept only for before/after comparison.

- `data/`, `images/`, `video/`: the capture files that were in `research/lamp_warn/{data,images,video}/` until 09:2x; moved here when the recapture on WarnLamps b11d8f93 took their names.
- `data/stutter_failing.tsv` and `images/stutter_failing_levels.png` were computed afterwards (09:16) from this capture with `W/lamp_warn_c10/tools/extra_plots2.py`. They show the old snap: the L0 Failing lamp 276,201 steps +0.719 at the first dip (f69) and −0.542 at the stage-0 return (f1627).
- The pre-fix analysis (report `10_lamp_look_check.md`, `data/10_*`, `images/10_*`, 08:2x–08:5x) moved to `analysis_prefix/` on 2026-10-22 10:0x, when the recapture's analysis (WarnLamps b11d8f93) took the `10_*` names in the parent folder. Its numbers are quoted as "pre-fix" in the new `../10_lamp_look_check.md`.
